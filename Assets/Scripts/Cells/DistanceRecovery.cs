using UnityEngine;
using VRTrainingBay.Assessment;

namespace VRTrainingBay.Cells
{
    /// <summary>
    /// Monitors cell distance from a reference anchor (e.g. Console).
    /// If dropped > maxDistance away or below floor level, resets to its initial spawn transform.
    /// Automatically handles full reset on AssessmentManager reset event.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PowerCell))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DistanceRecovery : MonoBehaviour
    {
        [Header("Recovery Bounds")]
        [Tooltip("Anchor to measure distance from (typically the Console table). If null, uses initial spawn point.")]
        [SerializeField] private Transform _referenceAnchor;

        [Tooltip("Maximum allowed distance from anchor before respawning (meters).")]
        [SerializeField] private float _maxDistance = 1.5f;

        [Tooltip("Y-position threshold below which the object is immediately recovered.")]
        [SerializeField] private float _floorKillY = -0.5f;

        [Header("Check Interval (Performance)")]
        [Tooltip("Time interval between distance evaluations to minimize CPU overhead.")]
        [SerializeField] private float _checkInterval = 0.2f;

        private PowerCell _powerCell;
        private Rigidbody _rigidbody;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _maxDistanceSqr;
        private float _timer;

        private void Awake()
        {
            _powerCell = GetComponent<PowerCell>();
            _rigidbody = GetComponent<Rigidbody>();

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _maxDistanceSqr = _maxDistance * _maxDistance;
        }

        private void Start()
        {
            // Subscribe to global reset event if AssessmentManager is in the scene
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.AddListener(ResetToSpawn);
            }
        }

        private void OnDestroy()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(ResetToSpawn);
            }
        }

        private void Update()
        {
            // Never recover while player is holding the object
            if (_powerCell.IsGrabbed) return;

            _timer += Time.deltaTime;
            if (_timer < _checkInterval) return;
            _timer = 0f;

            EvaluateRecovery();
        }

        private void EvaluateRecovery()
        {
            // 1. Fallback floor check
            if (transform.position.y < _floorKillY)
            {
                ResetToSpawn();
                return;
            }

            // 2. Distance check from reference anchor
            Vector3 anchorPos = _referenceAnchor != null ? _referenceAnchor.position : _spawnPosition;
            float sqrDist = (transform.position - anchorPos).sqrMagnitude;

            if (sqrDist > _maxDistanceSqr)
            {
                ResetToSpawn();
            }
        }

        /// <summary>
        /// Completely zeroes physics velocities and snaps back to original spawn transform.
        /// </summary>
        public void ResetToSpawn()
        {
#if UNITY_6000_0_OR_NEWER
            _rigidbody.linearVelocity = Vector3.zero;
#else
            _rigidbody.velocity = Vector3.zero;
#endif
            _rigidbody.angularVelocity = Vector3.zero;

            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        }
    }
}
