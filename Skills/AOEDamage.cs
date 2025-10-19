using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AOE(범위 공격) 데미지 처리 스크립트
/// SlashSkillHandler 등 범위 공격 스킬에서 사용
/// </summary>
public class AOEDamage : MonoBehaviour
{
    private float damage;
    private EntityType attackerType;
    private HashSet<EntityController> hitTargets = new HashSet<EntityController>(); // 중복 히트 방지
    
    /// <summary>
    /// AOE 데미지 설정
    /// </summary>
    /// <param name="damage">적용할 데미지</param>
    /// <param name="attackerType">공격자 타입 (팀킬 방지용)</param>
    public void Setup(float damage, EntityType attackerType)
    {
        this.damage = damage;
        this.attackerType = attackerType;
        
        Debug.Log($"[AOEDamage] Setup - Damage: {damage}, Type: {attackerType}");
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // EntityController가 있는 대상과 충돌 시
        EntityController target = collision.GetComponent<EntityController>();
        
        if (target != null)
        {
            // 중복 히트 방지
            if (hitTargets.Contains(target))
            {
                Debug.Log($"[AOEDamage] {target.name}은 이미 맞은 적 - 무시");
                return;
            }
            
            // 데미지 적용 시도 (팀킬 방지 포함)
            bool damageApplied = target.ApplyDamage((int)damage, attackerType);
            
            if (damageApplied)
            {
                // ✅ 데미지 성공 - 히트 목록에 추가
                hitTargets.Add(target);
                Debug.Log($"[AOEDamage] {target.name}에게 {damage} 데미지!");
            }
            else
            {
                // ❌ 팀킬 방지로 데미지 무시
                Debug.Log($"[AOEDamage] {target.name}은 아군이라 데미지 무시!");
            }
        }
    }
}

