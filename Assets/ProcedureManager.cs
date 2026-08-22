using UnityEngine;
using TMPro;

public class ProcedureManager : MonoBehaviour
{
    public static ProcedureManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text instructionText;
    public GameObject startButton;

    [Header("Procedure Steps")]
    public Step[] steps;

    private int  currentStepIndex  = -1;
    private bool procedureStarted  = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogError("[ProcedureManager] No steps defined!");
            return;
        }
        if (instructionText == null)
        {
            Debug.LogError("[ProcedureManager] Instruction Text not assigned!");
            return;
        }
        GoToStep(0);
    }

    public void CompleteStep()
    {
        if (!procedureStarted)
        {
            procedureStarted = true;
            if (startButton != null)
                startButton.SetActive(false);
            Debug.Log("[ProcedureManager] Procedure started.");
        }
        GoToStep(currentStepIndex + 1);
    }

    public bool IsCurrentStep(string stepId)
    {
        if (currentStepIndex < 0 || currentStepIndex >= steps.Length)
            return false;
        return steps[currentStepIndex].stepId == stepId;
    }

    public string CurrentStepId =>
        (currentStepIndex >= 0 && currentStepIndex < steps.Length)
            ? steps[currentStepIndex].stepId : "None";

    public int CurrentStepIndex => currentStepIndex;

    private void GoToStep(int index)
    {
        if (currentStepIndex >= 0 && currentStepIndex < steps.Length)
            steps[currentStepIndex].Deactivate();

        currentStepIndex = index;

        if (currentStepIndex >= steps.Length)
        {
            ShowCompletion();
            return;
        }

        Step current = steps[currentStepIndex];

        if (instructionText != null)
            instructionText.text = current.instructionMessage;

        current.Activate();

        // Move camera to correct position for this step
        if (CameraController.Instance != null)
            CameraController.Instance.MoveToStep(current.stepId);

        Debug.Log($"[ProcedureManager] Step {currentStepIndex}: " +
                  $"'{current.stepId}' — {current.instructionMessage}");
    }

    private void ShowCompletion()
    {
        if (instructionText != null)
            instructionText.text = "✓ Procedure Complete! Well done.";
        Debug.Log("[ProcedureManager] All steps completed!");
    }
}
