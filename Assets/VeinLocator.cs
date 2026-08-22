using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VeinLocator : MonoBehaviour
{
    [Header("Step ID")]
    public string requiredStepId = "LocateVein";

    [Header("X-Ray Panel UI")]
    public GameObject xrayPanel;

    [Header("Vein Buttons")]
    public Button correctVeinButton;
    public Button wrongVeinButton1;
    public Button wrongVeinButton2;

    [Header("UI Text")]
    public TMP_Text feedbackText;
    public TMP_Text instructionText;

    [Header("View Veins Button")]
    public GameObject viewVeinsButton;

    [Header("Wrong vein penalty (seconds)")]
    public float wrongVeinPenalty = 2.0f;

    private bool armIsReady   = false;
    private bool veinsVisible = false;
    private bool veinSelected = false;
    private bool canClick     = true;
    private bool stepStarted  = false;

    void Start()
    {
        if (xrayPanel       != null) xrayPanel.SetActive(false);
        if (viewVeinsButton != null) viewVeinsButton.SetActive(false);
        if (feedbackText    != null) feedbackText.text = "";

        if (correctVeinButton != null)
            correctVeinButton.onClick.AddListener(OnCorrectVeinClicked);
        if (wrongVeinButton1 != null)
            wrongVeinButton1.onClick.AddListener(() =>
                OnWrongVeinClicked("✗ That's the Cephalic vein. Try the center vein."));
        if (wrongVeinButton2 != null)
            wrongVeinButton2.onClick.AddListener(() =>
                OnWrongVeinClicked("✗ That's the Basilic vein. Try the center vein."));
    }

    void Update()
    {
        if (veinSelected) return;

        bool isMyStep = ProcedureManager.Instance != null &&
                        ProcedureManager.Instance.IsCurrentStep(requiredStepId);

        // Hide everything when not on LocateVein step
        if (!isMyStep)
        {
            if (viewVeinsButton != null) viewVeinsButton.SetActive(false);
            if (xrayPanel       != null) xrayPanel.SetActive(false);
            stepStarted = false;
            return;
        }

        // First frame this step becomes active
        if (!stepStarted)
        {
            stepStarted = true;
            Debug.Log("[VeinLocator] LocateVein step started!");

            // Only update instruction text NOW during this step
            if (instructionText != null)
                instructionText.text = armIsReady
                    ? "Click 'View Veins' to see the veins"
                    : "Click on the patient's arm to examine veins";
        }

        // Show View Veins button only on this step
        if (viewVeinsButton != null)
            viewVeinsButton.SetActive(armIsReady && !veinsVisible);
    }

    // Called by PatientController after arm rotates
    public void ArmReady()
    {
        armIsReady = true;
        Debug.Log("[VeinLocator] Arm ready!");

        // Only change UI if we're on the right step
        bool isMyStep = ProcedureManager.Instance != null &&
                        ProcedureManager.Instance.IsCurrentStep(requiredStepId);

        if (!isMyStep) return;

        if (instructionText != null)
            instructionText.text = "Click 'View Veins' to see the veins";

        if (viewVeinsButton != null)
            viewVeinsButton.SetActive(true);
    }

    // Called by ViewVeinsButton OnClick()
    public void ShowVeins()
    {
        if (veinsVisible || !armIsReady) return;

        bool isMyStep = ProcedureManager.Instance != null &&
                        ProcedureManager.Instance.IsCurrentStep(requiredStepId);
        if (!isMyStep) return;

        veinsVisible = true;
        Debug.Log("[VeinLocator] X-Ray panel opening!");

        if (xrayPanel       != null) xrayPanel.SetActive(true);
        if (viewVeinsButton != null) viewVeinsButton.SetActive(false);

        if (instructionText != null)
            instructionText.text = "Select the correct vein for injection";
        if (feedbackText != null)
            feedbackText.text = "Click the Median Cubital Vein (center)";
    }

    private void OnCorrectVeinClicked()
    {
        if (!canClick || veinSelected) return;
        veinSelected = true;
        Debug.Log("[VeinLocator] ✓ Correct vein selected!");

        if (feedbackText    != null) feedbackText.text = "✓ Correct! Median Cubital Vein.";
        if (instructionText != null) instructionText.text = "Now pick up the syringe.";

        Invoke(nameof(HideAndAdvance), 1.5f);
    }

    private void OnWrongVeinClicked(string message)
    {
        if (!canClick || veinSelected) return;
        canClick = false;
        if (feedbackText != null) feedbackText.text = message;
        Invoke(nameof(ResetAfterWrong), wrongVeinPenalty);
    }

    private void ResetAfterWrong()
    {
        canClick = true;
        if (feedbackText != null)
            feedbackText.text = "Click the Median Cubital Vein (center)";
    }

    private void HideAndAdvance()
    {
        if (xrayPanel    != null) xrayPanel.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";
        if (ProcedureManager.Instance != null)
            ProcedureManager.Instance.CompleteStep();
    }
}
