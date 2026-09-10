using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;
using VRTrainingBay.Assessment;

namespace VRTrainingBay.Cells
{
    /// <summary>
    /// Controls the power cell receptacle slot.
    /// Filters for Type-B cells to lock, and rejects Type-A cells with audio/visual/haptic feedback.
    /// Snaps reliably on first play and after restarts.
    /// Allocation-free (no runtime GC allocations in Update/Triggers).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CellSocket : MonoBehaviour
    {
        [Header("Socket Anchor")]
        [Tooltip("Transform where the cell snaps into (position and rotation).")]
        [SerializeField] private Transform _snapAnchor;

        [Header("Visual Feedback (LED Indicator)")]
        [SerializeField] private Renderer _ledRenderer;
        [SerializeField] private Color _idleColor = Color.gray;
        [SerializeField] private Color _acceptedColor = Color.cyan;
        [SerializeField] private Color _rejectedColor = Color.red;

        [Header("Holographic Ghost Silhouette")]
        [Tooltip("Ghost hologram preview of the cell that guides the user to the socket.")]
        [SerializeField] private GameObject _ghostSilhouette;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip _socketSnapClip;
        [SerializeField] private AudioClip _rejectionClip;

        [Header("Haptics")]
        [Range(0f, 1f)][SerializeField] private float _hapticAmplitude = 0.6f;
        [Range(0f, 1f)][SerializeField] private float _hapticFrequency = 0.5f;
        [SerializeField] private float _hapticDuration = 0.15f;

        [Header("Rejection Timing")]
        [Tooltip("Minimum seconds between repeated rejection triggers while hovering.")]
        [SerializeField] private float _rejectionCooldown = 1.0f;

        [Header("Events")]
        public UnityEvent<PowerCell> OnCellAccepted;
        public UnityEvent OnCellRejected;

        // Cached runtime state
        private Vector3 _cachedCellOriginalScale;
        private AudioSource _audioSource;
        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private PowerCell _socketedCell;
        private bool _isLocked;
        private float _lastRejectionTime;
        private float _hapticTimer;
        private float _ledResetTimer;
        private bool _isLedFlashing;

        public bool IsLocked => _isLocked;
        public PowerCell SocketedCell => _socketedCell;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f;

            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            if (_snapAnchor == null)
            {
                _snapAnchor = transform;
            }

            _propBlock = new MaterialPropertyBlock();
            SetLedColor(_idleColor);

            if (_ghostSilhouette != null)
            {
                _ghostSilhouette.SetActive(false);
            }
        }

        private void Start()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.AddListener(ReleaseAndReset);
            }
        }

        private void OnDestroy()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(ReleaseAndReset);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_hapticTimer > 0f)
            {
                _hapticTimer -= dt;
                if (_hapticTimer <= 0f)
                {
                    StopHaptics();
                }
            }

            if (_isLedFlashing)
            {
                _ledResetTimer -= dt;
                if (_ledResetTimer <= 0f)
                {
                    _isLedFlashing = false;
                    SetLedColor(_isLocked ? _acceptedColor : _idleColor);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isLocked) return;

            PowerCell cell = other.GetComponentInParent<PowerCell>();
            if (cell != null && cell.CellType == PowerCellType.TypeB_HexBlue)
            {
                if (_ghostSilhouette != null)
                {
                    _ghostSilhouette.SetActive(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isLocked) return;

            PowerCell cell = other.GetComponentInParent<PowerCell>();
            if (cell != null && cell.CellType == PowerCellType.TypeB_HexBlue)
            {
                if (_ghostSilhouette != null)
                {
                    _ghostSilhouette.SetActive(false);
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_isLocked) return;

            PowerCell cell = other.GetComponentInParent<PowerCell>();
            if (cell == null) return;

            // Case A: Reject Type-A Cell (Amber)
            if (cell.CellType == PowerCellType.TypeA_RoundAmber)
            {
                TriggerRejection();
                return;
            }

            // Case B: Evaluate Type-B Cell (Blue)
            if (cell.CellType == PowerCellType.TypeB_HexBlue)
            {
                // Accept cell in Begin or CellSelection phase (allows immediate socketing on Restart!)
                if (AssessmentManager.Instance != null &&
                    AssessmentManager.Instance.CurrentState != AssessmentState.Begin &&
                    AssessmentManager.Instance.CurrentState != AssessmentState.CellSelection)
                {
                    return;
                }

                // Snap the cell the moment player lets go inside the socket volume
                if (!cell.IsGrabbed)
                {
                    LockCell(cell);
                }
            }
        }

        private void TriggerRejection()
        {
            if (Time.time < _lastRejectionTime + _rejectionCooldown) return;
            _lastRejectionTime = Time.time;

            SetLedColor(_rejectedColor);
            _isLedFlashing = true;
            _ledResetTimer = 0.8f;

            if (_rejectionClip != null)
            {
                _audioSource.PlayOneShot(_rejectionClip);
            }

            TriggerHapticPulse();
            OnCellRejected?.Invoke();
#if UNITY_EDITOR
            Debug.Log("<color=red>[CellSocket] REJECTED: Decoy Type-A cell detected in slot!</color>");
#endif
        }

        private void LockCell(PowerCell cell)
        {
            _isLocked = true;
            _socketedCell = cell;

            // 1. Freeze physics movement
            Rigidbody rb = cell.RigidbodyRef;
            if (rb != null)
            {
                rb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.position = _snapAnchor.position;
                rb.rotation = _snapAnchor.rotation;
            }

            // 2. Make collider a trigger so it never fights the socket mesh
            Collider cellCol = cell.GetComponent<Collider>();
            if (cellCol != null)
            {
                cellCol.isTrigger = true;
            }

            // 3. Disable grabbables so player cannot pull it out
            Grabbable grabbable = cell.GetComponent<Grabbable>();
            if (grabbable != null) grabbable.enabled = false;

            GrabInteractable interactable = cell.GetComponent<GrabInteractable>();
            if (interactable != null) interactable.enabled = false;

            // 4. Disable distance recovery while socketed
            DistanceRecovery recovery = cell.GetComponent<DistanceRecovery>();
            if (recovery != null) recovery.enabled = false;

            // 5. Parent and snap with preserved scale
            Vector3 originalWorldScale = cell.transform.lossyScale;
            _cachedCellOriginalScale = cell.transform.localScale;

            cell.transform.SetParent(_snapAnchor);
            cell.transform.localPosition = Vector3.zero;
            cell.transform.localRotation = Quaternion.identity;

            Vector3 parentLossy = _snapAnchor.lossyScale;
            if (parentLossy.x != 0 && parentLossy.y != 0 && parentLossy.z != 0)
            {
                cell.transform.localScale = new Vector3(
                    originalWorldScale.x / parentLossy.x,
                    originalWorldScale.y / parentLossy.y,
                    originalWorldScale.z / parentLossy.z
                );
            }

            if (_ghostSilhouette != null)
            {
                _ghostSilhouette.SetActive(false);
            }

            // 6. Visual and Audio feedback
            SetLedColor(_acceptedColor);
            if (_socketSnapClip != null)
            {
                _audioSource.PlayOneShot(_socketSnapClip);
            }

            TriggerHapticPulse();
            OnCellAccepted?.Invoke(cell);

            // 7. Advance State Machine
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.NotifyCellSocketed();
            }

#if UNITY_EDITOR
            Debug.Log("<color=green>[CellSocket] ACCEPTED: Type-B cell locked dead-center.</color>");
#endif
        }

        /// <summary>
        /// Resets the socket and frees any locked cell on assessment reset.
        /// </summary>
        public void ReleaseAndReset()
        {
            if (_socketedCell != null)
            {
                // Restore solid collider
                Collider cellCol = _socketedCell.GetComponent<Collider>();
                if (cellCol != null) cellCol.isTrigger = false;

                // Unparent
                _socketedCell.transform.SetParent(null);

                // Restore original local scale
                if (_cachedCellOriginalScale != Vector3.zero)
                {
                    _socketedCell.transform.localScale = _cachedCellOriginalScale;
                }

                // Restore physics
                if (_socketedCell.RigidbodyRef != null)
                {
                    _socketedCell.RigidbodyRef.isKinematic = false;
                }

                // Restore grabbables
                Grabbable grabbable = _socketedCell.GetComponent<Grabbable>();
                if (grabbable != null) grabbable.enabled = true;

                GrabInteractable interactable = _socketedCell.GetComponent<GrabInteractable>();
                if (interactable != null) interactable.enabled = true;

                // Hide ghost silhouette
                if (_ghostSilhouette != null)
                {
                    _ghostSilhouette.SetActive(false);
                }

                // Re-enable and trigger distance recovery
                DistanceRecovery recovery = _socketedCell.GetComponent<DistanceRecovery>();
                if (recovery != null)
                {
                    recovery.enabled = true;
                    recovery.ResetToSpawn();
                }

                _socketedCell = null;
            }

            _isLocked = false;
            _isLedFlashing = false;
            SetLedColor(_idleColor);
            StopHaptics();
        }

        private void SetLedColor(Color color)
        {
            if (_ledRenderer == null) return;
            _propBlock.SetColor(BaseColorId, color);
            _ledRenderer.SetPropertyBlock(_propBlock);
        }

        private void TriggerHapticPulse()
        {
            _hapticTimer = _hapticDuration;
            OVRInput.SetControllerVibration(_hapticFrequency, _hapticAmplitude, OVRInput.Controller.RTouch);
            OVRInput.SetControllerVibration(_hapticFrequency, _hapticAmplitude, OVRInput.Controller.LTouch);
        }

        private void StopHaptics()
        {
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        }
    }
}
