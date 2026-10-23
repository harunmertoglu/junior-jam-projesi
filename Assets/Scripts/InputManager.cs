using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    InputActions inputActions;

    public static event Action<Vector2> OnMove;
    public static event Action<Vector2> OnLook;
    public static event Action<bool> OnSprint;
    public static event Action<bool> OnCrouch;
    public static event Action<bool> OnJump;
    public static event Action<bool> OnAttack;
    public static event Action<bool> OnInteract;
    public static event Action OnPrevious;
    public static event Action OnNext;

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

        inputActions.Player.Sprint.performed += SprintPerformed;
        inputActions.Player.Sprint.canceled += SprintCanceled;

        inputActions.Player.Crouch.performed += CrouchPerformed;
        inputActions.Player.Crouch.canceled += CrouchCanceled;

        inputActions.Player.Jump.performed += JumpPerformed;
        inputActions.Player.Jump.canceled += JumpCanceled;

        inputActions.Player.Attack.performed += AttackPerformed;
        inputActions.Player.Attack.canceled += AttackCanceled;

        inputActions.Player.Interact.performed += InteractPerformed;
        inputActions.Player.Interact.canceled += InteractCanceled;

        inputActions.Player.Previous.performed += PreviousPerformed;

        inputActions.Player.Next.performed += NextPerformed;

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

        inputActions.Player.Sprint.performed -= SprintPerformed;
        inputActions.Player.Sprint.canceled -= SprintCanceled;

        inputActions.Player.Crouch.performed -= CrouchPerformed;
        inputActions.Player.Crouch.canceled -= CrouchCanceled;

        inputActions.Player.Jump.performed -= JumpPerformed;
        inputActions.Player.Jump.canceled -= JumpCanceled;

        inputActions.Player.Attack.performed -= AttackPerformed;
        inputActions.Player.Attack.canceled -= AttackCanceled;

        inputActions.Player.Interact.performed -= InteractPerformed;
        inputActions.Player.Interact.canceled -= InteractCanceled;

        inputActions.Player.Previous.performed -= PreviousPerformed;

        inputActions.Player.Next.performed -= NextPerformed;

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
        OnLook?.Invoke(ctx.ReadValue<Vector2>());
    }
    void LookCanceled(InputAction.CallbackContext ctx)
    {
        OnLook?.Invoke(Vector2.zero);
    }
    #endregion

    #region Sprint
    void SprintPerformed(InputAction.CallbackContext ctx)
    {
        OnSprint?.Invoke(true);
    }
    void SprintCanceled(InputAction.CallbackContext ctx)
    {
        OnSprint?.Invoke(false);
    }
    #endregion

    #region Crouch
    void CrouchPerformed(InputAction.CallbackContext ctx)
    {
        OnCrouch?.Invoke(true);
    }
    void CrouchCanceled(InputAction.CallbackContext ctx)
    {
        OnCrouch?.Invoke(false);
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

    #region Attack
    void AttackPerformed(InputAction.CallbackContext ctx)
    {
        OnAttack?.Invoke(true);
    }
    void AttackCanceled(InputAction.CallbackContext ctx)
    {
        OnAttack?.Invoke(false);
    }
    #endregion

    #region Interact
    void InteractPerformed(InputAction.CallbackContext ctx)
    {
        OnInteract?.Invoke(true);
    }
    void InteractCanceled(InputAction.CallbackContext ctx)
    {
        OnInteract?.Invoke(false);
    }
    #endregion

    #region Previous
    void PreviousPerformed(InputAction.CallbackContext ctx)
    {
        OnPrevious?.Invoke();
    }
    #endregion

    #region Next
    void NextPerformed(InputAction.CallbackContext ctx)
    {
        OnNext?.Invoke();
    }
    #endregion
}
