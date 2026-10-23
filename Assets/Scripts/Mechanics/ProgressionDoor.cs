using System;
using System.Collections;
using UnityEngine;

// Şalter ya da tohum sunağı Open() çağırınca yoldan çekilir.
public class ProgressionDoor : MonoBehaviour
{
    public static event Action OnDoorOpened;

    [Header("Open")]
    // Açılınca ne kadar ve hangi yöne kayacağı (varsayılan: yere gömülür)
    [SerializeField] Vector3 openOffset = new Vector3(0f, -4f, 0f);
    [SerializeField] float openDuration = 1f;

    bool isOpen;

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        OnDoorOpened?.Invoke();
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        Vector3 closedPosition = transform.position;
        Vector3 openPosition = closedPosition + openOffset;
        float openTimer = 0f;

        while (openTimer < openDuration)
        {
            openTimer += Time.deltaTime;
            transform.position = Vector3.Lerp(closedPosition, openPosition, openTimer / openDuration);
            yield return null;
        }

        transform.position = openPosition;
    }
}
