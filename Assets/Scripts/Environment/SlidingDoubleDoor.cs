using UnityEngine;
using VRTrainingBay.Assessment;

namespace VRTrainingBay.Environment
{
    /// <summary>
    /// Smoothly slides two door panels in opposite directions with 3D spatial audio.
    /// Allocation-free (zero GC in Update).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlidingDoubleDoor : MonoBehaviour
    {
        [Header("Door Transforms")]
        [SerializeField] private Transform _leftDoor;
        [SerializeField] private Transform _rightDoor;

        [Header("Slide Settings (Local Space)")]
        [Tooltip("Local direction for the left door to slide.")]
        [SerializeField] private Vector3 _leftSlideDirection = Vector3.left;
        [Tooltip("Local direction for the right door to slide.")]
        [SerializeField] private Vector3 _rightSlideDirection = Vector3.right;
        [Tooltip("How far each door panel slides open in meters.")]
        [SerializeField] private float _slideDistance = 1.2f;
        [SerializeField] private float _openDuration = 2.0f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _doorOpenSound;

        private Vector3 _leftClosedPos;
        private Vector3 _rightClosedPos;
        private Vector3 _leftOpenPos;
        private Vector3 _rightOpenPos;

        private float _openProgress = 0f;
        private bool _isOpening = false;
        private bool _isOpen = false;

        private void Awake()
        {
            if (_leftDoor != null)
            {
                _leftClosedPos = _leftDoor.localPosition;
                _leftOpenPos = _leftClosedPos + _leftSlideDirection.normalized * _slideDistance;
            }

            if (_rightDoor != null)
            {
                _rightClosedPos = _rightDoor.localPosition;
                _rightOpenPos = _rightClosedPos + _rightSlideDirection.normalized * _slideDistance;
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.AddListener(CloseDoors);
            }
        }

        private void OnDestroy()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(CloseDoors);
            }
        }
        [ContextMenu("Test Open Doors")]
        public void OpenDoors()
        {
            if (_isOpen || _isOpening) return;

            _isOpening = true;
            _openProgress = 0f;

            if (_audioSource != null && _doorOpenSound != null)
            {
                _audioSource.PlayOneShot(_doorOpenSound);
            }
        }




        private void Update()
        {
            if (!_isOpening) return;

            _openProgress += Time.deltaTime / Mathf.Max(_openDuration, 0.01f);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_openProgress));

            if (_leftDoor != null)
            {
                _leftDoor.localPosition = Vector3.Lerp(_leftClosedPos, _leftOpenPos, t);
            }

            if (_rightDoor != null)
            {
                _rightDoor.localPosition = Vector3.Lerp(_rightClosedPos, _rightOpenPos, t);
            }

            if (_openProgress >= 1f)
            {
                _isOpening = false;
                _isOpen = true;
            }
        }

        public void CloseDoors()
        {
            _isOpening = false;
            _isOpen = false;
            _openProgress = 0f;

            if (_leftDoor != null) _leftDoor.localPosition = _leftClosedPos;
            if (_rightDoor != null) _rightDoor.localPosition = _rightClosedPos;
        }
    }
}
