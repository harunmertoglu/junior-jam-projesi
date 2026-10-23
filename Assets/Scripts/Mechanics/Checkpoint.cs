using UnityEngine;

// Trigger collider. Oyuncu girince buradan devam edilir.
public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        RespawnManager.Instance.SetCheckpoint(transform);
    }
}
