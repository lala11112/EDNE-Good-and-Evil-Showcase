using UnityEngine;

public class EnemyHpTester : MonoBehaviour
{
    public EnemyHp enemyHp; // 테스트할 EnemyHp 연결

    private void Update()
    {
        // 예: 스페이스바 누르면 HP 감소
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (enemyHp != null)
            {
                enemyHp.EnemyDecrease(10); // 10 데미지 예시
                Debug.Log("테스트: EnemyDecrease 호출됨");
            }
        }
    }
}