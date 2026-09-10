using UnityEngine;
using UnityEngine.Events;

namespace VRTrainingBay.Assessment
{
    /// <summary>
    /// Central state coordinator for the VR Technical Training Bay.
    /// Manages state progression: Activate/Tutorial -> Battery -> 3 Dials -> Doors Open & Restart.
    /// Completely allocation-free in Update and transition paths.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class AssessmentManager : MonoBehaviour
    {
        public static AssessmentManager Instance { get; private set; }

        [Header("Runtime State (Read Only)")]
        [SerializeField] private AssessmentState _currentState = AssessmentState.Begin;

        public AssessmentState CurrentState => _currentState;

        [Header("Decoupled Events")]
        public UnityEvent<AssessmentState> OnStateChanged;
        public UnityEvent OnAssessmentStarted;
        public UnityEvent OnCellSocketed;
        public UnityEvent OnCalibrationLocked;
        public UnityEvent OnAssessmentCompleted;
        public UnityEvent OnAssessmentReset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _currentState = AssessmentState.Begin;
        }

        // ==========================================
        // EXTERNAL TRIGGERS
        // ==========================================

        /// <summary>
        /// Called when the player presses the ACTIVATE button to start tutorial and begin.
        /// </summary>
        public void NotifyActivateAttempted()
        {
            if (_currentState == AssessmentState.Begin || _currentState == AssessmentState.CellSelection)
            {
                TransitionTo(AssessmentState.CellSelection);
                OnAssessmentStarted?.Invoke(); // Triggers the tutorial video to play!
#if UNITY_EDITOR
                Debug.Log("<color=cyan>[AssessmentManager] ACTIVATE pressed: Tutorial video started & Assessment began!</color>");
#endif
            }
        }

        /// <summary>
        /// Called when the Type-B cell is snapped into the socket.
        /// </summary>
        public void NotifyCellSocketed()
        {
            TransitionTo(AssessmentState.CellSocketed);
            OnCellSocketed?.Invoke();

            // Transition to dial calibration
            TransitionTo(AssessmentState.Calibration);
        }

        /// <summary>
        /// Called by TripleDialCoordinator once ALL 3 dials are locked.
        /// </summary>
        public void NotifyCalibrationLocked()
        {
            // All 3 dials are locked -> Assessment Completed & Doors Open!
            TransitionTo(AssessmentState.Completed);
            OnCalibrationLocked?.Invoke();
            OnAssessmentCompleted?.Invoke();
#if UNITY_EDITOR
            Debug.Log("<color=green>[AssessmentManager] All 3 dials calibrated! Doors opened & Restart button ready.</color>");
#endif
        }

        /// <summary>
        /// Called by the RESTART button to reset all systems back to start.
        /// </summary>
        public void ResetAssessment()
        {
            TransitionTo(AssessmentState.Begin);
            OnAssessmentReset?.Invoke();
#if UNITY_EDITOR
            Debug.Log("<color=yellow>[AssessmentManager] Full Assessment Reset.</color>");
#endif
        }

        private void TransitionTo(AssessmentState newState)
        {
            _currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
