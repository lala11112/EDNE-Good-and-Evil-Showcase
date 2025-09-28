using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement Settings (Temporary Variables)")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float maxSpeed = 5f; // 최대 속도 제한
    
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    
    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        
        if (rb != null)
        {
            rb.gravityScale = 0f; // 중력 제거
        }
    }
    
    private void FixedUpdate()
    {
        if (rb == null) return;
        
        // 간단한 입력 처리 (WASD, 화살표키 자동 지원)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 moveInput = new Vector2(horizontal, vertical);
        
        Vector2 targetVelocity = moveInput * moveSpeed;
        
        if (moveInput.magnitude > 0.1f)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }
        
        // 속도 제한 적용
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}
