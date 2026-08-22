using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class AlcoholSwabInteractable : MonoBehaviour
{
    [Header("Step ID")]
    public string requiredStepId = "AlcoholSwab";

    [Header("Hide this when SwabPad is picked up")]
    public GameObject packetToHide;

    [Header("Arm Target — empty GameObject on Chad's inner elbow")]
    public GameObject armTarget;

    [Header("Cleaning Settings")]
    public float cleaningRadius   = 0.3f;
    public float cleaningDuration = 5f;

    [Header("UI")]
    [Tooltip("Text shown while cleaning — displays countdown / progress")]
    public TMP_Text cleaningProgressText;

    [Header("Visuals")]
    public GameObject cleaningEffect;
    public GameObject highlightEffect;

    [Header("Drag Settings")]
    public float dragDepth = 1.5f;

    [Header("Rotation when held — like holding a swab to wipe")]
    [Tooltip("Rotation of swab while being dragged — adjust to look natural")]
    public Vector3 heldRotation = new Vector3(90f, 0f, 0f);

    [Tooltip("Rotation when sitting on tray")]
    public Vector3 trayRotation = new Vector3(0f, 0f, 0f);

    // Private state
    private bool  isDragging     = false;
    private bool  isCleaning     = false;
    private bool  isDone         = false;
    private float cleanTimer     = 0f;
    private bool  packetWasHidden = false;
    private Camera mainCam;
    private Vector3    originalPosition;
    private Quaternion originalRotation;

    void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Save tray rotation from actual current rotation
        trayRotation = transform.eulerAngles;
    }

    void Start()
    {
        mainCam = Camera.main;
        if (cleaningEffect != null) cleaningEffect.SetActive(false);
        if (highlightEffect != null) highlightEffect.SetActive(false);

        // IMPORTANT: keep the text object's GameObject active at all times.
        // We show/hide it by changing its text content, not by disabling
        // the GameObject — an inactive GameObject never renders, even if
        // you set .text on it.
        if (cleaningProgressText != null)
        {
            cleaningProgressText.gameObject.SetActive(true);
            cleaningProgressText.text = "";
        }
    }

    void Update()
    {
        if (isDone) return;

        bool isMyStep = ProcedureManager.Instance != null &&
                        ProcedureManager.Instance.IsCurrentStep(requiredStepId);

        if (highlightEffect != null)
            highlightEffect.SetActive(isMyStep && !isDragging);

        if (!isMyStep)
        {
            if (isDragging) DropSwab();
            return;
        }

        // ── PICK UP ──────────────────────────────────────────────────────
        if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
        {
            Ray ray = mainCam.ScreenPointToRay(
                          Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.collider.gameObject == gameObject ||
                    hit.collider.transform.IsChildOf(transform))
                {
                    isDragging = true;

                    // Hide packet wrapper
                    if (packetToHide != null && !packetWasHidden)
                    {
                        packetToHide.SetActive(false);
                        packetWasHidden = true;
                    }

                    // Rotate swab to vertical/held position
                    transform.rotation = Quaternion.Euler(heldRotation);

                    if (highlightEffect != null)
                        highlightEffect.SetActive(false);

                    Debug.Log("[AlcoholSwab] Picked up! Drag to patient's inner elbow.");
                }
            }
        }

        // ── DRAG ─────────────────────────────────────────────────────────
        if (isDragging)
        {
            // Follow mouse in 3D space
            Vector2 mousePos   = Mouse.current.position.ReadValue();
            Ray     ray        = mainCam.ScreenPointToRay(mousePos);
            Vector3 worldPoint = ray.GetPoint(dragDepth);
            transform.position = worldPoint;

            // Keep held rotation while dragging
            transform.rotation = Quaternion.Euler(heldRotation);

            // Check if over arm target
            if (armTarget != null)
            {
                float dist = Vector3.Distance(
                    transform.position,
                    armTarget.transform.position);

                if (dist <= cleaningRadius)
                {
                    if (!isCleaning)
                    {
                        isCleaning = true;
                        cleanTimer = 0f;
                        if (cleaningEffect != null)
                            cleaningEffect.SetActive(true);
                        Debug.Log("[AlcoholSwab] Cleaning... hold for " +
                                  cleaningDuration + " seconds!");
                    }

                    cleanTimer += Time.deltaTime;

                    float secondsLeft = Mathf.Max(0f, cleaningDuration - cleanTimer);

                    if (cleaningProgressText != null)
                        cleaningProgressText.text = $"{Mathf.CeilToInt(secondsLeft)}s";

                    if (cleanTimer >= cleaningDuration)
                    {
                        CompleteClean();
                        return;
                    }
                }
                else
                {
                    if (isCleaning)
                    {
                        isCleaning = false;
                        cleanTimer = 0f;
                        if (cleaningEffect != null)
                            cleaningEffect.SetActive(false);
                        if (cleaningProgressText != null)
                            cleaningProgressText.text = "Keep swab on arm to continue cleaning";
                        Debug.Log("[AlcoholSwab] Moved away — keep on arm!");
                    }
                }
            }

            // ── DROP ──────────────────────────────────────────────────────
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                DropSwab();
            }
        }
    }

    private void DropSwab()
    {
        isDragging = false;
        isCleaning = false;
        cleanTimer = 0f;

        if (cleaningEffect != null) cleaningEffect.SetActive(false);
        if (cleaningProgressText != null) cleaningProgressText.text = "";

        // Snap back to tray position AND original rotation
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // Show packet again
        if (packetToHide != null && packetWasHidden)
        {
            packetToHide.SetActive(true);
            packetWasHidden = false;
        }

        Debug.Log("[AlcoholSwab] Dropped — pick up and drag to arm again!");
    }

    private void CompleteClean()
    {
        isDone     = true;
        isDragging = false;
        isCleaning = false;

        if (cleaningEffect != null) cleaningEffect.SetActive(false);
        if (highlightEffect != null) highlightEffect.SetActive(false);
        if (cleaningProgressText != null) cleaningProgressText.text = "✓ Arm cleaned!";

        // Hide the swab visually WITHOUT disabling the GameObject yet —
        // disabling it now would kill the coroutine below before the
        // text gets a chance to clear itself.
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (packetToHide != null) packetToHide.SetActive(false);

        Debug.Log("[AlcoholSwab] Arm cleaned! Moving to tourniquet step.");
        ProcedureManager.Instance.CompleteStep();

        StartCoroutine(ClearTextThenDisable());
    }

    private System.Collections.IEnumerator ClearTextThenDisable()
    {
        // Keep "✓ Arm cleaned!" visible for a moment so it's clearly seen
        yield return new WaitForSeconds(2.5f);

        if (cleaningProgressText != null)
            cleaningProgressText.text = "";

        // Safe to fully deactivate now — nothing left to run
        gameObject.SetActive(false);
    }
}
