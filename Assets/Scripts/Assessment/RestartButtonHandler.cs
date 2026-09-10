using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using VRTrainingBay.Calibration;
using VRTrainingBay.Cells;

namespace VRTrainingBay.Assessment
{
    /// <summary>
    /// Attached directly to the RESTART Button GameObject.
    /// Cleanly anchors dials and power cells so nothing falls or flips on restart.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RestartButtonHandler : MonoBehaviour
    {
        [Header("Console Dials")]
        [SerializeField] private CalibrationDial[] _dials;

        // Exact socket positions on the console
        private static readonly Vector3 Dial1ConsolePos = new Vector3(1.1414852f, -1.0638616f, -0.12018156f);
        private static readonly Vector3 Dial0ConsolePos = new Vector3(1.1413469f, -1.0589864f, 0.16735363f);
        private static readonly Vector3 Dial2ConsolePos = new Vector3(1.1887112f, -0.41336882f, 0.1823616f);

        // Exact table spawn position for PowerCell_TypeB_HexBlue
        private static readonly Vector3 CellBSpawnPos = new Vector3(-1.76f, -0.838f, 2.82f);
        private static readonly Quaternion CellBSpawnRot = new Quaternion(0.5567052f, 0.43598095f, 0.43598095f, -0.5567052f);

        private Quaternion[] _initialRotations;

        private void Awake()
        {
            if (_dials == null || _dials.Length == 0)
            {
                _dials = FindObjectsByType<CalibrationDial>(FindObjectsSortMode.None);
            }

            if (_dials != null)
            {
                _initialRotations = new Quaternion[_dials.Length];
                for (int i = 0; i < _dials.Length; i++)
                {
                    if (_dials[i] != null)
                    {
                        _initialRotations[i] = _dials[i].transform.localRotation;
                    }
                }
            }

            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(HandleRestartClick);
            }
        }

        public void HandleRestartClick()
        {
            // 1. Reset PowerCell Type-B position & physics so it never falls down
            PowerCell[] cells = FindObjectsByType<PowerCell>(FindObjectsSortMode.None);
            for (int c = 0; c < cells.Length; c++)
            {
                if (cells[c] != null && cells[c].CellType == PowerCellType.TypeB_HexBlue)
                {
                    cells[c].transform.SetParent(null);
                    cells[c].transform.SetPositionAndRotation(CellBSpawnPos, CellBSpawnRot);

                    Rigidbody cellRb = cells[c].GetComponent<Rigidbody>();
                    if (cellRb != null)
                    {
                        cellRb.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
                        cellRb.linearVelocity = Vector3.zero;
#else
                        cellRb.velocity = Vector3.zero;
#endif
                        cellRb.angularVelocity = Vector3.zero;
                        cellRb.position = CellBSpawnPos;
                        cellRb.rotation = CellBSpawnRot;
                    }

                    // Ensure colliders are solid
                    Collider[] cols = cells[c].GetComponents<Collider>();
                    for (int j = 0; j < cols.Length; j++) cols[j].isTrigger = false;

                    // Re-enable interaction
                    Grabbable g = cells[c].GetComponent<Grabbable>();
                    if (g != null) g.enabled = true;
                    GrabInteractable gi = cells[c].GetComponent<GrabInteractable>();
                    if (gi != null) gi.enabled = true;

                    Physics.SyncTransforms();
                    if (cellRb != null) cellRb.isKinematic = false;
                }
            }

            // 2. Reset Dials
            if (_dials == null || _dials.Length == 0)
            {
                _dials = FindObjectsByType<CalibrationDial>(FindObjectsSortMode.None);
            }

            for (int i = 0; i < _dials.Length; i++)
            {
                CalibrationDial dial = _dials[i];
                if (dial == null) continue;

                Rigidbody rb = dial.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector3.zero;
#else
                    rb.velocity = Vector3.zero;
#endif
                    rb.angularVelocity = Vector3.zero;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }

                if (dial.name.Contains("(1)")) dial.transform.position = Dial1ConsolePos;
                else if (dial.name.Contains("(2)")) dial.transform.position = Dial2ConsolePos;
                else dial.transform.position = Dial0ConsolePos;

                if (_initialRotations != null && i < _initialRotations.Length)
                {
                    dial.transform.localRotation = _initialRotations[i];
                }

                OneGrabRotateTransformer transformer = dial.GetComponent<OneGrabRotateTransformer>();
                if (transformer != null)
                {
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    typeof(OneGrabRotateTransformer).GetField("_constrainedRelativeAngle", flags)?.SetValue(transformer, 0f);
                    typeof(OneGrabRotateTransformer).GetField("_relativeAngle", flags)?.SetValue(transformer, 0f);
                    typeof(OneGrabRotateTransformer).GetField("_startAngle", flags)?.SetValue(transformer, 0f);
                }

                Grabbable grabbable = dial.GetComponent<Grabbable>();
                if (grabbable != null)
                {
                    grabbable.InjectOptionalThrowWhenUnselected(false);
                    grabbable.InjectOptionalKinematicWhileSelected(true);
                }
            }

            // 3. Trigger State Reset
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.ResetAssessment();
            }

            Debug.Log("<color=green>[RestartButtonHandler] Dials and Power Cells cleanly reset on tables!</color>");
        }
    }
}
