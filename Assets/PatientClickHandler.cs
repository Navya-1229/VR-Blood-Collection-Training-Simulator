using UnityEngine;
using UnityEngine.InputSystem;

public class PatientClickHandler : MonoBehaviour
{
    [Tooltip("Drag the PatientController component reference here")]
    public PatientController patientController;

    void Update()
    {
        // New Input System compatible click detection
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Debug.Log("[PatientClickHandler] Mouse clicked somewhere");

        // Cast ray from camera through mouse position
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log("[PatientClickHandler] Raycast hit: " + hit.collider.gameObject.name);

            // Check if the hit object is Chad or any of Chad's children
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                Debug.Log("[PatientClickHandler] CHAD WAS HIT!");

                if (patientController != null)
                    patientController.BeginLieDownSequence();
                else
                    Debug.LogError("[PatientClickHandler] patientController is NULL!");
            }
        }
        else
        {
            Debug.Log("[PatientClickHandler] Raycast hit nothing");
        }
    }
}
