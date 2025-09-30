using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDataSO", menuName = "Entity/Entity Data/Monster Data")]
public class MonsterDataSO : EntityDataSO
{
    private void Awake()
    {
        entityType = EntityType.Monster;
    }
    
    [Header("이동 속도")]
    public float moveSpeed;
    public bool moveAble;
    
    [Header("몸박 데미지")]
    public bool hasBodyDamage;      // 몸박 데미지 여부
    public int bodyDamageAmount;    // 몸박 데미지 수치
    

}   
    

    