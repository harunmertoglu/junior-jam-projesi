using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static event Action OnJumped;
    public static event Action<bool> OnGroundedChanged;

    [Header("References")]
    [SerializeField] Transform playerVisualTransform;
    // Boş bırakılırsa Camera.main kullanılır
    [SerializeField] Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] float playerRunSpeed = 6f;
    // Hedef hıza ne kadar çabuk ulaşılacağı. Küçük değer = kayma
    [SerializeField] float groundControl = 12f;
    [SerializeField] float slipperyGroundControl = 1.5f;
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

    [Header("Ground Check")]
    [SerializeField] LayerMask groundLayers;
    [SerializeField] float groundCheckRadius = 0.25f;
    [SerializeField] float groundCheckDistance = 0.15f;

    Rigidbody playerRigidbody;
    Vector2 moveInput;
    Vector3 moveDirection;
    Vector3 playerVelocity;
    bool isGrounded;
    bool isDashing;
    bool isJumpHeld;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    float currentSlipperiness;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();

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
        PlayerDash.OnDashStarted += DashStarted;
        PlayerDash.OnDashEnded += DashEnded;
    }

    void OnDisable()
    {
        InputManager.OnMove -= MoveInputs;
        InputManager.OnJump -= JumpInputs;
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

        playerVelocity = playerRigidbody.linearVelocity;
        SetPlayerMovement();
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
        if (isDashing || moveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        playerVisualTransform.rotation = Quaternion.Slerp(playerVisualTransform.rotation, targetRotation, visualRotationSpeed * Time.deltaTime);
    }

    void CheckGround()
    {
        // Ayakların biraz üstünden aşağı doğru küre atıyoruz
        Vector3 castOrigin = playerRigidbody.position + Vector3.up * (groundCheckRadius + 0.05f);
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
        }
    }

    void SetPlayerMovement()
    {
        Vector3 horizontalVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
        Vector3 targetVelocity = moveDirection * playerRunSpeed;

        float control = isGrounded ? Mathf.Lerp(groundControl, slipperyGroundControl, currentSlipperiness) : airControl;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, control * Time.fixedDeltaTime);

        playerVelocity.x = horizontalVelocity.x;
        playerVelocity.z = horizontalVelocity.z;
    }

    void ApplyGravity()
    {
        // Düşerken ya da tuş bırakılınca daha güçlü yerçekimi: kısa basış = alçak zıplama
        bool useFallGravity = playerVelocity.y < 0f || !isJumpHeld;
        float gravityMultiplier = useFallGravity ? fallGravityMultiplier : 1f;

        playerVelocity.y -= gravity * gravityMultiplier * Time.fixedDeltaTime;
        playerVelocity.y = Mathf.Max(playerVelocity.y, -maxFallSpeed);
    }

    void TryJump()
    {
        bool isJumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
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

    public Vector3 GetFacingDirection()
    {
        return playerVisualTransform.forward;
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
