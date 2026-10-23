using System;
using UnityEngine;

// Trigger collider. Oyuncu üstüne gelince basılır ve kapıyı açar.
public class ProgressionSwitch : MonoBehaviour
{
    public static event Action OnSwitchPressed;

    [Header("References")]
    [SerializeField] ProgressionDoor targetDoor;
    [SerializeField] Transform switchVisual;

    [Header("Press")]
    [SerializeField] float pressedDepth = 0.15f;

    bool isPressed;

    void OnTriggerEnter(Collider other)
    {
        if (isPressed || !other.CompareTag("Player"))
        {
            return;
        }

        isPressed = true;

        if (switchVisual != null)
        {
            switchVisual.localPosition += Vector3.down * pressedDepth;
        }

        targetDoor.Open();
        OnSwitchPressed?.Invoke();
    }
}
