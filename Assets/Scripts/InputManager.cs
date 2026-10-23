using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    InputActions inputActions;

    public static event Action<Vector2> OnMove;
    // bool: girdi fareden mi geldi (fare delta'sı deltaTime ile çarpılmaz, stick çarpılır)
    public static event Action<Vector2, bool> OnLook;
    public static event Action<bool> OnJump;
    public static event Action OnDash;
    public static event Action<bool> OnSprint;
    public static event Action OnPause;

    void Awake()
    {
        AwakingController();
    }

    void OnEnable()
    {
        // Kopya objede inputActions oluşturulmadı, abone olmaya çalışma
        if (inputActions == null)
        {
            return;
        }

        inputActions.Player.Move.performed += MovePerformed;
        inputActions.Player.Move.canceled += MoveCanceled;

        inputActions.Player.Look.performed += LookPerformed;
        inputActions.Player.Look.canceled += LookCanceled;

        inputActions.Player.Jump.performed += JumpPerformed;
        inputActions.Player.Jump.canceled += JumpCanceled;

        inputActions.Player.Dash.performed += DashPerformed;

        inputActions.Player.Sprint.performed += SprintPerformed;
        inputActions.Player.Sprint.canceled += SprintCanceled;

        inputActions.Player.Pause.performed += PausePerformed;

        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        if (inputActions == null)
        {
            return;
        }

        inputActions.Player.Move.performed -= MovePerformed;
        inputActions.Player.Move.canceled -= MoveCanceled;

        inputActions.Player.Look.performed -= LookPerformed;
        inputActions.Player.Look.canceled -= LookCanceled;

        inputActions.Player.Jump.performed -= JumpPerformed;
        inputActions.Player.Jump.canceled -= JumpCanceled;

        inputActions.Player.Dash.performed -= DashPerformed;

        inputActions.Player.Sprint.performed -= SprintPerformed;
        inputActions.Player.Sprint.canceled -= SprintCanceled;

        inputActions.Player.Pause.performed -= PausePerformed;

        inputActions.Player.Disable();
    }

    void OnDestroy()
    {
        if (inputActions == null)
        {
            return;
        }

        inputActions.Disable();
        inputActions.Dispose();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    void AwakingController()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        inputActions = new InputActions();
    }

    #region Move
    void MovePerformed(InputAction.CallbackContext ctx)
    {
        OnMove?.Invoke(ctx.ReadValue<Vector2>());
    }
    void MoveCanceled(InputAction.CallbackContext ctx)
    {
        OnMove?.Invoke(Vector2.zero);
    }
    #endregion

    #region Look
    void LookPerformed(InputAction.CallbackContext ctx)
    {
        bool isMouse = ctx.control.device is Pointer;
        OnLook?.Invoke(ctx.ReadValue<Vector2>(), isMouse);
    }
    void LookCanceled(InputAction.CallbackContext ctx)
    {
        OnLook?.Invoke(Vector2.zero, false);
    }
    #endregion

    #region Jump
    void JumpPerformed(InputAction.CallbackContext ctx)
    {
        OnJump?.Invoke(true);
    }
    void JumpCanceled(InputAction.CallbackContext ctx)
    {
        OnJump?.Invoke(false);
    }
    #endregion

    #region Dash
    void DashPerformed(InputAction.CallbackContext ctx)
    {
        OnDash?.Invoke();
    }
    #endregion

    #region Sprint
    // true = basıldı, false = bırakıldı (basılı tutulduğu sürece koşar)
    void SprintPerformed(InputAction.CallbackContext ctx)
    {
        OnSprint?.Invoke(true);
    }
    void SprintCanceled(InputAction.CallbackContext ctx)
    {
        OnSprint?.Invoke(false);
    }
    #endregion

    #region Pause
    void PausePerformed(InputAction.CallbackContext ctx)
    {
        OnPause?.Invoke();
    }
    #endregion
}
