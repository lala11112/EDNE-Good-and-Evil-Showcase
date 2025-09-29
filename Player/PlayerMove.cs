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
        
        // 🚨 새로 추가된 부분: 이동 속도 계산
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

        // 3. ✨ 애니메이션 업데이트 (블렌드 트리 방식)
        // A. 방향 정보 전달 (블렌드 트리의 혼합 비율 제어)
        anim.SetFloat("XInput", horizontal);
        anim.SetFloat("YInput", vertical);
        
        // B. 속도 정보 전달 (Idle <-> Locomotion 상태 전환 제어)
        anim.SetFloat("Speed", speed); 
    }
}