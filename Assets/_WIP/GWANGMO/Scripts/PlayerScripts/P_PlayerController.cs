using UnityEngine;
using UnityEngine.InputSystem;

public class P_PlayerController : MonoBehaviour
{
    //기본 움직임 속도
    public float moveSpeed = 5f;
    // 달리기 속도 비율: 1.3
    public float runMultiplier = 1.3f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private P_Portal currentPortal;

    // Player 상태
    private bool isRunning;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
            isRunning = true;
        else if (context.canceled)
            isRunning = false;
    }

    void FixedUpdate()
    {
        float speed = isRunning ? moveSpeed * runMultiplier : moveSpeed;
        rb.linearVelocity = new Vector2(moveInput.x * speed, rb.linearVelocity.y);
    }

    // 상호작용 F Key
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && currentPortal != null)
        {
            currentPortal.Interact();
            //Debug.Log("상호작용 실행");
        }
    }

    // Portal 관련 상호작용
    void OnTriggerEnter2D(Collider2D collision)
    {
        P_Portal portal = collision.GetComponent<P_Portal>();
        if (portal != null)
        {
            currentPortal = portal;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        P_Portal portal = collision.GetComponent<P_Portal>();
        if (portal != null)
        {
            currentPortal = null;
        }
    }
}