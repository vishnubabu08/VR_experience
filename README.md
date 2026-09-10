# VR Technical Training Bay Assessment - Aonix Practical Round

## Submission Deliverables
- **1. Unity Project & Repository**: [GitHub Repository](https://github.com/vishnubabu08/VR_experience.git) (Branch: `master`)
- **2. Installable Meta Quest APK**: [Download VRTrainingBay_Quest.apk (Google Drive)](https://drive.google.com/file/d/1T0eKln33ZlLE8SAWyRAkYBG5GTaVO8qx/view?usp=sharing)
- **3. Video Demonstration (3–5 min)**: [Watch VR_TrainingVideo (Google Drive)](https://drive.google.com/file/d/1n8ZD9GOZVR6x-WgU6cQQsXbmI5nfsYJm/view?usp=sharing)
- **4. Executive Handoff PDF**: Included in repository at [`Submission_Package/VR_Assessment_Executive_Handoff.pdf`](Submission_Package/VR_Assessment_Executive_Handoff.pdf)

---

## Source Control & Unity Version Control (Plastic SCM)
- **GitHub Repository**: [github.com/vishnubabu08/VR_experience](https://github.com/vishnubabu08/VR_experience)
- **Unity Version Control (Plastic SCM) Spec**:
  - **Server / Organization**: `vishnu08.unity`
  - **Repository Spec**: `plastic://vishnu08.unity/repos/VR_task%2fVR_task`
  - **Branch**: `/main` (Head at Changeset 20)
  - **Account / Owner**: `vishnubabu1108@gmail.com`
  - **Total Checkins**: 21 changesets logged from September 6 to September 10, demonstrating continuous evolutionary development.

### Unity Version Control Checkin History (21 Changesets)
| Changeset | Date | Branch | Author | Development Milestone |
| :---: | :---: | :---: | :---: | :--- |
| **cs:0** | Sep 06, 11:15 | `/main` | `vishnubabu1108` | Initial Unity project workspace setup |
| **cs:1** | Sep 06, 11:18 | `/main` | `vishnubabu1108` | Add packages and project settings to Unity Version Control |
| **cs:2–cs:5** | Sep 07, 09:37 – 15:56 | `/main` | `vishnubabu1108` | Scene baseline layout and Meta XR Core SDK initialization |
| **cs:6–cs:9** | Sep 07, 18:17 – 18:39 | `/main` | `vishnubabu1108` | Interaction SDK setup: Ray pointer UI and hand tracking elements |
| **cs:10–cs:11** | Sep 07, 23:51 | `/main` | `vishnubabu1108` | Rotary calibration dials and angle constraint transformer |
| **cs:12–cs:15** | Sep 08, 09:48 – 18:56 | `/main` | `vishnubabu1108` | Power cell socketing, Type-B hex validation, and Type-A rejection feedback |
| **cs:16–cs:18** | Sep 09, 15:41 – 22:23 | `/main` | `vishnubabu1108` | Blast doors automation, DistanceRecovery safety bounds, tutorial video screen |
| **cs:19–cs:20** | Sep 10, 21:14 – 21:15 | `/main` | `vishnubabu1108` | Zero-GC profiling optimization, restart dial reset, and Meta Quest standalone build |

---

## 1. Project Baseline & Exact Versions
- **Unity Version**: Unity 6 (6000.3.20f1)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Target Platform**: Meta Quest Standalone (Android / ARM64)
- **Scripting Backend**: IL2CPP / .NET Standard 2.1
- **Package Manifest**:
  - `com.meta.xr.sdk.core` (v72.0.0+)
  - `com.meta.xr.sdk.interaction` (v72.0.0+)
  - `com.meta.xr.sdk.interaction.ovr`
  - `com.unity.xr.openxr` (v1.14.0+)
  - `com.unity.render-pipelines.universal` (v17.0.3+)
  - `com.unity.textmeshpro` (v3.0.9+)

---

## 2. How to Open, Run, Build & Control

### Opening & Running
1. Open the project in **Unity 6000.3.20f1**.
2. Open the main scene: `Assets/Scenes/TrainingBay_Main.unity`.
3. Press **Play** in the Unity Editor (tested with Meta Horizon Link / Meta XR Simulator) or deploy APK to a Quest headset.

### Building the APK
1. Go to **File &rarr; Build Settings**.
2. Switch Platform to **Android** (Texture Compression: ASTC).
3. Ensure `Assets/Scenes/TrainingBay_Main.unity` is checked in **Scenes in Build**.
4. Click **Build and Run** (creates an ARM64 APK).

### Control Scheme
- **World-Space UI Interaction**: Right Touch Controller draws a custom laser pointer (`VRControllerRayPointer.cs`). Point at buttons and press **Index Trigger** or **A Button** to click.
- **Grabbing Power Cells**: Use the controller **Grip Button** to pick up and manipulate power cells.
- **Calibrating Dials**: Reach for a dial and press **Grip Button** to rotate the knob.
- **Step-by-Step Task Flow**:
  1. **Begin**: Aim laser pointer at **ACTIVATE** on the UI screen and pull Trigger &rarr; Starts tutorial video on the wall screen.
  2. **Select & Socket**: Retrieve the **Type-B (Hex Key, Blue)** cell from the table and insert it into the console socket (Decoy Type-A Round Amber cell triggers rejection feedback).
  3. **Calibrate**: Rotate all 3 calibration dials into the target band (**70°–85°**) and hold steady for **1.0 continuous second** until they lock green.
  4. **Complete**: When all 3 dials lock, the double blast doors slide open automatically with spatial sound.
  5. **Restart**: Aim at the **RESTART** button and press Trigger &rarr; Cleanly resets all objects, UI, video, doors, and dial positions.

---

## 3. Architecture & State Management

The project is built on decoupled, zero-GC, single-responsibility components:

- **`AssessmentManager.cs`**: Central state coordinator controlling progression (`Begin` &rarr; `CellSelection` &rarr; `CellSocketed` &rarr; `Calibration` &rarr; `Completed`). Communicates via decoupled `UnityEvent` listeners to avoid circular dependencies.
- **`TrainingBayFeedback.cs`**: Contextual UI manager driving the world-space HUD. Displays one primary instruction at a time and toggles Activate vs. Restart controls dynamically.
- **`CellSocket.cs`**: Snapping and validation system for the power cell slot:
  - Validates `PowerCellType.TypeB_HexBlue`.
  - Rejection mechanism: Plays error buzzer, flashes red LED, and pulses haptics when Type-A is inserted.
  - Holographic ghost silhouette illuminates only when the correct cell approaches.
  - Scale-compensated parenting guarantees the battery maintains exact visual scale on snap.
- **`DistanceRecovery.cs`**: 0.2s interval zero-GC monitor attached to cells. If a cell falls through the floor or is thrown > 1.5m from the console, physics velocities are zeroed and it respawns to its initial workstation position.
- **`CalibrationDial.cs` & `TripleDialCoordinator.cs`**: Rotary calibration tracker measuring deviation via `Quaternion.Angle`. Dials lock independently upon holding for 1.0s in 70°–85°. `TripleDialCoordinator` monitors 3/3 dial completion to trigger doors.
- **`RestartButtonHandler.cs`**: Dedicated restart handler attached to the Restart UI button. Snaps dials back to their console socket anchors and resets the `OneGrabRotateTransformer` internal angle so dials always rotate to the right.
- **`SlidingDoubleDoor.cs` & `SocketMover.cs`**: Smooth procedural mechanisms with spatial audio feedback.
- **`TutorialVideoScreen.cs`**: High-performance video screen rendering into a dedicated URP RenderTexture.

---

## 4. Standalone Quest Performance & 3 Key Decisions

Target: **72 Hz locked** on standalone Meta Quest (Snapdragon XR2).

### Three Highest-Impact Optimization Decisions:
1. **Zero Runtime GC Allocations in Core Loop**:
   - Eliminated string concatenations and boxing in `Update()`.
   - Cached all physics vectors, arrays, and audio components in `Awake()`.
   - Used cached non-allocating raycasts and distance checks (`sqrMagnitude` instead of `Vector3.Distance`).
2. **MaterialPropertyBlock for Dynamic Visuals**:
   - Indicator lights, glowing outlines, and dial colors utilize `MaterialPropertyBlock` rather than accessing `renderer.material`.
   - This prevents material cloning in memory, avoids shader recompilation spikes, and preserves GPU SRP batching.
3. **Optimized Physics Constraints & Kinematic Locking**:
   - All console dials, sockets, and moving platforms are kinematic rigidbodies with zero continuous physics simulation overhead.
   - Disabled `throwWhenUnselected` on dials to eliminate PhysX solver depenetration overhead.

---

## 5. Device & Runtime Verification
- **Tested Environment**: Meta Horizon Link + Meta XR Simulator (OpenXR runtime on Unity 6).
- **Observed Performance**: 72+ FPS stable, 0 GC allocations per frame in profiler during active interaction.
- **What Remains Unverified**: Full standalone battery drain profiling on physical Quest 3 hardware over > 30 minutes continuous execution.

---

## 6. Asset Credits & Licences
- **Kenney Prototype Kit & Interface Sounds**: CC0 (Public Domain) - UI sounds and modular tech console trim.
- **Mixkit Audio**: Free Commercial License - High-tech mechanical locking and electrical hum sound effects.
- **Meta XR SDK**: Meta Platform Technologies License (Interaction SDK, Core SDK).
- **All other models, scripts, shaders, and animations**: Authored specifically for this technical training bay challenge.

---

## 7. Known Issues & Future Improvements (With One More Day)
1. **Hand Tracking Synthetic Poses**: Integrate Meta synthetic hand poses (`HandGrabPose`) to provide bespoke finger-wrap poses around the cylindrical dial knobs and battery handle.
2. **Meta XR Haptics SDK (.haptic assets)**: Upgrade controller vibrations from procedural `OVRInput.SetControllerVibration` to pre-authored frequency-modulated `.haptic` audio-haptic clips.
3. **Bake Lightmaps**: Transition real-time spot lighting to baked mixed lightmaps with light probes for even lower GPU fragment cost on Quest 2.

---

## 8. Development Notes
- IDE tooling and code completion assistants were used for boilerplate syntax and math formulas.
- All architecture, interactions, physics, and state flows were engineered, tested, and verified directly in Unity.

