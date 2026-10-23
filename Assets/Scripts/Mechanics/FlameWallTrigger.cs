using System;
using UnityEngine;

// Oyuncu bu trigger'a girince yaz ruhu yolu alev duvarıyla kapatır, başka yol bulunmalı.
// Alev duvarı objesinde trigger collider + HazardZone + görsel olur.
public class FlameWallTrigger : MonoBehaviour
{
    public static event Action OnFlameWallOpened;

    [Header("References")]
    [SerializeField] GameObject flameWall;

    bool isTriggered;

    void Start()
    {
        flameWall.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isTriggered || !other.CompareTag("Player"))
        {
            return;
        }

        isTriggered = true;
        flameWall.SetActive(true);
        OnFlameWallOpened?.Invoke();
    }
}
