using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using VRTrainingBay.Calibration;

namespace VRTrainingBay.Assessment
{
    /// <summary>
    /// Attached directly to the RESTART Button GameObject.
    /// Runs ONLY when you click the RESTART button.
    /// 1. Snaps dials back to their console sockets.
    /// 2. Resets rotation transformer back to 0 so you always rotate to the RIGHT.
    /// 3. Leaves AssessmentManager and CalibrationDial 100% untouched.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RestartButtonHandler : MonoBehaviour
    {
        [Header("Console Dials")]
        [Tooltip("Assign the 3 dials here, or leave empty to auto-find in scene.")]
        [SerializeField] private CalibrationDial[] _dials;

        // Exact socket positions on the console
        private static readonly Vector3 Dial1ConsolePos = new Vector3(1.1414852f, -1.0638616f, -0.12018156f);
        private static readonly Vector3 Dial0ConsolePos = new Vector3(1.1413469f, -1.0589864f, 0.16735363f);
        private static readonly Vector3 Dial2ConsolePos = new Vector3(1.1887112f, -0.41336882f, 0.1823616f);

        // Saved starting orientations
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

        /// <summary>
        /// Runs ONLY when the RESTART button is clicked.
        /// </summary>
        public void HandleRestartClick()
        {
            if (_dials == null || _dials.Length == 0)
            {
                _dials = FindObjectsByType<CalibrationDial>(FindObjectsSortMode.None);
            }

            for (int i = 0; i < _dials.Length; i++)
            {
                CalibrationDial dial = _dials[i];
                if (dial == null) continue;

                // 1. Freeze physics so it never falls
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

                // 2. Snap position to console socket
                if (dial.name.Contains("(1)"))
                {
                    dial.transform.position = Dial1ConsolePos;
                }
                else if (dial.name.Contains("(2)"))
                {
                    dial.transform.position = Dial2ConsolePos;
                }
                else
                {
                    dial.transform.position = Dial0ConsolePos;
                }

                // 3. Reset rotation back to starting angle
                if (_initialRotations != null && i < _initialRotations.Length)
                {
                    dial.transform.localRotation = _initialRotations[i];
                }

                // 4. CRITICAL: Reset OneGrabRotateTransformer internal angle to 0
                // This ensures you rotate to the RIGHT side every single time!
                OneGrabRotateTransformer transformer = dial.GetComponent<OneGrabRotateTransformer>();
                if (transformer != null)
                {
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    typeof(OneGrabRotateTransformer).GetField("_constrainedRelativeAngle", flags)?.SetValue(transformer, 0f);
                    typeof(OneGrabRotateTransformer).GetField("_relativeAngle", flags)?.SetValue(transformer, 0f);
                    typeof(OneGrabRotateTransformer).GetField("_startAngle", flags)?.SetValue(transformer, 0f);
                }

                // 5. Disable throwing so releasing never drops it
                Grabbable grabbable = dial.GetComponent<Grabbable>();
                if (grabbable != null)
                {
                    grabbable.InjectOptionalThrowWhenUnselected(false);
                    grabbable.InjectOptionalKinematicWhileSelected(true);
                }
            }

            // 6. Trigger the normal AssessmentManager reset
            if (AssessmentManager.Instance != null)
            {
                AssessmentManager.Instance.ResetAssessment();
            }

            Debug.Log("<color=green>[RestartButtonHandler] Dials and rotation angles reset: You can now rotate to the RIGHT side!</color>");
        }
    }
}
