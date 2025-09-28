using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FireBall : MonoBehaviour
{
    [HideInInspector] public float damage;     // SO에서 세팅
    [HideInInspector] public float speed;      // SO에서 세팅
    public float lifeTime = 5f;                // 일정 시간 후 자동 삭제

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;       // 2D 발사체이므로 중력 영향 없도록
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime); // 일정 시간 후 자동 삭제
    }

    // 발사 방향 지정
    public void FireTowards(Transform target)
    {
        if (target == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }

    // 충돌 처리
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Player 태그인지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHp hp = collision.gameObject.GetComponent<PlayerHp>();
            if (hp != null)
            {
                hp.Damage(Mathf.RoundToInt(damage));
            }

            Destroy(gameObject); // Player와 부딪히면 삭제
        }
        // 그 외의 오브젝트는 그냥 무시 (삭제 안 함)
    }
}
