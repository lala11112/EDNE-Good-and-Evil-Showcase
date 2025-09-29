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

        // 2. 물리 이동 로직
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

        // 3. 애니메이션 업데이트 (4방향 Bool 값 설정)
        UpdateAnimationWithFourBools(horizontal, vertical);
    }

    private void UpdateAnimationWithFourBools(float horizontalInput, float verticalInput)
    {
        // 모든 Bool 값을 일단 끕니다. (중복 모션 방지)
        anim.SetBool("IsMovingUp", false);
        anim.SetBool("IsMovingDown", false);
        anim.SetBool("IsMovingLeft", false); // ⭐ 새로 추가된 Bool
        anim.SetBool("IsMovingRight", false); // ⭐ 새로 추가된 Bool

        // 3.1. 위아래 입력 처리 (수직 방향 우선)
        if (verticalInput > 0.01f) // 위쪽 (BackMove)
        {
            anim.SetBool("IsMovingUp", true);
        }
        else if (verticalInput < -0.01f) // 아래쪽 (FrontMove)
        {
            anim.SetBool("IsMovingDown", true);
        }

        // 3.2. 좌우 입력 처리 (수평 방향)
        // 위아래 움직임이 강할 경우(큰 값일 경우) 좌우 Bool은 설정하지 않아,
        // 애니메이터가 상하 모션을 우선하도록 할 수 있습니다. 
        // 여기서는 단순함을 위해 입력이 들어오면 Bool을 설정합니다.
        
        if (horizontalInput > 0.01f) // 오른쪽 입력
        {
            anim.SetBool("IsMovingRight", true);
        }
        else if (horizontalInput < -0.01f) // 왼쪽 입력
        {
            anim.SetBool("IsMovingLeft", true);
        }

        
    }
}