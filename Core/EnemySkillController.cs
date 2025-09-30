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
        
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 쿨타임 배열 초기화
        cooldownTimers = new float[skills.Length];  
    }

    private void Update()
    {
        if (player == null) return;

        // 플레이어와의 거리 계산
        float distance = Vector2.Distance(transform.position, player.position);

        // 감지 범위 안이면 스킬 발사 작동이 되었다 안됬다 합니다. 왠지 모르지만 가끔 오류가 생기는.. 고쳐봅시다.
        if (distance <= detectionRange)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                cooldownTimers[i] -= Time.deltaTime;

                if (cooldownTimers[i] <= 0)
                {
                    UseSkill(i);
                    cooldownTimers[i] = skills[i].cooldownTime;
                }
            }
        }
    }

    
    private void UseSkill(int index)
    {
        EnemySkillData skill = skills[index];

        if (skill.skillEffectPrefab != null && player != null)
        {
          
            GameObject projObj = Instantiate(skill.skillEffectPrefab, transform.position, Quaternion.identity);
            
           
            ISkillProjectile projScript = projObj.GetComponent<ISkillProjectile>(); 
            
            if(projScript != null)
            {
                
                projScript.SetSkillData(skill.damage, skill.projectileSpeed);
                
               
                Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
                if(rb != null)
                {
                    
                    Vector2 dir = (player.position - transform.position).normalized;
                    rb.linearVelocity = dir * skill.projectileSpeed; 
                }
            }
        }
    }
}