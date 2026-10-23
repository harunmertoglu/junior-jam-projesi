using UnityEngine;

// Üstüne gelen oyuncuyu geliş yönünün aynası yönünde, geliş hızının bir oranıyla fırlatır.
// Yokuştan kayıp gelirsen ileri-yukarı, tepeden düşersen dümdüz yukarı sektirir.
// PlayerMovement'tan önce çalışır ki oyuncunun hızını frenlenmeden önce kaydedebilsin
[DefaultExecutionOrder(-100)]
public class BouncePad : MonoBehaviour
{
    // 1.2 = çıkış hızı geliş hızının %120'si. Sadece pede ilk gelişte
    [SerializeField, Range(0f, 2f)] float bounceRatio = 1.2f;
    // Başka zemine basmadan pede tekrar inince her sekme bir öncekinin bu oranı kadar olur
    [SerializeField, Range(0f, 1f)] float repeatBounceRatio = 0.8f;
    // Bundan yavaş gelinirse zıplatmaz, üstünde dururken titremesin
    [SerializeField] float minBounceSpeed = 2f;

    Collider padCollider;
    Rigidbody playerRigidbody;
    Collider playerCollider;
    Vector3 playerVelocityBeforeStep;
    bool hasBoosted;
    float lastBounceSpeed;

    void Awake()
    {
        padCollider = GetComponent<Collider>();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
            playerCollider = player.GetComponent<Collider>();
        }
    }

    void FixedUpdate()
    {
        // Pede değmeden bir adım önce zemin kontrolü pedi görür ve normal zemin freni
        // hızı keser. Geliş hızı olarak frenden önceki hızı kullanmak için burada saklıyoruz
        if (playerRigidbody != null)
        {
            playerVelocityBeforeStep = playerRigidbody.linearVelocity;
        }
    }

    void OnEnable()
    {
        PlayerMovement.OnGroundedChanged += PlayerGroundedChanged;
    }

    void OnDisable()
    {
        PlayerMovement.OnGroundedChanged -= PlayerGroundedChanged;
    }

    void PlayerGroundedChanged(bool isGrounded)
    {
        // Pedin dışında bir zemine basınca bir sonraki geliş yine ilk geliş sayılır
        if (isGrounded && playerCollider != null && !IsPlayerAbovePad())
        {
            hasBoosted = false;
        }
    }

    // Zemin kontrolü pede değmeden biraz önce "yere bastı" der.
    // O yüzden çarpışmaya değil, oyuncunun pedin hemen üstünde olup olmadığına bakıyoruz
    bool IsPlayerAbovePad()
    {
        Bounds player = playerCollider.bounds;
        Bounds pad = padCollider.bounds;

        bool isOverPadX = player.max.x > pad.min.x && player.min.x < pad.max.x;
        bool isOverPadZ = player.max.z > pad.min.z && player.min.z < pad.max.z;
        // Hızlı inişte ayaklar pedin içine biraz girebilir, alt sınır pedin tabanı
        bool isNearPadTop = player.min.y > pad.min.y && player.min.y < pad.max.y + 0.5f;

        return isOverPadX && isOverPadZ && isNearPadTop;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Player") || collision.rigidbody == null)
        {
            return;
        }

        // Sadece üst yüzeye değince. Yandan çarpınca zıplatmasın.
        // Ped eğikse de çalışsın diye temasın yönüne bakıyoruz
        Vector3 padUp = transform.up;
        if (Mathf.Abs(Vector3.Dot(collision.GetContact(0).normal, padUp)) < 0.7f)
        {
            return;
        }

        // Çarpışmadan önceki hız. Yönü pede doğru (aşağı) olacak şekilde çevrilir
        Vector3 arrivalVelocity = collision.rigidbody == playerRigidbody ? playerVelocityBeforeStep : collision.relativeVelocity;
        if (Vector3.Dot(arrivalVelocity, padUp) > 0f)
        {
            arrivalVelocity = -arrivalVelocity;
        }

        if (arrivalVelocity.magnitude < minBounceSpeed)
        {
            return;
        }

        float bounceSpeed = arrivalVelocity.magnitude * bounceRatio;
        if (hasBoosted)
        {
            // Tekrar inişte bir önceki sekmeden de yavaş çık. Düşerken yerçekimi daha güçlü
            // olduğu için sadece geliş hızına bakarsak zıplama aynı yükseklikte sonsuza kadar sürer
            bounceSpeed = Mathf.Min(arrivalVelocity.magnitude, lastBounceSpeed) * repeatBounceRatio;
            if (bounceSpeed < minBounceSpeed)
            {
                return;
            }
        }
        hasBoosted = true;
        lastBounceSpeed = bounceSpeed;

        // Geliş yönünün pedin yüzeyine göre aynası: aşağı-ileri gelen, yukarı-ileri gider
        Vector3 bounceDirection = Vector3.Reflect(arrivalVelocity, padUp).normalized;
        Vector3 bounceVelocity = bounceDirection * bounceSpeed;

        PlayerMovement playerMovement = collision.rigidbody.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.Launch(bounceVelocity);
        }
        else
        {
            collision.rigidbody.linearVelocity = bounceVelocity;
        }
    }
}
