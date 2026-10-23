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

    [Header("Dash")]
    [SerializeField] float dashDistance = 6f;
    [SerializeField] float dashDuration = 0.18f;
    // Dash bitince kalan hız, aniden durmasın
    [SerializeField] float dashExitSpeed = 6f;

    [Header("Recall")]
    // SeasonManager yoksa kullanılır
    [SerializeField] float defaultRecallDuration = 10f;
    // Sürenin son kaç saniyesinde işaret yanıp sönsün
    [SerializeField] float blinkDuration = 2f;
    [SerializeField] float blinkInterval = 0.15f;
    // Geri dönüş noktasını gösteren placeholder (ör. yassı silindir)
    [SerializeField] GameObject recallMarkerPrefab;

    Rigidbody playerRigidbody;
    PlayerMovement playerMovement;
    GameObject recallMarker;
    Vector3 recallPosition;
    bool hasRecallPoint;
    bool canDash = true;
    bool isDashing;
    bool isPlayerGrounded;
    Coroutine dashCoroutine;
    Coroutine recallTimerCoroutine;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void OnEnable()
    {
        InputManager.OnDash += DashInputs;
        PlayerMovement.OnGroundedChanged += GroundedChanged;
        RespawnManager.OnPlayerRespawned += PlayerRespawned;
    }

    void OnDisable()
    {
        InputManager.OnDash -= DashInputs;
        PlayerMovement.OnGroundedChanged -= GroundedChanged;
        RespawnManager.OnPlayerRespawned -= PlayerRespawned;
    }

    void DashInputs()
    {
        if (isDashing)
        {
            return;
        }

        // Tek tuş: nokta varsa geri dön, yoksa dash at
        if (hasRecallPoint)
        {
            Recall();
        }
        else if (canDash)
        {
            dashCoroutine = StartCoroutine(DashRoutine());
        }
    }

    void GroundedChanged(bool value)
    {
        isPlayerGrounded = value;

        // Yere basınca dash hakkı geri gelir
        if (value && !isDashing)
        {
            canDash = true;
        }
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
        canDash = false;

        Vector3 dashDirection = playerMovement.GetFacingDirection();
        CreateRecallPoint(playerRigidbody.position);
        OnDashStarted?.Invoke(recallPosition);

        // 6 m / 0.18 sn ≈ 33 m/sn
        float dashSpeed = dashDistance / dashDuration;
        float dashTimer = 0f;

        while (dashTimer < dashDuration)
        {
            // Yerçekimi yok, sadece ileri
            playerRigidbody.linearVelocity = dashDirection * dashSpeed;
            dashTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        EndDash(dashDirection * dashExitSpeed);
    }

    void EndDash(Vector3 exitVelocity)
    {
        isDashing = false;
        playerRigidbody.linearVelocity = exitVelocity;

        // Havada bittiyse yere inene kadar yeni dash yok
        canDash = isPlayerGrounded;

        OnDashEnded?.Invoke();
    }

    void Recall()
    {
        Vector3 targetPosition = recallPosition;
        ClearRecallPoint();

        playerMovement.TeleportTo(targetPosition);

        // Havadaki bir noktaya dönüldüyse yere inmeden dash yok.
        // Yerdeyse bir sonraki zemin kontrolünde hak geri gelir.
        canDash = false;

        OnRecalled?.Invoke();
    }

    void CreateRecallPoint(Vector3 position)
    {
        recallPosition = position;
        hasRecallPoint = true;

        if (recallMarkerPrefab != null)
        {
            recallMarker = Instantiate(recallMarkerPrefab, position, Quaternion.identity);
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
        float recallDuration = SeasonManager.Instance != null ? SeasonManager.Instance.GetRecallDuration() : defaultRecallDuration;

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
