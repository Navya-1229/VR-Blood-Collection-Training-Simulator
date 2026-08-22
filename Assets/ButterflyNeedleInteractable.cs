using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class ButterflyNeedleInteractable : MonoBehaviour
{
    [Header("Step ID")]
    public string requiredStepId = "InsertSyringe";

    [Header("References")]
    public Transform  needleTip;
    public Transform  injectionPoint;
    public GameObject needleVisual;
    public Transform  bloodFill;
    public GameObject bloodVFX;

    [Header("Angle Settings")]
    public float minAngle     = 5f;
    public float maxAngle     = 45f;

    [Header("Snap Distance — increase if needle cant reach arm")]
    public float snapDistance = 1.0f;

    [Header("Drag Settings")]
    [Tooltip("Increase this to move needle further from camera")]
    public float dragDepth = 2.0f;

    [Header("Blood Flow")]
    public float bloodStartDelay   = 0.5f;
    public float bloodFillDuration = 4f;

    [Header("Blood Flow - Shader Fill")]
    [Tooltip("The Mesh Renderer that has the tube material slot (e.g. NeedleVisual's renderer)")]
    public Renderer tubeRenderer;
    [Tooltip("Which Materials element index is the tube (check Mesh Renderer > Materials list)")]
    public int tubeMaterialIndex = 0;

    [Header("Cleanup After Collection")]
    [Tooltip("The visible tourniquet object on the patient's arm — will be hidden after collection")]
    public GameObject tourniquetVisual;
    [Tooltip("Delay before needle + tourniquet disappear, after blood collection finishes")]
    public float cleanupDelay = 1f;

    [Header("Final Report")]
    [Tooltip("The UI panel GameObject that shows the end-of-procedure report")]
    public GameObject reportPanel;

    [Tooltip("Text fields on the report card")]
    public TMP_Text reportPatientNameText;
    public TMP_Text reportPatientIdText;
    public TMP_Text reportDateTimeText;
    public TMP_Text reportBloodGroupText;
    public TMP_Text reportVolumeText;
    public TMP_Text reportCollectedByText;

    [Header("Final Report — Values")]
    [Tooltip("Set these to whatever this procedure/patient should show")]
    public string patientName    = "Chad Patient";
    public string patientId      = "PT-00123";
    public string bloodGroupResult = "O+";
    public string volumeCollected  = "10 mL";
    public string collectedByName  = "Student Technician";

    private MaterialPropertyBlock bloodMPB;

    [Header("UI")]
    public TMP_Text angleText;
    public TMP_Text feedbackText;
    public TMP_Text instructionText;

    // Private
    private bool       isPickedUp = false;
    private bool       isInserted = false;
    private Camera     cam;
    private Vector3    startPos;
    private Quaternion startRot;
    private Vector3    bloodStartScale;
    private Collider   myCollider;

    void Start()
    {
        cam      = Camera.main;
        startPos = transform.position;
        startRot = transform.rotation;

        // Auto-add collider if missing
        myCollider = GetComponent<Collider>();
        if (myCollider == null)
        {
            myCollider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("[ButterflyNeedle] Added BoxCollider automatically!");
        }

        if (needleVisual != null) needleVisual.SetActive(false);

        if (bloodFill != null)
        {
            bloodStartScale = bloodFill.localScale;
            bloodFill.localScale = new Vector3(
                bloodStartScale.x, 0f, bloodStartScale.z);
            bloodFill.gameObject.SetActive(false);
        }

        if (bloodVFX != null) bloodVFX.SetActive(false);
        if (angleText    != null) angleText.text    = "";
        if (feedbackText != null) feedbackText.text = "";

        // Shader-based blood fill setup
        bloodMPB = new MaterialPropertyBlock();
        SetBloodFillAmount(0f);

        if (reportPanel != null) reportPanel.SetActive(false);

        Debug.Log("[ButterflyNeedle] Ready! Snap distance: " +
                  snapDistance + " Drag depth: " + dragDepth);
    }

    void Update()
    {
        if (isInserted) return;

        bool isMyStep = ProcedureManager.Instance != null &&
                        ProcedureManager.Instance.IsCurrentStep(requiredStepId);

        if (!isMyStep)
        {
            if (isPickedUp) DropNeedle();
            if (angleText != null) angleText.text = "";
            return;
        }

        // PICK UP
        if (Mouse.current.leftButton.wasPressedThisFrame && !isPickedUp)
        {
            Ray        ray = cam.ScreenPointToRay(
                             Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 500f))
            {
                Debug.Log("[ButterflyNeedle] Hit: " +
                          hit.collider.gameObject.name);

                if (hit.collider.gameObject == gameObject ||
                    hit.collider.transform.IsChildOf(transform))
                {
                    isPickedUp = true;
                    Debug.Log("[ButterflyNeedle] Picked up!");

                    if (instructionText != null)
                        instructionText.text =
                            "Move needle to inner elbow and hold";
                    if (feedbackText != null)
                        feedbackText.text =
                            "Drag toward Chad's inner elbow";
                }
            }
            else
            {
                Debug.Log("[ButterflyNeedle] Hit nothing — " +
                          "check collider and layer.");
            }
        }

        // DRAG + CHECK INSERTION
        if (isPickedUp)
        {
            // Move needle with mouse at specified depth
            Vector2    mousePos   = Mouse.current.position.ReadValue();
            Ray        ray        = cam.ScreenPointToRay(mousePos);
            Vector3    worldPoint = ray.GetPoint(dragDepth);

            // Smooth movement
            transform.position = Vector3.Lerp(
                transform.position, worldPoint, Time.deltaTime * 20f);

            // Check distance to injection point
            if (injectionPoint != null)
            {
                // Use needle tip if available, otherwise needle root
                Vector3 checkPos = needleTip != null
                    ? needleTip.position
                    : transform.position;

                float dist = Vector3.Distance(checkPos,
                                              injectionPoint.position);

                // Show live distance and angle
                Vector3 toInj   = (injectionPoint.position - checkPos).normalized;
                float   angle   = Vector3.Angle(toInj, injectionPoint.up);
                float   surface = Mathf.Abs(90f - angle);

                bool rightAngle  = surface >= minAngle && surface <= maxAngle;
                bool closeEnough = dist <= snapDistance;

                string angleCol = rightAngle ? "green" : "yellow";

                if (angleText != null)
                    angleText.text =
                        $"Distance: <b>{dist:F3}m</b>\n" +
                        $"Snap at: {snapDistance}m\n" +
                        $"Angle: <color={angleCol}>{surface:F1}°</color>\n" +
                        $"({minAngle}°–{maxAngle}° needed)";

                // Feedback based on what's wrong
                if (feedbackText != null)
                {
                    if (!closeEnough)
                        feedbackText.text =
                            $"Move closer to vein ({dist:F2}m away)";
                    else if (!rightAngle)
                        feedbackText.text =
                            $"Adjust angle: {surface:F1}° " +
                            $"(need {minAngle}°–{maxAngle}°)";
                    else
                        feedbackText.text = "✓ Inserting now...";
                }

                Debug.Log($"[ButterflyNeedle] Dist:{dist:F3} " +
                          $"Angle:{surface:F1}° " +
                          $"Close:{closeEnough} RightAngle:{rightAngle}");

                // INSERT — close enough AND right angle
                if (closeEnough && rightAngle)
                {
                    Debug.Log("[ButterflyNeedle] ✓ INSERTING!");
                    StartCoroutine(InsertNeedle(surface));
                    return;
                }
            }

            // Drop on mouse release
            if (Mouse.current.leftButton.wasReleasedThisFrame)
                DropNeedle();
        }
    }

    private void DropNeedle()
    {
        isPickedUp         = false;
        transform.position = startPos;
        transform.rotation = startRot;

        if (angleText    != null) angleText.text    = "";
        if (feedbackText != null)
            feedbackText.text =
                "Pick up needle and drag to inner elbow";

        Debug.Log("[ButterflyNeedle] Dropped.");
    }

    private IEnumerator InsertNeedle(float angle)
    {
        isInserted = true;
        isPickedUp = false;

        // Smoothly snap needle to injection point
        float      t       = 0f;
        Vector3    fromPos = transform.position;
        Quaternion fromRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime / 0.3f;
            transform.position = Vector3.Lerp(
                fromPos, injectionPoint.position, t);
            transform.rotation = Quaternion.Lerp(
                fromRot, injectionPoint.rotation, t);
            yield return null;
        }

        // Hide tray needle WITHOUT disabling the GameObject —
        // disabling the whole object would kill this coroutine
        // (Unity stops all coroutines on a deactivated GameObject),
        // which is why blood flow never used to start.
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        if (myCollider != null) myCollider.enabled = false;

        // Show inserted needle at arm
        if (needleVisual != null)
        {
            needleVisual.transform.position = injectionPoint.position;
            needleVisual.transform.rotation = injectionPoint.rotation;
            needleVisual.SetActive(true);
        }

        if (angleText    != null) angleText.text    = "";
        if (feedbackText != null)
            feedbackText.text = $"✓ Needle inserted at {angle:F1}°!";
        if (instructionText != null)
            instructionText.text = "Blood is flowing into the tube...";

        Debug.Log("[ButterflyNeedle] ✓ Needle inserted! Starting blood flow.");

        yield return new WaitForSeconds(bloodStartDelay);
        yield return StartCoroutine(FlowBlood());
    }

    private IEnumerator FlowBlood()
    {
        // Show VFX
        if (bloodVFX != null) bloodVFX.SetActive(true);

        // Show blood tube
        if (bloodFill != null)
            bloodFill.gameObject.SetActive(true);

        Debug.Log("[ButterflyNeedle] Blood flowing...");

        float elapsed = 0f;
        while (elapsed < bloodFillDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / bloodFillDuration);

            // Drive the shader's _FillAmount so blood creeps along the coiled tube
            SetBloodFillAmount(progress);

            int pct = Mathf.RoundToInt(progress * 100f);
            if (instructionText != null)
                instructionText.text = $"Collecting blood... {pct}%";

            yield return null;
        }

        // Make sure it lands exactly at 100%
        SetBloodFillAmount(1f);

        // Done — freeze here. Fill Amount stays at 1, so blood remains visible
        // in the mesh. Nothing below this point touches _FillAmount again,
        // so the tube will not drain or change further.
        if (bloodVFX != null) bloodVFX.SetActive(false);
        if (feedbackText != null)
            feedbackText.text = "✓ Blood collected!";
        if (instructionText != null)
            instructionText.text = "Removing needle and tourniquet...";

        Debug.Log("[ButterflyNeedle] ✓ Blood collection complete!");

        yield return new WaitForSeconds(cleanupDelay);

        // Remove needle + tourniquet from view
        CleanupAfterCollection();

        yield return new WaitForSeconds(0.5f);

        // Show final report
        ShowReport();

        yield return new WaitForSeconds(1.5f);

        if (ProcedureManager.Instance != null)
            ProcedureManager.Instance.CompleteStep();

        // Safe to fully deactivate now — coroutine has nothing left to run
        gameObject.SetActive(false);
    }

    private void CleanupAfterCollection()
    {
        // Hide the inserted needle/tube visual entirely
        if (needleVisual != null)
            needleVisual.SetActive(false);

        // Hide the tourniquet on the patient's arm
        if (tourniquetVisual != null)
            tourniquetVisual.SetActive(false);

        Debug.Log("[ButterflyNeedle] Needle and tourniquet removed from view.");
    }

    private void ShowReport()
    {
        if (reportPanel != null)
            reportPanel.SetActive(true);

        if (reportPatientNameText != null)
            reportPatientNameText.text = patientName;

        if (reportPatientIdText != null)
            reportPatientIdText.text = patientId;

        if (reportDateTimeText != null)
            reportDateTimeText.text = System.DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");

        if (reportBloodGroupText != null)
            reportBloodGroupText.text = bloodGroupResult;

        if (reportVolumeText != null)
            reportVolumeText.text = volumeCollected;

        if (reportCollectedByText != null)
            reportCollectedByText.text = collectedByName;

        if (instructionText != null)
            instructionText.text = "Procedure complete.";

        Debug.Log($"[ButterflyNeedle] Report shown — Blood Group: {bloodGroupResult}");
    }

    private void SetBloodFillAmount(float t)
    {
        if (tubeRenderer == null)
        {
            Debug.LogWarning("[BloodFill] tubeRenderer is NULL — field not wired in Inspector!");
            return;
        }

        tubeRenderer.GetPropertyBlock(bloodMPB, tubeMaterialIndex);
        bloodMPB.SetFloat("_FillAmount", t);
        tubeRenderer.SetPropertyBlock(bloodMPB, tubeMaterialIndex);

        Debug.Log($"[BloodFill] Set _FillAmount = {t:F2} on {tubeRenderer.gameObject.name}, element {tubeMaterialIndex}");
    }
}
