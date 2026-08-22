using UnityEngine;

// Add this to the swab to remember its tray position
// so it snaps back if dropped before reaching the arm
public class SwabOriginalPosition : MonoBehaviour
{
    [HideInInspector]
    public Vector3 originalPos;

    void Awake()
    {
        originalPos = transform.position;
    }
}
