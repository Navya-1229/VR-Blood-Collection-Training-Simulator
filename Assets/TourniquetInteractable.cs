using UnityEngine;
using UnityEngine.InputSystem;

public class TourniquetInteractable : MonoBehaviour
{
    [Header("Step ID")]
    public string requiredStepId = "Tourniquet";

    [Header("Chad's Upper Arm Target")]
    public GameObject upperArmTarget;

    [Header("Snap radius — how close before it wraps around arm")]
    public float snapRadius = 3.0f;

    [Header("Tourniquet ON arm — position this correctly in Scene view")]
    public GameObject tourniquetOnArm;

    [Header("Visuals")]
    public GameObject highlightEffect;

    [Header("Drag Settings")]
    [Tooltip("Fallback depth used ONLY if upperArmTarget is unassigned. When the target is set, the tourniquet is dragged on the arm's plane so depth always matches the arm.")]
    public float dragDepth = 3.0f;

    [Header("Rotation when held")]
    public Vector3 heldRotation = new Vector3(0f, 0f, 0f);

    private bool   isDragging  = false;
    private bool   isApplied   = false;
    private Camera mainCam;
    private Vector3    originalPosition;
    private Quaternion originalRotation;

    void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void Start()
    {
        mainCam = Camera.main;

        if (highlightEffect != null)
            highlightEffect.SetActive(false);

        if (tourniquetOnArm != null)
            tourniquetOnArm.SetActive(false);

        if (upperArmTarget == null)
            Debug.LogError("[Tourniquet] upperArmTarget NOT assigned!");

        if (tourniquetOnArm == null)
            Debug.LogError("[Tourniquet] tourniquetOnArm NOT assigned!");
    }

    void Update()
    {
        if (isApplied) return;

        bool isMyStep = ProcedureManager.Instance != null &&
                        ProcedureManager.Instance.IsCurrentStep(requiredStepId);

        if (highlightEffect != null)
            highlightEffect.SetActive(isMyStep && !isDragging);

        if (!isMyStep)
        {
            if (isDragging) DropTourniquet();
            return;
        }

        // PICK UP
        if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
        {
            Ray ray = mainCam.ScreenPointToRay(
                Mouse.current.position.ReadValue());
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, 100f))
            {
                Debug.Log("[Tourniquet] Clicked: " + hitInfo.collider.gameObject.name);

                if (hitInfo.collider.gameObject == gameObject ||
                    hitInfo.collider.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    transform.rotation = Quaternion.Euler(heldRotation);
                    if (highlightEffect != null)
                        highlightEffect.SetActive(false);
                    Debug.Log("[Tourniquet] Picked up! Drag to Chad's upper arm.");
                }
            }
        }

        // DRAG
        if (isDragging)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray     ray      = mainCam.ScreenPointToRay(mousePos);

            // ── THE FIX ───────────────────────────────────────────────
            // Drag on a plane that passes through the upper arm target and
            // is parallel to the camera screen. This keeps the dragged
            // tourniquet at the SAME depth as the arm, so when it visually
            // overlaps the arm on screen, the 3D distance is near zero and
            // the snap check actually succeeds.
            Vector3 worldPoint;
            if (upperArmTarget != null &&
                new Plane(mainCam.transform.forward,
                          upperArmTarget.transform.position).Raycast(ray, out float dist))
            {
                worldPoint = ray.GetPoint(dist);
            }
            else
            {
                worldPoint = ray.GetPoint(dragDepth);
            }

            transform.position = worldPoint;
            transform.rotation = Quaternion.Euler(heldRotation);

            // Check distance to arm target
            if (upperArmTarget != null)
            {
                float armDist = Vector3.Distance(
                    transform.position,
                    upperArmTarget.transform.position);

                Debug.Log($"[Tourniquet] Distance to arm: {armDist:F3} " +
                          $"(snaps at < {snapRadius})");

                if (armDist <= snapRadius)
                {
                    Debug.Log("[Tourniquet] Close enough — wrapping around arm!");
                    ApplyTourniquet();
                    return;
                }
            }

            // On mouse release — generous check
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                bool snapped = false;
                if (upperArmTarget != null)
                {
                    float releaseDist = Vector3.Distance(
                        transform.position,
                        upperArmTarget.transform.position);

                    Debug.Log($"[Tourniquet] Released at distance: {releaseDist:F3}");

                    if (releaseDist <= snapRadius * 1.5f)
                    {
                        ApplyTourniquet();
                        snapped = true;
                    }
                }

                if (!snapped)
                    DropTourniquet();
            }
        }
    }

    private void DropTourniquet()
    {
        isDragging         = false;
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        Debug.Log("[Tourniquet] Dropped — drag it closer to Chad's upper arm!");
    }

    private void ApplyTourniquet()
    {
        isApplied  = true;
        isDragging = false;

        // Hide tray tourniquet
        gameObject.SetActive(false);

        // Show the pre-positioned arm tourniquet — no transform overrides
        if (tourniquetOnArm != null)
        {
            tourniquetOnArm.SetActive(true);
            Debug.Log("[Tourniquet] Wrapped around arm at: " +
                      tourniquetOnArm.transform.position);
        }

        if (highlightEffect != null)
            highlightEffect.SetActive(false);

        Debug.Log("[Tourniquet] Applied! Moving to locate vein step.");
        Invoke(nameof(AdvanceStep), 0.5f);
    }

    private void AdvanceStep()
    {
        if (ProcedureManager.Instance != null)
            ProcedureManager.Instance.CompleteStep();
    }
}
