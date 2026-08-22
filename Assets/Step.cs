using UnityEngine;
using UnityEngine.Events;

// One entry in the procedure checklist.
// Make this a plain serializable class so it shows up nicely in the Inspector
// as a list on ProcedureManager.
[System.Serializable]
public class Step
{
    [Tooltip("Short name for your own reference, e.g. 'LieDown', 'Gloves'")]
    public string stepId;

    [TextArea(2, 4)]
    [Tooltip("Text shown in the instruction popup for this step")]
    public string instructionMessage;

    [Tooltip("Objects that should ONLY be active/interactable during this step. " +
             "Everything else stays disabled until its turn.")]
    public GameObject[] objectsToEnable;

    [Tooltip("Optional: objects to disable when this step starts (e.g. hide a 'call patient' button once used)")]
    public GameObject[] objectsToDisable;

    [Tooltip("Fired automatically the moment this step becomes active. " +
             "Hook things like starting an avatar walk animation here from the Inspector.")]
    public UnityEvent onStepStart;

    // Called by ProcedureManager when this step becomes current
    public void Activate()
    {
        foreach (var obj in objectsToEnable)
            if (obj != null) obj.SetActive(true);

        foreach (var obj in objectsToDisable)
            if (obj != null) obj.SetActive(false);

        onStepStart?.Invoke();
    }

    // Called by ProcedureManager when this step is finished and we move to the next
    public void Deactivate()
    {
        foreach (var obj in objectsToEnable)
            if (obj != null) obj.SetActive(false);
    }
}
