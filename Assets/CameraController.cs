using UnityEngine;
using System.Collections;

// Attach this to Main Camera
// It smoothly moves the camera to the best angle for each procedure step
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [System.Serializable]
    public class CameraPosition
    {
        public string stepId;
        [Tooltip("Where the camera moves to for this step")]
        public Vector3 position;
        [Tooltip("Where the camera looks for this step")]
        public Vector3 rotation;
    }

    [Header("Camera positions for each step")]
    public CameraPosition[] cameraPositions;

    [Header("Transition speed")]
    public float moveSpeed = 2.0f;

    private Vector3    targetPosition;
    private Quaternion targetRotation;
    private bool       isMoving = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // Smoothly move camera to target
        if (isMoving)
        {
            transform.position = Vector3.Lerp(
                transform.position, targetPosition, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Lerp(
                transform.rotation, targetRotation, Time.deltaTime * moveSpeed);

            // Stop when close enough
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isMoving = false;
            }
        }
    }

    // Call this from ProcedureManager when step changes
    public void MoveToStep(string stepId)
    {
        foreach (var cam in cameraPositions)
        {
            if (cam.stepId == stepId)
            {
                targetPosition = cam.position;
                targetRotation = Quaternion.Euler(cam.rotation);
                isMoving = true;
                Debug.Log($"[CameraController] Moving to position for step: {stepId}");
                return;
            }
        }
        Debug.Log($"[CameraController] No camera position defined for step: {stepId}");
    }
}
