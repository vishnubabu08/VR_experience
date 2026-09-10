using UnityEngine;
using Oculus.Interaction;

namespace VRTrainingBay.Visuals
{
    /// <summary>
    /// Lightweight, zero-allocation emission glow on hover/grab for Meta ISDK interactables.
    /// Safe when objects start inactive, get disabled, or during scene exit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GrabbableGlow : MonoBehaviour
    {
        [Header("Interactable Source")]
        [SerializeField] private GrabInteractable _grabInteractable;

        [Header("Renderer to Glow")]
        [SerializeField] private Renderer _targetRenderer;

        [Header("Shader Property")]
        [SerializeField] private string _colorPropertyName = "_EmissionColor";

        [Header("Glow Colors")]
        [ColorUsage(true, true)][SerializeField] private Color _normalColor = Color.black;
        [ColorUsage(true, true)][SerializeField] private Color _hoverColor = new Color(0f, 0.9f, 1f) * 2.0f; // Bright Cyan glow
        [ColorUsage(true, true)][SerializeField] private Color _selectColor = Color.white * 2.0f;

        private MaterialPropertyBlock _propBlock;
        private int _propertyId;

        private void Awake()
        {
            if (_grabInteractable == null)
            {
                _grabInteractable = GetComponent<GrabInteractable>();
            }

            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponentInChildren<Renderer>();
            }

            _propBlock = new MaterialPropertyBlock();
            _propertyId = Shader.PropertyToID(_colorPropertyName);

            ApplyColor(_normalColor);
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.WhenStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.WhenStateChanged -= HandleStateChanged;
            }
            ApplyColor(_normalColor);
        }

        private void HandleStateChanged(InteractableStateChangeArgs args)
        {
            switch (args.NewState)
            {
                case InteractableState.Hover:
                    // Glow ONLY before picking (when hand is hovering near)
                    ApplyColor(_hoverColor);
                    break;

                case InteractableState.Select:
                case InteractableState.Normal:
                case InteractableState.Disabled:
                default:
                    // Turn OFF glow immediately when grabbed (picked) or idle
                    ApplyColor(_normalColor);
                    break;
            }
        }
        private void ApplyColor(Color color)
        {
            if (_targetRenderer == null) return;

            _targetRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(_propertyId, color);
            _targetRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
