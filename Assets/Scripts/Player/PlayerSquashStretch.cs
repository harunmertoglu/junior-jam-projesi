using UnityEngine;

// Zıplarken uzar, yere inince basılır, dash'te ileri doğru uzar (rig olmadığı için koddan)
public class PlayerSquashStretch : MonoBehaviour
{
    [Header("References")]
    // Ayak hizasındaki pivot (WobblePivot). Ölçek bunun etrafında değişir, ayaklar yerde kalır
    [SerializeField] Transform squashPivotTransform;

    [Header("Squash & Stretch")]
    // x * y * z ≈ 1 olsun, karakter şişmiş ya da küçülmüş görünmesin
    [SerializeField] Vector3 jumpStretchScale = new Vector3(0.85f, 1.3f, 0.85f);
    [SerializeField] Vector3 landSquashScale = new Vector3(1.25f, 0.65f, 1.25f);
    // z = karakterin baktığı yön
    [SerializeField] Vector3 dashStretchScale = new Vector3(0.8f, 0.8f, 1.5f);
    // Normal boyuta dönüş hızı: 10 = ~0.2 sn'de neredeyse tamamen geri döner
    [SerializeField] float returnSpeed = 10f;

    [Header("Landing")]
    // Bu düşüş hızının altında basılma yok (küçük basamaklar, ışınlanma)
    [SerializeField] float minLandingFallSpeed = 3f;
    // Bu düşüş hızında ve üstünde tam basılma
    [SerializeField] float fullLandingFallSpeed = 14f;

    Rigidbody playerRigidbody;
    Vector3 originalScale;
    // 1,1,1 = normal boyut. Asıl ölçek = originalScale * currentSquashScale
    Vector3 currentSquashScale = Vector3.one;
    bool isGrounded;
    // Havadayken ulaşılan en yüksek düşüş hızı
    float airFallSpeed;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        originalScale = squashPivotTransform.localScale;
    }

    void OnEnable()
    {
        PlayerMovement.OnJumped += Jumped;
        PlayerMovement.OnGroundedChanged += GroundedChanged;
        PlayerDash.OnDashStarted += DashStarted;
        PlayerDash.OnRecalled += Teleported;
        RespawnManager.OnPlayerRespawned += Teleported;
    }

    void OnDisable()
    {
        PlayerMovement.OnJumped -= Jumped;
        PlayerMovement.OnGroundedChanged -= GroundedChanged;
        PlayerDash.OnDashStarted -= DashStarted;
        PlayerDash.OnRecalled -= Teleported;
        RespawnManager.OnPlayerRespawned -= Teleported;
    }

    void Jumped()
    {
        currentSquashScale = jumpStretchScale;
    }

    void GroundedChanged(bool value)
    {
        isGrounded = value;

        if (value)
        {
            SetLandingSquash();
            airFallSpeed = 0f;
        }
    }

    void DashStarted(Vector3 dashStartPosition)
    {
        currentSquashScale = dashStretchScale;
    }

    // Işınlanınca hız sıfırlanır, eski düşüş hızıyla basılmasın
    void Teleported()
    {
        airFallSpeed = 0f;
    }

    void Update()
    {
        RecordFallSpeed();
        UpdateSquashScale();
    }

    void RecordFallSpeed()
    {
        if (isGrounded)
        {
            return;
        }

        airFallSpeed = Mathf.Max(airFallSpeed, -playerRigidbody.linearVelocity.y);
    }

    void SetLandingSquash()
    {
        // 0 = hafif iniş (basılma yok), 1 = sert iniş (tam basılma)
        float landingStrength = Mathf.InverseLerp(minLandingFallSpeed, fullLandingFallSpeed, airFallSpeed);

        if (landingStrength <= 0f)
        {
            return;
        }

        currentSquashScale = Vector3.Lerp(Vector3.one, landSquashScale, landingStrength);
    }

    void UpdateSquashScale()
    {
        currentSquashScale = Vector3.Lerp(currentSquashScale, Vector3.one, returnSpeed * Time.deltaTime);
        squashPivotTransform.localScale = Vector3.Scale(originalScale, currentSquashScale);
    }
}
