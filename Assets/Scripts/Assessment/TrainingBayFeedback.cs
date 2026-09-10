using UnityEngine;
using TMPro;

namespace VRTrainingBay.Assessment
{
    /// <summary>
    /// Controls UI feedback and toggles Activate vs Restart buttons based on assessment progress.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TrainingBayFeedback : MonoBehaviour
    {
        [Header("UI Status Text")]
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Buttons")]
        [Tooltip("The Activate/Start button.")]
        [SerializeField] private GameObject _activateButton;

        [Tooltip("The Restart button (only appears after doors open).")]
        [SerializeField] private GameObject _restartButton;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _stateTransitionClip;
        [SerializeField] private AudioClip _cellSocketedClip;
        [SerializeField] private AudioClip _activationSuccessClip;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f;

            // Show Activate button, hide Restart button at start
            if (_activateButton != null) _activateButton.SetActive(true);
            if (_restartButton != null) _restartButton.SetActive(false);
        }

        private void Start()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnStateChanged.AddListener(HandleStateChanged);
                AssessmentManager.Instance.OnAssessmentReset.AddListener(HandleReset);
            }

            UpdateStatus("Press ACTIVATE to watch tutorial and begin.");
        }

        private void OnDestroy()
        {
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnStateChanged.RemoveListener(HandleStateChanged);
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(HandleReset);
            }
        }

        private void HandleStateChanged(AssessmentState newState)
        {
            switch (newState)
            {
                case AssessmentState.Begin:
                    UpdateStatus("Press ACTIVATE to watch tutorial and begin.");
                    if (_activateButton != null) _activateButton.SetActive(true);
                    if (_restartButton != null) _restartButton.SetActive(false);
                    break;

                case AssessmentState.CellSelection:
                    UpdateStatus("Tutorial playing... Retrieve & insert Type-B Cell (Green).");
                    if (_activateButton != null) _activateButton.SetActive(false); // Hide Activate while doing task
                    if (_restartButton != null) _restartButton.SetActive(false);
                    PlayAudio(_stateTransitionClip);
                    break;

                case AssessmentState.CellSocketed:
                    UpdateStatus("Cell locked. Initiating calibration sequence...");
                    PlayAudio(_cellSocketedClip);
                    break;

                case AssessmentState.Calibration:
                    UpdateStatus("Rotate and pull all 3 dials into target zone (55° - 105°).");
                    break;

                case AssessmentState.Completed:
                    // All 3 dials are locked and doors have slid open!
                    UpdateStatus("<color=green>ASSESSMENT COMPLETE! Doors Opened.</color>");
                    if (_activateButton != null) _activateButton.SetActive(false);
                    if (_restartButton != null) _restartButton.SetActive(true); // RESTART button appears now!
                    PlayAudio(_activationSuccessClip);
                    break;
            }
        }

        private void HandleReset()
        {
            UpdateStatus("Press ACTIVATE to watch tutorial and begin.");
            if (_activateButton != null) _activateButton.SetActive(true);
            if (_restartButton != null) _restartButton.SetActive(false);
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        private void PlayAudio(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
