using UnityEngine;
using UnityEngine.UI;

namespace VRTrainingBay.Assessment
{
    /// <summary>
    /// High-performance VR Laser Pointer for Meta Quest Touch Controllers.
    /// Draws a laser beam and clicks World Space UI Buttons via the Index Trigger or A button.
    /// Zero GC allocations in Update.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class VRControllerRayPointer : MonoBehaviour
    {
        [Header("Controller")]
        [Tooltip("Set to RTouch for Right Controller, or LTouch for Left Controller.")]
        [SerializeField] private OVRInput.Controller _controller = OVRInput.Controller.RTouch;

        [Header("Laser Visuals")]
        [SerializeField] private float _maxDistance = 5.0f;
        [SerializeField] private float _rayWidth = 0.006f;
        [SerializeField] private Color _idleColor = new Color(0.2f, 0.7f, 1f, 0.5f);
        [SerializeField] private Color _hoverColor = new Color(0.2f, 1f, 0.4f, 0.9f);

        [Header("Audio & Haptics")]
        [SerializeField] private AudioClip _clickSound;
        [Range(0f, 1f)][SerializeField] private float _hapticAmplitude = 0.4f;

        private LineRenderer _line;
        private AudioSource _audioSource;
        private GameObject _cursor;
        private Button _hoveredButton;

        private void Reset()
        {
            ApplyEditorVisuals();
        }

        private void OnValidate()
        {
            ApplyEditorVisuals();
        }

        private void ApplyEditorVisuals()
        {
            if (_line == null) _line = GetComponent<LineRenderer>();
            if (_line == null) return;

            _line.positionCount = 2;
            _line.startWidth = _rayWidth;
            _line.endWidth = _rayWidth * 0.4f;
            _line.useWorldSpace = true;

            // Assign URP compatible material so it never turns pink
            if (_line.sharedMaterial == null || !_line.sharedMaterial.shader.name.Contains("Universal Render Pipeline"))
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null) s = Shader.Find("Sprites/Default");
                if (s != null)
                {
                    Material m = new Material(s);
                    m.color = _idleColor;
                    _line.material = m;
                }
            }
        }

        private void Awake()
        {
            ApplyEditorVisuals();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
            }

            // Create reticle cursor dot
            _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _cursor.name = "LaserCursor";
            _cursor.transform.localScale = Vector3.one * 0.018f;
            Collider c = _cursor.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = _cursor.GetComponent<Renderer>();
            if (r != null && _line.material != null)
            {
                r.material = _line.material;
            }
        }

        private void Update()
        {
            Vector3 startPos = transform.position;
            Vector3 forward = transform.forward;
            Vector3 endPos = startPos + forward * _maxDistance;
            Button hitButton = null;

            // 1. Raycast for 3D physics colliders (buttons with BoxCollider)
            if (Physics.Raycast(startPos, forward, out RaycastHit hit, _maxDistance))
            {
                endPos = hit.point;
                hitButton = hit.collider.GetComponentInParent<Button>();
            }
            else
            {
                // 2. Also check World-Space Canvases
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                float closestDist = _maxDistance;

                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i].renderMode != RenderMode.WorldSpace) continue;
                    RectTransform rect = canvases[i].GetComponent<RectTransform>();
                    if (rect == null) continue;

                    Plane p = new Plane(-rect.forward, rect.position);
                    Ray r = new Ray(startPos, forward);

                    if (p.Raycast(r, out float enter) && enter < closestDist)
                    {
                        Vector3 pPoint = r.GetPoint(enter);
                        Vector2 localPoint;
                        Camera cam = Camera.main;

                        if (cam != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, cam.WorldToScreenPoint(pPoint), cam, out localPoint))
                        {
                            if (rect.rect.Contains(localPoint))
                            {
                                closestDist = enter;
                                endPos = pPoint;

                                Button[] buttons = canvases[i].GetComponentsInChildren<Button>();
                                for (int b = 0; b < buttons.Length; b++)
                                {
                                    if (!buttons[b].gameObject.activeInHierarchy || !buttons[b].interactable) continue;
                                    RectTransform bRect = buttons[b].GetComponent<RectTransform>();
                                    if (RectTransformUtility.RectangleContainsScreenPoint(bRect, cam.WorldToScreenPoint(pPoint), cam))
                                    {
                                        hitButton = buttons[b];
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Hover status & colors
            _hoveredButton = hitButton;
            Color currentColor = (_hoveredButton != null) ? _hoverColor : _idleColor;
            _line.startColor = currentColor;
            _line.endColor = currentColor;
            _line.SetPosition(0, startPos);
            _line.SetPosition(1, endPos);

            if (_cursor != null)
            {
                _cursor.transform.position = endPos;
            }

            // Click check (Trigger or A Button)
            bool isTriggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, _controller) ||
                                 OVRInput.GetDown(OVRInput.Button.One, _controller);

            if (isTriggerDown && _hoveredButton != null)
            {
                _hoveredButton.onClick.Invoke();

                if (_clickSound != null)
                {
                    _audioSource.PlayOneShot(_clickSound);
                }

                // Short haptic vibration pulse
                OVRInput.SetControllerVibration(0.5f, _hapticAmplitude, _controller);
                Invoke(nameof(StopHaptics), 0.08f);

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[VRControllerRayPointer] Clicked UI Button: {_hoveredButton.name}</color>");
#endif
            }
        }

        private void StopHaptics()
        {
            OVRInput.SetControllerVibration(0f, 0f, _controller);
        }

        private void OnDisable()
        {
            if (_cursor != null) _cursor.SetActive(false);
        }

        private void OnEnable()
        {
            if (_cursor != null) _cursor.SetActive(true);
        }

        private void OnDestroy()
        {
            if (_cursor != null) Destroy(_cursor);
        }
    }
}
