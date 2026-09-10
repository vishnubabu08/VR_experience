using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Interaction;
using VRTrainingBay.Assessment;

namespace VRTrainingBay.Calibration
{
    public enum DialAxis { X, Y, Z }

    /// <summary>
    /// Monitors a rotatable dial constrained between 0 and 120 degrees.
    /// Requires holding the dial between 70 and 85 degrees for 1 continuous second to lock.
    /// Allocation-free in Update and event execution paths.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Grabbable))]
    [RequireComponent(typeof(OneGrabRotateTransformer))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CalibrationDial : MonoBehaviour
    {
        [Header("Axis Configuration")]
        [Tooltip("Local axis around which the dial rotates.")]
        [SerializeField] private DialAxis _rotationAxis = DialAxis.Y;

        [Header("Target Calibration Window (Degrees)")]
        [SerializeField] private float _targetMinAngle = 70f;
        [SerializeField] private float _targetMaxAngle = 85f;
        [SerializeField] private float _requiredHoldDuration = 1.0f;

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private Renderer _indicatorRenderer;
        [SerializeField] private Color _idleColor = Color.gray;
        [SerializeField] private Color _calibratingColor = new Color(1f, 0.6f, 0f); // Amber / Orange
        [SerializeField] private Color _lockedColor = Color.green;
        [SerializeField] private TextMeshPro _statusText;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip _calibrationLockedClip;

        [Header("Haptics")]
        [Range(0f, 1f)][SerializeField] private float _hapticAmplitude = 0.7f;
        [Range(0f, 1f)][SerializeField] private float _hapticFrequency = 0.6f;
        [SerializeField] private float _hapticDuration = 0.2f;

        [Header("Events")]
        public UnityEvent<float> OnAngleChanged;
        public UnityEvent OnCalibrationLocked;

        // Cached references & State
        private Grabbable _grabbable;
        private GrabInteractable _grabInteractable;
        private AudioSource _audioSource;
        private Quaternion _initialLocalRotation;
        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private float _holdTimer;
        private float _lastDisplayedAngle = -1f;
        private float _hapticTimer;
        private bool _isLocked;

        public bool IsLocked => _isLocked;
        public float CurrentAngle => GetCurrentAngle();
        public float CalibrationProgress => Mathf.Clamp01(_holdTimer / _requiredHoldDuration);

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            _grabInteractable = GetComponent<GrabInteractable>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f;

            // Auto-find knob renderer so it can change color to orange/green
            if (_indicatorRenderer == null)
            {
                _indicatorRenderer = GetComponentInChildren<Renderer>();
            }

            _initialLocalRotation = transform.localRotation;
            _propBlock = new MaterialPropertyBlock();

            SetDialColor(_idleColor);
            UpdateUI(0f, 0f);
        }


        private void Start()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.AddListener(ResetDial);
            }
        }

        private void OnDestroy()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(ResetDial);
            }
        }

        private void Update()
        {
            // Handle haptic duration
            if (_hapticTimer > 0f)
            {
                _hapticTimer -= Time.deltaTime;
                if (_hapticTimer <= 0f)
                {
                    StopHaptics();
                }
            }

            if (_isLocked) return;

            float currentAngle = GetCurrentAngle();

            // Fire angle changed event if changed
            if (Mathf.Abs(currentAngle - _lastDisplayedAngle) > 0.5f)
            {
                _lastDisplayedAngle = currentAngle;
                OnAngleChanged?.Invoke(currentAngle);
            }

            // Only allow calibration progress if assessment is in Calibration state
            bool isCalibrationState = AssessmentManager.Instance == null ||
                                     AssessmentManager.Instance.CurrentState == AssessmentState.Calibration;

            bool inTargetZone = currentAngle >= _targetMinAngle && currentAngle <= _targetMaxAngle;

            if (isCalibrationState && inTargetZone)
            {
                _holdTimer += Time.deltaTime;
                SetDialColor(_calibratingColor);
                UpdateUI(currentAngle, CalibrationProgress);

#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[CalibrationDial] {gameObject.name} in 70-85° zone! Timer: {_holdTimer:F1}/1.0s</color>");
#endif

                if (_holdTimer >= _requiredHoldDuration)
                {
                    LockCalibration();
                }
            }

            else
            {
                // Continuous requirement: reset timer immediately if dial moves outside 70-85 degrees
                if (_holdTimer > 0f)
                {
                    _holdTimer = 0f;
                    SetDialColor(_idleColor);
                }
                UpdateUI(currentAngle, 0f);
            }
        }

        private float GetCurrentAngle()
        {
            // Automatically measures rotation in degrees across ANY axis (X, Y, or Z)
            return Mathf.Clamp(Quaternion.Angle(_initialLocalRotation, transform.localRotation), 0f, 180f);
        }


        private void LockCalibration()
        {
            _isLocked = true;
            _holdTimer = _requiredHoldDuration;

            // 1. Physically lock dial: disable grabbable so it cannot be rotated anymore
            if (_grabbable != null) _grabbable.enabled = false;
            if (_grabInteractable != null) _grabInteractable.enabled = false;

            // 2. Visual confirmation
            SetDialColor(_lockedColor);
            if (_statusText != null)
            {
                _statusText.text = $"<color=green>LOCKED: {GetCurrentAngle():F0}°</color>";
            }

            // 3. Audio & Haptic confirmation
            if (_calibrationLockedClip != null)
            {
                _audioSource.PlayOneShot(_calibrationLockedClip);
            }
            TriggerHapticPulse();

            OnCalibrationLocked?.Invoke();

/*            // 4. Notify State Machine
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.NotifyCalibrationLocked();
            }*/

#if UNITY_EDITOR
            Debug.Log("<color=green>[CalibrationDial] SUCCESS: Dial locked! Calibration complete.</color>");
#endif
        }

        public void ResetDial()
        {
            _isLocked = false;
            _holdTimer = 0f;
            transform.localRotation = _initialLocalRotation;

            if (_grabbable != null) _grabbable.enabled = true;
            if (_grabInteractable != null) _grabInteractable.enabled = true;

            SetDialColor(_idleColor);
            UpdateUI(0f, 0f);
            StopHaptics();
        }

        private void SetDialColor(Color color)
        {
            if (_indicatorRenderer == null) return;

            _indicatorRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, color);
            _propBlock.SetColor(Shader.PropertyToID("_EmissionColor"), color * 1.5f);
            _indicatorRenderer.SetPropertyBlock(_propBlock);
        }

        private void UpdateUI(float angle, float progress)
        {
            if (_statusText == null) return;

            if (_isLocked) return;

            if (progress > 0f)
            {
                _statusText.text = $"HOLDING: {angle:F0}° ({Mathf.RoundToInt(progress * 100f)}%)";
            }
            else
            {
                _statusText.text = $"DIAL: {angle:F0}° / [70°-85°]";
            }
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
