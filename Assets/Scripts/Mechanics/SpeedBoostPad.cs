using UnityEngine;

// Üstünden geçen oyuncunun yatay hızını gittiği yönde katlar.
// Trigger collider ister. Hız sınırı olduğu için pede tekrar tekrar girmek hızı büyütmez.
public class SpeedBoostPad : MonoBehaviour
{
    // 3 = gelen hızın 3 katı
    [SerializeField] float boostMultiplier = 3f;
    // Boost sonrası yatay hız bunu geçemez
    [SerializeField] float maxBoostSpeed = 20f;
    // Bundan yavaş gelinirse karakterin baktığı yöne bu hızla itilir, durarak girince de boost olsun
    [SerializeField] float minBoostSpeed = 6f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        Rigidbody playerRigidbody = other.attachedRigidbody;
        if (playerMovement == null || playerRigidbody == null)
        {
            return;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        // Gittiğimiz yön. Neredeyse duruyorsak karakterin baktığı yön
        Vector3 boostDirection = horizontalVelocity.magnitude > 0.5f ? horizontalVelocity.normalized : playerMovement.GetFacingDirection();
        boostDirection.y = 0f;
        boostDirection.Normalize();

        float boostSpeed = Mathf.Clamp(horizontalVelocity.magnitude * boostMultiplier, minBoostSpeed, maxBoostSpeed);

        // Zaten sınırdan hızlıysak yavaşlatma
        boostSpeed = Mathf.Max(boostSpeed, horizontalVelocity.magnitude);

        // Dikey hız korunur
        Vector3 boostVelocity = boostDirection * boostSpeed;
        boostVelocity.y = velocity.y;

        // Launch kısa süre kontrolü azaltır, boost hemen sönmez
        playerMovement.Launch(boostVelocity);
    }
}
