using UnityEngine;
using VRTrainingBay.Assessment;

namespace VRTrainingBay.Cells
{
    /// <summary>
    /// Smoothly slides the socket GameObject to the destination anchor when the battery snaps in.
    /// Allocation-free (zero GC in Update).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SocketMover : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CellSocket _socket;

        [Tooltip("The socket GameObject that will physically move. If empty, moves THIS object.")]
        [SerializeField] private Transform _socketToMove;

        [Tooltip("Destination anchor where the socket slides to (e.g. Move_Anchor).")]
        [SerializeField] private Transform _destinationAnchor;

        [Header("Movement")]
        [SerializeField] private float _moveDuration = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _movementSound;

        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private float _moveProgress = 0f;
        private bool _isMoving = false;
        private bool _isAtTarget = false;

        private void Awake()
        {
            if (_socket == null) _socket = GetComponent<CellSocket>();
            if (_socketToMove == null) _socketToMove = transform;

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.spatialBlend = 1.0f;
                    _audioSource.playOnAwake = false;
                }
            }

            _startPosition = _socketToMove.position;
        }

        private void Start()
        {
            UpdateEndPosition();
        }

        private void UpdateEndPosition()
        {
            if (_destinationAnchor != null)
            {
                _endPosition = _destinationAnchor.position;
            }
            else
            {
                _endPosition = _startPosition + new Vector3(0f, 0f, -0.3f);
            }
        }

        private void OnEnable()
        {
            if (_socket != null)
            {
                _socket.OnCellAccepted.AddListener(HandleCellAccepted);
            }

            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.AddListener(ResetPosition);
            }
        }

        private void OnDisable()
        {
            if (_socket != null)
            {
                _socket.OnCellAccepted.RemoveListener(HandleCellAccepted);
            }

            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.OnAssessmentReset.RemoveListener(ResetPosition);
            }
        }

        private void HandleCellAccepted(PowerCell cell)
        {
            StartMoving();
        }

        [ContextMenu("Test Move Socket")]
        public void StartMoving()
        {
            if (_isAtTarget) return;

            UpdateEndPosition();
            _isMoving = true;
            _moveProgress = 0f;

            if (_audioSource != null && _movementSound != null)
            {
                _audioSource.PlayOneShot(_movementSound);
            }
        }

        private void Update()
        {
            if (!_isMoving) return;

            _moveProgress += Time.deltaTime / Mathf.Max(_moveDuration, 0.01f);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_moveProgress));
            _socketToMove.position = Vector3.Lerp(_startPosition, _endPosition, t);

            if (_moveProgress >= 1f)
            {
                _isMoving = false;
                _isAtTarget = true;
                _socketToMove.position = _endPosition;
            }
        }

        public void ResetPosition()
        {
            _isMoving = false;
            _isAtTarget = false;
            _moveProgress = 0f;
            if (_socketToMove != null)
            {
                _socketToMove.position = _startPosition;
            }
        }
    }
}
