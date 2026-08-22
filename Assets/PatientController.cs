using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PatientController : MonoBehaviour
{
    [Header("Target Points")]
    public Transform chairApproachPoint;
    public Transform chairLiePosition;

    [Header("Animator Parameter Names")]
    public string walkBoolParam  = "IsWalking";
    public string lieDownTrigger = "LieDown";

    [Header("Step ID")]
    public string lieDownStepId = "LieDown";

    [Header("Sit-down animation length (seconds)")]
    public float lieDownAnimDuration = 2.0f;

    [Header("Arm Rotation")]
    public string  forearmBoneName    = "mixamorig:LeftForeArm";
    public float   armRotateDelay     = 0.5f;
    public Vector3 armExposedRotation = new Vector3(90f, 0f, 0f);

    // Private state
    private NavMeshAgent agent;
    private Animator     animator;
    private bool isWalking        = false;
    private bool hasArrived       = false;
    private bool procedureStarted = false;
    private bool armRotated       = false;

    private Vector3    lockedPosition;
    private Quaternion lockedRotation;
    private Transform  forearmBone;

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        lockedPosition = transform.position;
        lockedRotation = transform.rotation;

        if (animator != null)
            animator.applyRootMotion = false;
        if (agent != null)
            agent.isStopped = true;
    }

    void Start()
    {
        ForceToStartPosition();

        // Find forearm bone
        forearmBone = FindBone(forearmBoneName);
        if (forearmBone == null)
            Debug.LogError("[PatientController] Bone NOT FOUND: '" +
                           forearmBoneName + "' — check exact name in Hierarchy!");
        else
            Debug.Log("[PatientController] ✓ Found bone: " + forearmBone.name);
    }

    // Use LateUpdate to override animation AFTER it runs each frame
    // This is KEY — LateUpdate runs after Animator updates bone positions
    void LateUpdate()
    {
        // Lock position before procedure
        if (!procedureStarted)
        {
            transform.position = lockedPosition;
            transform.rotation = lockedRotation;
        }

        // Walking — sync with NavMesh
        if (isWalking && agent.enabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance)
                ArriveAtChair();
        }

        // ALWAYS override arm rotation after it's been set
        // LateUpdate runs AFTER animator — so this wins every frame
        if (armRotated && forearmBone != null)
        {
            forearmBone.localRotation =
                Quaternion.Euler(armExposedRotation);
        }
    }

    // Called by PatientClickHandler
    public void BeginLieDownSequence()
    {
        Debug.Log("[PatientController] BeginLieDownSequence called.");

        if (ProcedureManager.Instance == null)
        {
            Debug.LogError("[PatientController] ProcedureManager not found!");
            return;
        }

        if (!ProcedureManager.Instance.IsCurrentStep(lieDownStepId))
        {
            Debug.LogWarning("[PatientController] Wrong step. Current='" +
                             ProcedureManager.Instance.CurrentStepId +
                             "' Expected='" + lieDownStepId + "'");
            return;
        }

        if (isWalking || hasArrived) return;

        if (!agent.isOnNavMesh)
        {
            agent.Warp(lockedPosition);
            if (!agent.isOnNavMesh)
            {
                Debug.LogError("[PatientController] NOT on NavMesh!");
                return;
            }
        }

        procedureStarted     = true;
        isWalking            = true;
        agent.isStopped      = false;
        agent.updateRotation = true;
        agent.SetDestination(chairApproachPoint.position);
        animator.SetBool(walkBoolParam, true);
        Debug.Log("[PatientController] Walking to chair.");
    }

    private void ForceToStartPosition()
    {
        transform.position = lockedPosition;
        transform.rotation = lockedRotation;
        if (agent != null && agent.isOnNavMesh)
            agent.Warp(lockedPosition);
    }

    private void ArriveAtChair()
    {
        if (hasArrived) return;
        Debug.Log("[PatientController] Arrived at chair.");

        isWalking  = false;
        hasArrived = true;

        animator.SetBool(walkBoolParam, false);
        agent.isStopped = true;
        agent.enabled   = false;

        if (chairLiePosition != null)
        {
            transform.position = chairLiePosition.position;
            transform.rotation = chairLiePosition.rotation;
        }

        StartCoroutine(LockPositionDuringSit());
        animator.SetTrigger(lieDownTrigger);
        Invoke(nameof(FinishLieDown), lieDownAnimDuration);
    }

    private System.Collections.IEnumerator LockPositionDuringSit()
    {
        Vector3    sitPos = chairLiePosition.position;
        Quaternion sitRot = chairLiePosition.rotation;
        float elapsed = 0f;
        while (elapsed < lieDownAnimDuration)
        {
            transform.position = sitPos;
            transform.rotation = sitRot;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void FinishLieDown()
    {
        Debug.Log("[PatientController] Sitting complete. " +
                  "Rotating arm in " + armRotateDelay + "s...");
        Invoke(nameof(RotateArmToExposeElbow), armRotateDelay);

        if (ProcedureManager.Instance != null)
            ProcedureManager.Instance.CompleteStep();
    }

    private void RotateArmToExposeElbow()
    {
        armRotated = true; // LateUpdate will now enforce rotation every frame

        if (forearmBone != null)
        {
            forearmBone.localRotation = Quaternion.Euler(armExposedRotation);
            Debug.Log("[PatientController] ✓ Arm rotated to: " +
                      armExposedRotation + " — inner elbow exposed!");

            // Tell VeinLocator arm is ready
            VeinLocator vl = FindFirstObjectByType<VeinLocator>();
            if (vl != null) vl.ArmReady();
        }
        else
        {
            Debug.LogError("[PatientController] Bone is null! " +
                           "Cannot rotate arm. Check bone name: '" +
                           forearmBoneName + "'");
        }
    }

    private Transform FindBone(string boneName)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
            if (t.name == boneName) return t;
        return null;
    }
}
