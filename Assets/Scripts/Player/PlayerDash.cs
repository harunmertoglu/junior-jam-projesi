using System;
using System.Collections;
using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    // Vector3: dash'in başladığı nokta (geri dönüş noktası)
    public static event Action<Vector3> OnDashStarted;
    public static event Action OnDashEnded;
    public static event Action OnRecalled;
    public static event Action OnRecallPointExpired;

    [Header("References")]
    // Boş bırakılırsa Camera.main kullanılır
    [SerializeField] Transform cameraTransform;

    [Header("Dash")]
    [SerializeField] float dashDistance = 6f;
    [SerializeField] float dashDuration = 0.18f;
    // Dash bitince kalan hız, aniden durmasın
    [SerializeField] float dashExitSpeed = 6f;

    [Header("Recall")]
    // Geri dönüş noktasının kaç saniye kalacağı
    [SerializeField] float defaultRecallDuration = 4f;
    // Sürenin son kaç saniyesinde işaret yanıp sönsün
    [SerializeField] float blinkDuration = 1f;
    [SerializeField] float blinkInterval = 0.15f;
    // Geri dönüş noktasını gösteren placeholder (ör. yassı silindir)
    [SerializeField] GameObject recallMarkerPrefab;

    Rigidbody playerRigidbody;
    PlayerMovement playerMovement;
    GameObject recallMarker;
    Vector3 recallPosition;
    bool hasRecallPoint;
    bool isDashing;
    bool isPlayerGrounded;
    bool isInDialogue;
    Coroutine dashCoroutine;
    Coroutine recallTimerCoroutine;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        InputManager.OnDash += DashInputs;
        PlayerMovement.OnGroundedChanged += GroundedChanged;
        RespawnManager.OnPlayerRespawned += PlayerRespawned;
        DialogueManager.OnDialogueChanged += DialogueChanged;
    }

    void OnDisable()
    {
        InputManager.OnDash -= DashInputs;
        PlayerMovement.OnGroundedChanged -= GroundedChanged;
        RespawnManager.OnPlayerRespawned -= PlayerRespawned;
        DialogueManager.OnDialogueChanged -= DialogueChanged;
    }

    void DialogueChanged(bool isOpen)
    {
        isInDialogue = isOpen;
    }

    void DashInputs()
    {
        // Diyalogda dash ve geri dönüş yok
        if (isDashing || isInDialogue)
        {
            return;
        }

        // Tek tuş: nokta varsa geri dön, yoksa dash at
        if (hasRecallPoint)
        {
            Recall();
        }
        else
        {
            dashCoroutine = StartCoroutine(DashRoutine());
        }
    }

    void GroundedChanged(bool value)
    {
        isPlayerGrounded = value;
    }

    void PlayerRespawned()
    {
        if (isDashing)
        {
            StopCoroutine(dashCoroutine);
            EndDash(Vector3.zero);
        }

        ClearRecallPoint();
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;

        Vector3 dashDirection = GetDashDirection();
        // Karakter dash yönüne (kameranın baktığı yöne) baksın, yoksa geri geri uçabilir
        playerMovement.SetFacingDirection(dashDirection);

        // Dönüş işareti de aynı yöne baksın: karakter az önce dash yönüne çevrildi
        Quaternion recallRotation = Quaternion.LookRotation(playerMovement.GetFacingDirection());
        CreateRecallPoint(playerRigidbody.position, recallRotation);
        OnDashStarted?.Invoke(recallPosition);

        // 6 m / 0.18 sn ≈ 33 m/sn
        float dashSpeed = dashDistance / dashDuration;
        float dashTimer = 0f;

        while (dashTimer < dashDuration)
        {
            // Bounce pad gibi bir şey hızı ele geçirdiyse dash'i kes, hızını ezme
            if (playerMovement.IsLaunched())
            {
                isDashing = false;
                OnDashEnded?.Invoke();
                yield break;
            }

            // Yerçekimi yok, sadece ileri
            playerRigidbody.linearVelocity = dashDirection * dashSpeed;
            dashTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        EndDash(dashDirection * dashExitSpeed);
    }

    Vector3 GetDashDirection()
    {
        // Kameranın baktığı yön = kameradan karaktere doğru
        Vector3 dashDirection = cameraTransform.forward;

        // Yerdeyken aşağı doğru dash zemine girer, o yüzden düzleştiriyoruz
        if (isPlayerGrounded && dashDirection.y < 0f)
        {
            dashDirection.y = 0f;
        }

        // Kamera tam yukarıdan bakıyorsa düzleşince yön kalmaz, karakterin baktığı yöne at
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            return playerMovement.GetFacingDirection();
        }

        return dashDirection.normalized;
    }

    void EndDash(Vector3 exitVelocity)
    {
        isDashing = false;
        playerRigidbody.linearVelocity = exitVelocity;
        OnDashEnded?.Invoke();
    }

    void Recall()
    {
        Vector3 targetPosition = recallPosition;
        ClearRecallPoint();

        playerMovement.TeleportTo(targetPosition);

        OnRecalled?.Invoke();
    }

    void CreateRecallPoint(Vector3 position, Quaternion rotation)
    {
        recallPosition = position;
        hasRecallPoint = true;

        if (recallMarkerPrefab != null)
        {
            recallMarker = Instantiate(recallMarkerPrefab, position, rotation);
        }

        recallTimerCoroutine = StartCoroutine(RecallTimerRoutine());
    }

    void ClearRecallPoint()
    {
        hasRecallPoint = false;

        if (recallTimerCoroutine != null)
        {
            StopCoroutine(recallTimerCoroutine);
            recallTimerCoroutine = null;
        }

        if (recallMarker != null)
        {
            Destroy(recallMarker);
            recallMarker = null;
        }
    }

    IEnumerator RecallTimerRoutine()
    {
        // Şimdilik süre sadece buradan gelir. Ada sistemi kurulunca adaya göre değişecek
        float recallDuration = defaultRecallDuration;

        yield return new WaitForSeconds(Mathf.Max(0f, recallDuration - blinkDuration));

        // Süre bitmek üzere: işaret yanıp söner
        float blinkTimer = 0f;
        while (blinkTimer < blinkDuration)
        {
            if (recallMarker != null)
            {
                recallMarker.SetActive(!recallMarker.activeSelf);
            }

            yield return new WaitForSeconds(blinkInterval);
            blinkTimer += blinkInterval;
        }

        // Kendi coroutine'imizi ClearRecallPoint içinde durdurmayalım
        recallTimerCoroutine = null;
        ClearRecallPoint();
        OnRecallPointExpired?.Invoke();
    }
}
