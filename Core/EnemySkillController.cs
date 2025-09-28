using UnityEngine;

public class EnemySkillController : MonoBehaviour
{
    [Header("스킬 데이터들 (SO)")]
    public EnemySkillData[] skills; // 여러 스킬 관리

    [Header("플레이어 감지 관련")]
    public float detectionRange = 5f; 
    private Transform player;

    private float[] cooldownTimers;

    private void Start()
    {
        // 플레이어 Transform 가져오기
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 쿨타임 배열 초기화
        cooldownTimers = new float[skills.Length];  
    }

    private void Update()
    {
        if (player == null) return;

        // 플레이어와의 거리 계산
        float distance = Vector2.Distance(transform.position, player.position);

        // 감지 범위 안이면 스킬 발사
        if (distance <= detectionRange)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                cooldownTimers[i] -= Time.deltaTime;

                if (cooldownTimers[i] <= 0)
                {
                    FireSkill(i);
                    cooldownTimers[i] = skills[i].cooldownTime;
                }
            }
        }
    }

    // 스킬 발사 담당
    private void FireSkill(int index)
    {
        EnemySkillData skill = skills[index];

        if (skill.skillEffectPrefab != null && player != null)
        {
            // 스킬 프리팹 생성
            GameObject projObj = Instantiate(skill.skillEffectPrefab, transform.position, Quaternion.identity);

            // Rigidbody2D 가져오기
            Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 발사 직후 플레이어 방향으로 속도 설정
                Vector2 dir = (player.position - transform.position).normalized;
                rb.linearVelocity = dir * skill.projectileSpeed;
            }
        }
    }
}
