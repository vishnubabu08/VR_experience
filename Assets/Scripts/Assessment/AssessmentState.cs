namespace VRTrainingBay.Assessment
{
    /// <summary>
    /// Represents the sequential phases of the VR technical training bay assessment.
    /// </summary>
    public enum AssessmentState
    {
        Begin = 0,             // Waiting for user to press Start on the welcome UI
        CellSelection = 1,     // User must locate and retrieve Type-B cell (rejects Type-A)
        CellSocketed = 2,      // Type-B cell snapped into the slot
        Calibration = 3,       // User is rotating dial to reach 70-85 degrees
        Calibrated = 4,        // Dial held for 1.0s and locked; Activate button is now armed
        Completed = 5          // Activate pressed; assessment success, restart available
    }
}
