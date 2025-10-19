using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 엔티티 기본 데이터 ScriptableObject
/// 모든 엔티티(플레이어, 몬스터, 상호작용 오브젝트)의 원본 데이터를 담습니다.
/// 이 클래스는 상속받아 PlayerSO, MonsterSO 등으로 확장됩니다.
/// </summary>
[CreateAssetMenu(fileName = "EntityDataSO", menuName = "Entity/Entity Data")]
public class EntityDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 ID (중복 불가, 저장/로드에 사용)")]
    public string uniqueID;
    
    [Tooltip("엔티티 이름 (UI 표시용)")]
    public string entityName;
    
    [Tooltip("엔티티 타입 (Player, Monster, Environment 등)")]
    public EntityType entityType;
    
    [Header("체력 시스템")]
    [Tooltip("최대 체력 (시작 체력이기도 함)")]
    public int maxHealth;
    
    [Tooltip("데미지 감소율 (0~1, 0.2 = 20% 감소)")]
    [Range(0f, 1f)]
    public float damageReduction = 0f;
    
    [Header("이동")]
    [Tooltip("기본 이동 속도")]
    public float moveSpeed = 5f;
    
    [Header("비주얼")]
    [Tooltip("엔티티 스프라이트 (UI용)")]
    public Sprite entitySprite;
}

