using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using VRTrainingBay.Assessment;

namespace VRTrainingBay.Environment
{
    /// <summary>
    /// Controls tutorial video playback using a high-resolution RenderTexture for URP.
    /// Works with both 3D Quads (MeshRenderer) and UI Screens (RawImage).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class TutorialVideoScreen : MonoBehaviour
    {
        [Header("Optional Manual Texture")]
        [Tooltip("Optional: Assign a custom RenderTexture asset. If left empty, one is created automatically!")]
        [SerializeField] private RenderTexture _customRenderTexture;

        [Header("Target Screen Display")]
        [Tooltip("Assign if using a UI Canvas screen.")]
        [SerializeField] private RawImage _uiRawImage;

        [Tooltip("Assign if using a 3D Quad/Monitor.")]
        [SerializeField] private MeshRenderer _meshRenderer;

        private VideoPlayer _videoPlayer;
        private RenderTexture _activeTexture;
        private bool _isRuntimeTextureCreated;

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;

            // Auto-detect target screen components if not assigned
            if (_uiRawImage == null) _uiRawImage = GetComponent<RawImage>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            // 1. Create or assign 1280x720 RenderTexture for crisp video in URP
            if (_customRenderTexture != null)
            {
                _activeTexture = _customRenderTexture;
            }
            else
            {
                _activeTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
                _activeTexture.name = "Auto_Video_RenderTexture";
                _activeTexture.Create();
                _isRuntimeTextureCreated = true;
            }

            // 2. Configure VideoPlayer to target the RenderTexture
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _activeTexture;

            // 3. Connect the RenderTexture to the display
            if (_uiRawImage != null)
            {
                _uiRawImage.texture = _activeTexture;
            }

            if (_meshRenderer != null)
            {
                Material mat = _meshRenderer.material;
                if (mat != null)
                {
                    // Set URP BaseMap or standard MainTex
                    if (mat.HasProperty("_BaseMap"))
                    {
                        mat.SetTexture("_BaseMap", _activeTexture);
                    }
                    else if (mat.HasProperty("_MainTex"))
                    {
                        mat.SetTexture("_MainTex", _activeTexture);
                    }
                }
            }
        }

        private void Start()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentStarted.AddListener(PlayVideo);
                AssessmentManager.Instance.OnAssessmentReset.AddListener(ResetVideo);
            }
        }

        private void OnDestroy()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentStarted.RemoveListener(PlayVideo);
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(ResetVideo);
            }

            if (_isRuntimeTextureCreated && _activeTexture != null)
            {
                _activeTexture.Release();
                Destroy(_activeTexture);
            }
        }

        [ContextMenu("Test Play Video")]
        public void PlayVideo()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.Play();
#if UNITY_EDITOR
                Debug.Log("<color=cyan>[TutorialVideoScreen] Video playing on RenderTexture screen!</color>");
#endif
            }
        }

        [ContextMenu("Test Reset Video")]
        public void ResetVideo()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }
    }
}
