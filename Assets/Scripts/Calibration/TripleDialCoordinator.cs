using UnityEngine;
using UnityEngine.Events;
using VRTrainingBay.Assessment;

namespace VRTrainingBay.Calibration
{
    /// <summary>
    /// Monitors all 3 calibration dials and triggers the double door once ALL 3 are locked.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TripleDialCoordinator : MonoBehaviour
    {
        [Header("The 3 Calibration Dials")]
        [SerializeField] private CalibrationDial _dial1;
        [SerializeField] private CalibrationDial _dial2;
        [SerializeField] private CalibrationDial _dial3;

        [Header("Events")]
        [Tooltip("Triggered when all 3 dials are locked.")]
        public UnityEvent OnAllDialsLocked;

        private int _lockedDialsCount = 0;
        private bool _allCompleted = false;

        private void Start()
        {
            if (_dial1 != null) _dial1.OnCalibrationLocked.AddListener(HandleDialLocked);
            if (_dial2 != null) _dial2.OnCalibrationLocked.AddListener(HandleDialLocked);
            if (_dial3 != null) _dial3.OnCalibrationLocked.AddListener(HandleDialLocked);

            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.AddListener(ResetCoordinator);
            }
        }

        private void OnDestroy()
        {
            if (_dial1 != null) _dial1.OnCalibrationLocked.RemoveListener(HandleDialLocked);
            if (_dial2 != null) _dial2.OnCalibrationLocked.RemoveListener(HandleDialLocked);
            if (_dial3 != null) _dial3.OnCalibrationLocked.RemoveListener(HandleDialLocked);

            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(ResetCoordinator);
            }
        }

        private void HandleDialLocked()
        {
            if (_allCompleted) return;

            _lockedDialsCount++;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[TripleDialCoordinator] Progress: {_lockedDialsCount}/3 dials locked.</color>");
#endif

            if (_lockedDialsCount >= 3)
            {
                _allCompleted = true;

                // 1. Trigger the doors to open
                OnAllDialsLocked?.Invoke();

                // 2. Advance the state machine to Calibrated!
                if (AssessmentManager.Instance != null)
                {
                    AssessmentManager.Instance.NotifyCalibrationLocked();
                }

#if UNITY_EDITOR
                Debug.Log("<color=green>[TripleDialCoordinator] ALL 3 DIALS LOCKED! Opening doors & advancing assessment.</color>");
#endif
            }
        }
        [ContextMenu("Test Force All Dials Locked")]
        [ContextMenu("Test Force All 3 Dials Locked")]
        public void TestForceAllLocked()
        {
            _allCompleted = true;
            OnAllDialsLocked?.Invoke();
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.NotifyCalibrationLocked();
            }
        }


        private void ResetCoordinator()
        {
            _lockedDialsCount = 0;
            _allCompleted = false;
        }
    }
}
