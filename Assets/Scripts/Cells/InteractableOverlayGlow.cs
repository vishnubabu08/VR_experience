using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

namespace VRTrainingBay.Visuals
{
    /// <summary>
    /// Spawns a glowing silhouette outline around interactables that blinks to attract the user.
    /// Never alters the original material/textures of the object.
    /// Turns off when grabbed (picked).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableOverlayGlow : MonoBehaviour
    {
        [Header("Overlay Material")]
        [Tooltip("Assign M_Glow_Cyan or M_Glow_Amber.")]
        [SerializeField] private Material _overlayMaterial;

        [Header("Interactable Source")]
        [SerializeField] private GrabInteractable _grabInteractable;

        private readonly List<GameObject> _overlayInstances = new List<GameObject>();

        private void Awake()
        {
            if (_grabInteractable == null)
            {
                _grabInteractable = GetComponent<GrabInteractable>();
            }

            // Find all mesh filters on this object and its children (handles dials and cells)
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                // Create a lightweight child GameObject dedicated to the glowing outline
                GameObject overlay = new GameObject($"{mf.name}_GlowOverlay");
                overlay.transform.SetParent(mf.transform, false);
                overlay.transform.localPosition = Vector3.zero;
                overlay.transform.localRotation = Quaternion.identity;
                overlay.transform.localScale = Vector3.one;

                MeshFilter overlayFilter = overlay.AddComponent<MeshFilter>();
                overlayFilter.sharedMesh = mf.sharedMesh;

                MeshRenderer overlayRenderer = overlay.AddComponent<MeshRenderer>();
                overlayRenderer.sharedMaterial = _overlayMaterial;
                overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                overlayRenderer.receiveShadows = false;

                _overlayInstances.Add(overlay);
            }
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.WhenStateChanged += HandleStateChanged;
            }
            SetOverlaysVisible(true);
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.WhenStateChanged -= HandleStateChanged;
            }
            SetOverlaysVisible(false);
        }

        private void HandleStateChanged(InteractableStateChangeArgs args)
        {
            switch (args.NewState)
            {
                case InteractableState.Select:
                    // Player grabbed (picked) the object -> turn off outline!
                    SetOverlaysVisible(false);
                    break;

                case InteractableState.Hover:
                case InteractableState.Normal:
                default:
                    // Player let go or is hovering -> keep blinking outline active!
                    SetOverlaysVisible(true);
                    break;
            }
        }

        private void SetOverlaysVisible(bool visible)
        {
            for (int i = 0; i < _overlayInstances.Count; i++)
            {
                if (_overlayInstances[i] != null)
                {
                    _overlayInstances[i].SetActive(visible);
                }
            }
        }
    }
}
