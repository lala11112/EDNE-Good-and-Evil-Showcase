using UnityEngine;

/// <summary>
/// 용기 랭크 효과 설정 데이터 ScriptableObject
/// 화면 가장자리 반짝임 효과의 설정값을 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "CourageRankEffectDataSO", menuName = "Player/Courage Rank Effect Data")]
public class CourageRankEffectDataSO : ScriptableObject
{
    [Header("=== 효과 기본 설정 ===")]
    [Tooltip("효과 지속 시간 (초)")]
    [Range(0.5f, 5f)]
    public float effectDuration = 2f;
    
    [Tooltip("효과 페이드 아웃 시간 (초)")]
    [Range(0.1f, 2f)]
    public float fadeOutDuration = 0.5f;
    
    [Header("=== 쉐이더 설정 ===")]
    [Tooltip("가장자리 색상")]
    public Color edgeColor = new Color(1f, 1f, 0.5f, 1f);
    
    [Tooltip("가장자리 너비 (0 = 중심, 0.5 = 전체)")]
    [Range(0.05f, 0.3f)]
    public float edgeWidth = 0.1f;
    
    [Tooltip("반짝임 강도")]
    [Range(0f, 2f)]
    public float sparkleIntensity = 1f;
    
    [Tooltip("반짝임 속도")]
    [Range(0f, 10f)]
    public float sparkleSpeed = 2f;
    
    [Tooltip("노이즈 스케일")]
    [Range(1f, 20f)]
    public float noiseScale = 5f;
    
    [Header("=== 랭크별 설정 (선택적) ===")]
    [Tooltip("랭크별 색상 오버라이드 (비어있으면 기본 색상 사용)")]
    public RankColorOverride[] rankColorOverrides = new RankColorOverride[0];
    
    /// <summary>
    /// 특정 랭크의 색상을 가져옵니다.
    /// 랭크별 오버라이드가 있으면 해당 색상을, 없으면 기본 색상을 반환합니다.
    /// </summary>
    public Color GetColorForRank(SkillRank rank)
    {
        // 랭크별 색상 오버라이드 확인
        if (rankColorOverrides != null && rankColorOverrides.Length > 0)
        {
            foreach (var overrideData in rankColorOverrides)
            {
                // null 체크 및 랭크 매칭 확인
                if (overrideData != null && overrideData.rank == rank)
                {
                    // 알파 값이 0이면 기본 색상 사용
                    if (overrideData.color.a <= 0f)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"[CourageRankEffectDataSO] 랭크 {rank}의 색상 알파 값이 0입니다. 기본 색상을 사용합니다.");
#endif
                        // 기본 색상도 알파 값 확인
                        if (edgeColor.a <= 0f)
                        {
                            return Color.white;
                        }
                        return edgeColor;
                    }
                    
                    // 랭크별 색상 반환
#if UNITY_EDITOR
                    Debug.Log($"[CourageRankEffectDataSO] 랭크 {rank}의 랭크별 색상 적용: RGBA({overrideData.color.r:F2}, {overrideData.color.g:F2}, {overrideData.color.b:F2}, {overrideData.color.a:F2})");
#endif
                    return overrideData.color;
                }
            }
        }
        
        // 랭크별 색상이 없으면 기본 색상 사용
        // 기본 색상도 알파 값 확인
        if (edgeColor.a <= 0f)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CourageRankEffectDataSO] 기본 색상의 알파 값이 0입니다! 흰색으로 대체합니다.");
#endif
            return Color.white;
        }
        
#if UNITY_EDITOR
        Debug.Log($"[CourageRankEffectDataSO] 랭크 {rank}에 대한 랭크별 색상이 없어 기본 색상 사용: RGBA({edgeColor.r:F2}, {edgeColor.g:F2}, {edgeColor.b:F2}, {edgeColor.a:F2})");
#endif
        return edgeColor;
    }
}

/// <summary>
/// 랭크별 색상 오버라이드 데이터
/// </summary>
[System.Serializable]
public class RankColorOverride
{
    [Tooltip("스킬 랭크")]
    public SkillRank rank;
    
    [Tooltip("해당 랭크의 색상")]
    public Color color = Color.white;
}

