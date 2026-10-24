using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static event Action OnJumped;
    public static event Action<bool> OnGroundedChanged;
    // Bounce pad gibi bir şey oyuncuyu fırlattı. PlayerDash dinler, dash'i keser
    public static event Action OnLaunched;

    [Header("References")]
    [SerializeField] Transform playerVisualTransform;
    // Boş bırakılırsa Camera.main kullanılır
    [SerializeField] Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] float playerRunSpeed = 6f;
    // Sprint basılıyken koşu hızı bununla çarpılır: 6 * 1.3 = 7.8 m/sn
    [SerializeField] float sprintSpeedMultiplier = 1.3f;
    // Hedef hıza ne kadar çabuk ulaşılacağı. Küçük değer = kayma
    [SerializeField] float groundControl = 12f;
    [SerializeField] float slipperyGroundControl = 1f;
    [SerializeField] float airControl = 6f;
    [SerializeField] float visualRotationSpeed = 15f;

    [Header("Jump")]
    [SerializeField] float jumpHeight = 2.2f;
    [SerializeField] float gravity = 30f;
    // Düşerken ve tuş bırakılınca yerçekimi bununla çarpılır
    [SerializeField] float fallGravityMultiplier = 1.6f;
    [SerializeField] float maxFallSpeed = 25f;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;

    [Header("Slope Slide")]
    // Kaygan eğimde aşağı çeken kuvvetin çarpanı. 0 = eğimde kayma yok
    [SerializeField] float slopeSlideStrength = 1f;
    [SerializeField] float maxSlopeSlideSpeed = 14f;

    [Header("Launch")]
    // Fırlatılınca bu süre boyunca kontrol azalır, boost hemen sönmez
    [SerializeField] float launchDuration = 0.6f;
    [SerializeField] float launchControl = 0.5f;

    [Header("Ground Check")]
    [SerializeField] LayerMask groundLayers;
    [SerializeField] float groundCheckRadius = 0.25f;
    [SerializeField] float groundCheckDistance = 0.15f;

    Rigidbody playerRigidbody;
    Collider playerCollider;
    Vector2 moveInput;
    Vector3 moveDirection;
    Vector3 playerVelocity;
    bool isGrounded;
    bool isDashing;
    bool isJumpHeld;
    bool isSprintHeld;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    float currentSlipperiness;
    float launchEndTime = -10f;
    Vector3 groundNormal = Vector3.up;
    bool isGliding;
    Transform glideCarrier;
    Vector3 glideOffset;
    GlideObject currentGlideObject;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        // Yerçekimini kendimiz uyguluyoruz, dönmeyi de Visual yapıyor
        playerRigidbody.useGravity = false;
        playerRigidbody.freezeRotation = true;
        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        // Dash çok hızlı (33 m/sn), ince duvarların içinden geçmesin
        playerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        InputManager.OnMove += MoveInputs;
        InputManager.OnJump += JumpInputs;
        InputManager.OnSprint += SprintInputs;
        PlayerDash.OnDashStarted += DashStarted;
        PlayerDash.OnDashEnded += DashEnded;
    }

    void OnDisable()
    {
        InputManager.OnMove -= MoveInputs;
        InputManager.OnJump -= JumpInputs;
        InputManager.OnSprint -= SprintInputs;
        PlayerDash.OnDashStarted -= DashStarted;
        PlayerDash.OnDashEnded -= DashEnded;
    }

    void MoveInputs(Vector2 moveInput)
    {
        this.moveInput = moveInput;
    }

    void JumpInputs(bool value)
    {
        isJumpHeld = value;

        if (value)
        {
            lastJumpPressedTime = Time.time;
        }
    }

    void SprintInputs(bool value)
    {
        isSprintHeld = value;
    }

    void DashStarted(Vector3 dashStartPosition)
    {
        if (isGliding)
            DetachFromGlider(currentGlideObject != null ? currentGlideObject.CurrentVelocity : Vector3.zero);
        isDashing = true;
        // Dash basıldıysa ped fırlatması biter, dash her zaman atılabilir
        launchEndTime = -10f;
    }

    void DashEnded()
    {
        isDashing = false;
    }

    void Update()
    {
        SetMoveDirection();
        UpdateVisualRotation();
    }

    void FixedUpdate()
    {
        CheckGround();

        // Dash sırasında hızı PlayerDash yönetir
        if (isDashing)
        {
            return;
        }

        // Glider'a bağlıyken pozisyonu carrier takip eder, hareket/yerçekimi iptal
        if (isGliding)
        {
            TryJump();
            if (!isGliding) return; // zıplayarak ayrıldı

            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.MovePosition(glideCarrier.position + glideOffset);
            return;
        }

        playerVelocity = playerRigidbody.linearVelocity;
        SetPlayerMovement();
        ApplySlopeSlide();
        ApplyGravity();
        TryJump();
        playerRigidbody.linearVelocity = playerVelocity;
    }

    void SetMoveDirection()
    {
        Vector3 cameraDirectionForward = cameraTransform.forward;
        cameraDirectionForward.y = 0f;
        cameraDirectionForward.Normalize();

        Vector3 cameraDirectionRight = cameraTransform.right;
        cameraDirectionRight.y = 0f;
        cameraDirectionRight.Normalize();

        moveDirection = cameraDirectionForward * moveInput.y + cameraDirectionRight * moveInput.x;
    }

    void UpdateVisualRotation()
    {
        if (isDashing || isGliding || moveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        playerVisualTransform.rotation = Quaternion.Slerp(playerVisualTransform.rotation, targetRotation, visualRotationSpeed * Time.deltaTime);
    }

    void CheckGround()
    {
        // Ayak = collider'ın en alt noktası, pivot nerede olursa olsun doğru çalışır
        Vector3 playerFeetPosition = playerCollider.bounds.center;
        playerFeetPosition.y = playerCollider.bounds.min.y;

        // Ayakların biraz üstünden aşağı doğru küre atıyoruz
        Vector3 castOrigin = playerFeetPosition + Vector3.up * (groundCheckRadius + 0.05f);
        RaycastHit groundHit;
        bool hitGround = Physics.SphereCast(castOrigin, groundCheckRadius, Vector3.down, out groundHit, groundCheckDistance + 0.05f, groundLayers, QueryTriggerInteraction.Ignore);

        // Zıplarken yukarı çıkıyorsak zemine değsek bile yerde sayılmayız
        bool newGrounded = hitGround && playerRigidbody.linearVelocity.y <= 0.1f;

        if (newGrounded != isGrounded)
        {
            isGrounded = newGrounded;
            OnGroundedChanged?.Invoke(isGrounded);
        }

        if (isGrounded)
        {
            lastGroundedTime = Time.time;

            GroundSurface groundSurface = groundHit.collider.GetComponent<GroundSurface>();
            currentSlipperiness = groundSurface != null ? groundSurface.GetSlipperiness() : 0f;

            // Zeminin yüzey yönü: düz zeminde (0, 1, 0), eğimde yana yatık
            groundNormal = groundHit.normal;
        }
        else
        {
            groundNormal = Vector3.up;
        }
    }

    void SetPlayerMovement()
    {
        Vector3 horizontalVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
        float currentRunSpeed = isSprintHeld ? playerRunSpeed * sprintSpeedMultiplier : playerRunSpeed;
        Vector3 targetVelocity = moveDirection * currentRunSpeed;

        float control = isGrounded ? Mathf.Lerp(groundControl, slipperyGroundControl, currentSlipperiness) : airControl;
        if (IsLaunched())
        {
            control = launchControl;
        }
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, control * Time.fixedDeltaTime);

        playerVelocity.x = horizontalVelocity.x;
        playerVelocity.z = horizontalVelocity.z;
    }

    void ApplySlopeSlide()
    {
        // Sadece yerdeyken ve kaygan zeminde
        if (!isGrounded || currentSlipperiness <= 0f)
        {
            return;
        }

        // Yerçekimini zemine yansıtınca eğimin aşağı yönü çıkar. Düz zeminde sıfır olur
        Vector3 slopeDownDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal);

        // Bu yönün uzunluğu eğimin sinüsü: 0° = 0, 20° = 0.34, 30° = 0.5
        float slopeSteepness = slopeDownDirection.magnitude;
        if (slopeSteepness < 0.01f)
        {
            return;
        }
        slopeDownDirection.Normalize();

        // Eğim dikleştikçe ve zemin kayganlaştıkça daha çok hızlan
        float slideAcceleration = gravity * slopeSteepness * currentSlipperiness * slopeSlideStrength;
        playerVelocity += slopeDownDirection * slideAcceleration * Time.fixedDeltaTime;

        // Uzun bir yokuşta sonsuza kadar hızlanmasın
        Vector3 horizontalVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
        if (horizontalVelocity.magnitude > maxSlopeSlideSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSlopeSlideSpeed;
            playerVelocity.x = horizontalVelocity.x;
            playerVelocity.z = horizontalVelocity.z;
        }
    }

    void ApplyGravity()
    {
        // Düşerken ya da tuş bırakılınca daha güçlü yerçekimi: kısa basış = alçak zıplama
        // Fırlatılınca tuş basılı sayılır, yükselişi kısalmasın
        bool useFallGravity = playerVelocity.y < 0f || (!isJumpHeld && !IsLaunched());
        float gravityMultiplier = useFallGravity ? fallGravityMultiplier : 1f;

        playerVelocity.y -= gravity * gravityMultiplier * Time.fixedDeltaTime;
        playerVelocity.y = Mathf.Max(playerVelocity.y, -maxFallSpeed);
    }

    void TryJump()
    {
        bool isJumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;

        // Glider'dayken: ayrıl ve objenin yatay momentumunu koruyarak zıpla
        if (isGliding)
        {
            if (!isJumpBuffered) return;
            lastJumpPressedTime = -10f;
            Vector3 carrierVel = currentGlideObject != null ? currentGlideObject.CurrentVelocity : Vector3.zero;
            float jumpVel = Mathf.Sqrt(2f * gravity * jumpHeight);
            DetachFromGlider(new Vector3(carrierVel.x, jumpVel, carrierVel.z));
            OnJumped?.Invoke();
            return;
        }

        bool isInCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;

        if (!isJumpBuffered || !isInCoyoteTime)
        {
            return;
        }

        // h yüksekliğine çıkmak için gereken hız: v = √(2·g·h)
        playerVelocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);

        // Aynı basış ve aynı coyote süresi ikinci kez kullanılmasın
        lastJumpPressedTime = -10f;
        lastGroundedTime = -10f;

        OnJumped?.Invoke();
    }

    bool IsLaunched()
    {
        return Time.time < launchEndTime;
    }

    public void AttachToGlider(Transform carrier, GlideObject glideObject)
    {
        isGliding = true;
        glideCarrier = carrier;
        currentGlideObject = glideObject;
        // Oyuncuyu hangPoint'e ışınla, sonra offset'i hesapla
        playerRigidbody.position = glideObject.GetSnapPosition();
        glideOffset = playerRigidbody.position - carrier.position;
        playerRigidbody.linearVelocity = Vector3.zero;
    }

    public void DetachFromGlider(Vector3 exitVelocity)
    {
        if (!isGliding) return;
        isGliding = false;
        currentGlideObject?.OnPlayerDetached();
        currentGlideObject = null;
        glideCarrier = null;
        playerRigidbody.linearVelocity = exitVelocity;
    }

    // Zıplatma pedi gibi şeyler kullanır. Hızı verir, kısa süre kontrolü azaltır
    public void Launch(Vector3 launchVelocity)
    {
        playerRigidbody.linearVelocity = launchVelocity;
        launchEndTime = Time.time + launchDuration;
        OnLaunched?.Invoke();
    }

    public Vector3 GetFacingDirection()
    {
        return playerVisualTransform.forward;
    }

    // Karakteri anında bu yöne çevirir, sadece yatayda (dash başında kullanılıyor)
    public void SetFacingDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        playerVisualTransform.rotation = Quaternion.LookRotation(direction);
    }

    // Pivot ayakların ne kadar üstünde (pivot ayaktaysa 0)
    public float GetFeetHeight()
    {
        return playerRigidbody.position.y - playerCollider.bounds.min.y;
    }

    public void TeleportTo(Vector3 targetPosition)
    {
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.position = targetPosition;
        transform.position = targetPosition;

        // Yeni yerde zemin bir sonraki FixedUpdate'te tekrar kontrol edilir
        lastGroundedTime = -10f;
        if (isGrounded)
        {
            isGrounded = false;
            OnGroundedChanged?.Invoke(false);
        }
    }
}
