using UnityEngine;

// Yürürken karakteri ayakların etrafında hafifçe sağa sola yatırır (rig olmadığı için)
public class PlayerVisualWobble : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody playerRigidbody;
    // Ayak hizasındaki boş obje, mesh bunun child'ı
    [SerializeField] Transform wobblePivotTransform;

    [Header("Wobble")]
    // En fazla kaç derece yatsın
    [SerializeField] float wobbleAngle = 4f;
    // Sallanma hızı: 10 = bir sağ-sol turu ~0.63 sn
    [SerializeField] float wobbleSpeed = 10f;
    // Bu hızda sallanma tam şiddette olur
    [SerializeField] float fullWobbleRunSpeed = 6f;
    // Sprintte (6 m/sn üstü) sallanma en fazla bu kata çıkar: 1.1 = %10 daha fazla
    [SerializeField] float sprintWobbleMultiplier = 1.1f;
    // Durunca / havaya çıkınca sallanmanın ne kadar çabuk sönmesi
    [SerializeField] float wobbleBlendSpeed = 8f;

    bool isGrounded;
    bool isDashing;
    float wobbleTimer;
    float currentWobbleAmount;

    void OnEnable()
    {
        PlayerMovement.OnGroundedChanged += GroundedChanged;
        PlayerDash.OnDashStarted += DashStarted;
        PlayerDash.OnDashEnded += DashEnded;
    }

    void OnDisable()
    {
        PlayerMovement.OnGroundedChanged -= GroundedChanged;
        PlayerDash.OnDashStarted -= DashStarted;
        PlayerDash.OnDashEnded -= DashEnded;
    }

    void GroundedChanged(bool value)
    {
        isGrounded = value;
    }

    void DashStarted(Vector3 dashStartPosition)
    {
        isDashing = true;
    }

    void DashEnded()
    {
        isDashing = false;
    }

    void Update()
    {
        SetWobble();
    }

    void SetWobble()
    {
        Vector3 playerHorizontalVelocity = playerRigidbody.linearVelocity;
        playerHorizontalVelocity.y = 0f;

        // 0 = duruyor, 1 = normal koşu, 1'in üstü = sprint (en fazla sprintWobbleMultiplier)
        float runPercent = Mathf.Clamp(playerHorizontalVelocity.magnitude / fullWobbleRunSpeed, 0f, sprintWobbleMultiplier);
        float targetWobbleAmount = isGrounded && !isDashing ? runPercent : 0f;

        // Açıyı değil şiddeti yumuşatıyoruz, böylece sallanma ani kesilmez
        currentWobbleAmount = Mathf.Lerp(currentWobbleAmount, targetWobbleAmount, wobbleBlendSpeed * Time.deltaTime);

        // Sallanma varken zaman ilerler, durunca sallanma kaldığı yerden söner
        if (currentWobbleAmount > 0.01f)
        {
            wobbleTimer += Time.deltaTime * wobbleSpeed;
        }

        float currentWobbleAngle = Mathf.Sin(wobbleTimer) * wobbleAngle * currentWobbleAmount;
        wobblePivotTransform.localRotation = Quaternion.Euler(0f, 0f, currentWobbleAngle);
    }
}
