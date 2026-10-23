using System;
using UnityEngine;

// Trigger collider. Oyuncu tohumla gelince tohum yerine konur ve kapı açılır.
public class SeedAltar : MonoBehaviour
{
    public static event Action OnSeedPlaced;

    [Header("References")]
    [SerializeField] SeasonSeed requiredSeed;
    [SerializeField] ProgressionDoor targetDoor;
    [SerializeField] Transform seedPlacePoint;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || !requiredSeed.IsCarried())
        {
            return;
        }

        requiredSeed.PlaceAt(seedPlacePoint.position);
        targetDoor.Open();
        OnSeedPlaced?.Invoke();
    }
}
