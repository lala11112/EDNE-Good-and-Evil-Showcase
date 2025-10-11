using UnityEngine;

/// <summary>
/// MonsterController - Debug 파트
/// 에디터에서 디버그 정보 시각화
/// </summary>
public partial class MonsterController : EntityController
{
    #region Debug Visualization - Gizmos 시각화
    
    private void OnDrawGizmosSelected()
    {
        if (monsterData == null) return;
        
        Vector3 position = Application.isPlaying ? transform.position : transform.position;
        
        // ⭐ 런타임/에디터 모드에 따라 다른 값 사용
        float displayDetectionRange = Application.isPlaying ? runtimeDetectionRange : monsterData.detectionRange;
        float displayStopDistance = Application.isPlaying ? runtimeStopDistance : monsterData.stopDistance;
        
        // ========== 인식 범위 (노란색) ==========
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, displayDetectionRange);
        
        // ========== 정지 거리 (초록색) ==========
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(position, displayStopDistance);
        
        // ⭐ 공격 범위는 각 스킬의 minRange/maxRange로 대체됨
        // 스킬별 범위는 SkillPriority에서 설정
        
        // ========== 타겟 관계 ==========
        if (target != null)
        {
            float distance = Vector2.Distance(position, target.position);
            bool isInRange = distance <= displayDetectionRange;
            
            // 타겟까지의 선
            if (isInRange)
            {
                // 인식 범위 내
                Gizmos.color = isMovementEnabled ? Color.green : Color.red;
            }
            else
            {
                // 인식 범위 밖
                Gizmos.color = Color.gray;
            }
            Gizmos.DrawLine(position, target.position);
            
            // 타겟 위치 표시
            Gizmos.DrawWireSphere(target.position, 0.5f);
            
            // 거리 텍스트 (에디터에서만)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                (position + target.position) / 2,
                $"Distance: {distance:F1}m"
            );
            #endif
        }
        
        // ========== 스킬 사정거리들 (빨간색) ==========
        if (skillPriorities != null && skillPriorities.Length > 0)
        {
            for (int i = 0; i < skillPriorities.Length; i++)
            {
                var skillPriority = skillPriorities[i];
                if (skillPriority.skill == null) continue;
                
                // 최대 범위 (빨간색, 반투명)
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(position, skillPriority.maxRange);
                
                // 최소 범위 (빨간색, 더 진하게)
                if (skillPriority.minRange > 0f)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
                    Gizmos.DrawWireSphere(position, skillPriority.minRange);
                }
            }
        }
        
        // ========== NavMesh 경로 표시 (플레이 중일 때만) ==========
        // ⭐ NavMeshAgent가 활성화되어 있고 NavMesh에 있을 때만 표시
        if (Application.isPlaying && navAgent != null && navAgent.enabled && navAgent.isOnNavMesh && navAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            var path = navAgent.path;
            Vector3 previousCorner = position;
            
            foreach (var corner in path.corners)
            {
                Gizmos.DrawLine(previousCorner, corner);
                Gizmos.DrawWireSphere(corner, 0.2f);
                previousCorner = corner;
            }
        }
        
        // ========== 이동 방향 표시 (NavMeshAgent 속도) ==========
        if (Application.isPlaying && IsMoving())
        {
            Gizmos.color = Color.yellow;
            Vector3 endPoint = position + navAgent.velocity.normalized * 1.5f;
            
            Gizmos.DrawLine(position, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.3f);
        }
    }
    
    #endregion
    
    #region Debug Info - 디버그 정보 (나중에 GUI로 표시 가능)
    
    #if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 디버그 정보 표시 (Scene View GUI)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (monsterData == null) return;
        
        // Scene View에 정보 표시
        Vector3 labelPosition = transform.position + Vector3.up * 2f;
        
        string info = $"{gameObject.name}\n";
        info += $"HP: {currentHealth}/{MaxHealth}\n";
        info += $"Move: {(isMovementEnabled ? "ON" : "OFF")}\n";
        info += $"Skill: {(isSkillActive ? "ACTIVE" : "READY")}\n";
        
        if (target != null)
        {
            float distance = GetDistanceToTarget();
            info += $"Distance: {distance:F1}m";
        }
        
        UnityEditor.Handles.Label(labelPosition, info);
    }
    #endif
    
    #endregion
}

