using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;

namespace VRTrainingBay.Cells
{
    /// <summary>
    /// Identifies and handles power cell interactions.
    /// Allocation-free caching for Meta XR Interaction SDK grabbables.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Grabbable))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PowerCell : MonoBehaviour
    {
        [Header("Cell Properties")]
        [SerializeField] private PowerCellType _cellType = PowerCellType.TypeB_HexBlue;

        [Header("Events")]
        [Tooltip("Fires when this cell is grabbed.")]
        public UnityEvent<PowerCell> OnCellGrabbed;

        [Tooltip("Fires when this cell is released.")]
        public UnityEvent<PowerCell> OnCellReleased;

        private Grabbable _grabbable;
        private Rigidbody _rigidbody;
        private bool _wasGrabbed;

        public PowerCellType CellType => _cellType;
        public Rigidbody RigidbodyRef => _rigidbody;
        public bool IsGrabbed => _grabbable != null && _grabbable.SelectingPointsCount > 0;

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            bool currentlyGrabbed = IsGrabbed;

            if (currentlyGrabbed && !_wasGrabbed)
            {
                _wasGrabbed = true;
                OnCellGrabbed?.Invoke(this);
            }
            else if (!currentlyGrabbed && _wasGrabbed)
            {
                _wasGrabbed = false;
                OnCellReleased?.Invoke(this);
            }
        }
    }
}
