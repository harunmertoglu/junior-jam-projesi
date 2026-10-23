using UnityEngine;

// Trigger collider. Değince son checkpoint'e dönülür.
// Adanın altındaki düşme alanı, alevler ve alev duvarı bunu kullanır.
public class HazardZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        RespawnManager.Instance.RespawnPlayer();
    }
}
