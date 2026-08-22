using UnityEngine;
using UnityEngine.InputSystem;

public class GloveInteractable : MonoBehaviour
{
    [Header("Step ID — must match ProcedureManager exactly")]
    public string requiredStepId = "Gloves";

    [Header("Doctor hands — shown after wearing gloves")]
    public GameObject doctorGlovedHands;

    [Header("Highlight effect")]
    public GameObject highlightEffect;

    private bool isEquipped = false;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (doctorGlovedHands != null)
            doctorGlovedHands.SetActive(false);
        if (highlightEffect != null)
            highlightEffect.SetActive(false);

        Debug.Log("[GloveInteractable] Started on: " + gameObject.name);
    }

    void Update()
    {
        if (isEquipped) return;
        if (ProcedureManager.Instance == null) return;

        bool isMyStep = ProcedureManager.Instance.IsCurrentStep(requiredStepId);

        // Show highlight during gloves step
        if (highlightEffect != null)
            highlightEffect.SetActive(isMyStep);

        if (!isMyStep) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // Cast ray from camera
        Ray ray = mainCam.ScreenPointToRay(
                      Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, 100f)) return;

        Debug.Log("[GloveInteractable] Ray hit: " + hit.collider.gameObject.name);

        // Check if gloves were clicked
        if (hit.collider.gameObject == gameObject ||
            hit.collider.transform.IsChildOf(transform))
        {
            EquipGloves();
        }
    }

    private void EquipGloves()
    {
        isEquipped = true;
        Debug.Log("[GloveInteractable] Gloves equipped!");

        // Hide gloves on tray
        gameObject.SetActive(false);

        // Show gloved hands
        if (doctorGlovedHands != null)
            doctorGlovedHands.SetActive(true);

        if (highlightEffect != null)
            highlightEffect.SetActive(false);

        // Advance to next step
        ProcedureManager.Instance.CompleteStep();
    }
}
