using UnityEngine;

// A ile B arasında ileri geri giden platform.
// Kinematic Rigidbody + MovePosition: fizik motoru oyuncuyu platformla doğru iter.
// Yapı: Platform (bu script + Rigidbody + Collider) -> Visual | TargetPoint
public class MovingPlatform : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] Transform targetPoint;
    [SerializeField] float moveSpeed = 3f;

    Rigidbody platformRigidbody;
    Vector3 startPosition;
    // targetPoint child olduğu için hareketle kayar; Awake'te bir kez sabitliyoruz
    Vector3 targetWorldPosition;
    bool movingToTarget = true;

    void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();
        startPosition = transform.position;
        targetWorldPosition = targetPoint.position;
    }

    void FixedUpdate()
    {
        Vector3 destination = movingToTarget ? targetWorldPosition : startPosition;
        Vector3 toDestination = destination - platformRigidbody.position;
        float stepSize = moveSpeed * Time.fixedDeltaTime;

        if (toDestination.magnitude <= stepSize)
        {
            platformRigidbody.MovePosition(destination);
            movingToTarget = !movingToTarget;
            return;
        }

        platformRigidbody.MovePosition(platformRigidbody.position + toDestination.normalized * stepSize);
    }

    void OnDrawGizmos()
    {
        if (targetPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawSphere(targetPoint.position, 0.2f);
        Gizmos.DrawLine(transform.position, targetPoint.position);
    }
}
