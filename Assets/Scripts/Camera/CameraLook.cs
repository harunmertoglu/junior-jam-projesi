using Unity.Cinemachine;
using UnityEngine;

// InputManager'dan gelen Look girdisini Cinemachine'in orbit eksenlerine yazar.
// Otomatik geri dönme (Recentering) ve duvar çarpışması Cinemachine ayarlarından gelir.
public class CameraLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CinemachineOrbitalFollow orbitalFollow;

    [Header("Sensitivity")]
    // Fare: piksel başına derece
    [SerializeField] float mouseSensitivity = 0.15f;
    // Gamepad: saniyede derece
    [SerializeField] float gamepadSensitivity = 180f;
    [SerializeField] bool invertY;

    Vector2 lookInput;
    bool isMouseLook;
    bool isPaused;

    void OnEnable()
    {
        InputManager.OnLook += LookInputs;
        PauseMenu.OnPauseChanged += PauseChanged;
    }

    void OnDisable()
    {
        InputManager.OnLook -= LookInputs;
        PauseMenu.OnPauseChanged -= PauseChanged;
    }

    void LookInputs(Vector2 lookInput, bool isMouseLook)
    {
        this.lookInput = lookInput;
        this.isMouseLook = isMouseLook;
    }

    void PauseChanged(bool value)
    {
        isPaused = value;
    }

    void Update()
    {
        if (isPaused)
        {
            return;
        }

        SetCameraRotation();
    }

    void SetCameraRotation()
    {
        // Fare zaten "bu karede ne kadar kaydı" verir; stick -1..1 verir, zamanla çarpılmalı
        Vector2 lookDelta = isMouseLook ? lookInput * mouseSensitivity : lookInput * gamepadSensitivity * Time.deltaTime;
        float verticalDirection = invertY ? 1f : -1f;

        float horizontalValue = orbitalFollow.HorizontalAxis.Value + lookDelta.x;
        float verticalValue = orbitalFollow.VerticalAxis.Value + lookDelta.y * verticalDirection;

        // ClampValue: eksenin Range'i dışına çıkmasın (yatay eksen 360'ta başa sarar)
        orbitalFollow.HorizontalAxis.Value = orbitalFollow.HorizontalAxis.ClampValue(horizontalValue);
        orbitalFollow.VerticalAxis.Value = orbitalFollow.VerticalAxis.ClampValue(verticalValue);
    }
}
