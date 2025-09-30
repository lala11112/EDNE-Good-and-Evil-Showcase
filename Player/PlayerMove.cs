using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || anim == null) return;

        // 1. 입력 처리
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 moveInput = new Vector2(horizontal, vertical);
        
       
        // moveInput의 크기(magnitude)를 계산하여 속도 파라미터로 사용합니다.
        float speed = moveInput.magnitude;

        // 2. 물리 이동 로직
        Vector2 targetVelocity = moveInput * moveSpeed;

        if (speed > 0.1f) // 입력이 있을 때
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else // 입력이 없을 때
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }

        // 속도 제한 적용
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        
        anim.SetFloat("XInput", horizontal);
        anim.SetFloat("YInput", vertical);
        
    
        anim.SetFloat("Speed", speed); 
    }
}