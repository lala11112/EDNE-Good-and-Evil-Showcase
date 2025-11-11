using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 용기 랭크 화면 가장자리 반짝임 효과 컨트롤러
/// 플레이어의 용기 랭크가 올라갔을 때 화면 가장자리에 반짝거리는 효과를 표시합니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class CourageRankEdgeEffectController : MonoBehaviour
{
    [Header("=== 효과 설정 ===")]
    [Tooltip("효과 설정 데이터 (ScriptableObject)")]
    [SerializeField] private CourageRankEffectDataSO effectData;
    
    [Header("=== 참조 ===")]
    [Tooltip("플레이어 컨트롤러 (자동 할당)")]
    [SerializeField] private PlayerController playerController;
    
    [Header("=== 초기화 설정 ===")]
    [Tooltip("게임 시작 시 초기 랭크 설정에서 효과 표시 안 함 (기본: true)")]
    [SerializeField] private bool ignoreInitialRank = true;
    
    // 컴포넌트 참조
    private Image effectImage;
    private Material effectMaterial;
    
    // 상태
    private bool isEffectActive = false;
    private Coroutine effectCoroutine;
    private SkillRank previousRank;
    private bool isInitialized = false; // 초기화 완료 여부
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Image 컴포넌트 가져오기
        effectImage = GetComponent<Image>();
        
        // PlayerController 자동 할당
        if (playerController == null)
        {
            playerController = PlayerController.Instance;
        }
        
        // 초기 상태: 효과 비활성화
        if (effectImage != null)
        {
            effectImage.enabled = false;
        }
    }
    
    private void OnEnable()
    {
        // 초기화 플래그 리셋
        isInitialized = false;
        
        // 플레이어 컨트롤러가 있으면 랭크 변경 감지 시작
        if (playerController != null)
        {
            previousRank = playerController.CurrentSkillRank;
        }
    }
    
    private void Update()
    {
        // 플레이어 컨트롤러가 없으면 자동 할당 시도
        if (playerController == null)
        {
            playerController = PlayerController.Instance;
            if (playerController != null)
            {
                previousRank = playerController.CurrentSkillRank;
                // 초기화 플래그 리셋 (플레이어 컨트롤러를 새로 찾았으므로)
                isInitialized = false;
            }
            return;
        }
        
        // 랭크 변경 감지
        SkillRank currentRank = playerController.CurrentSkillRank;
        if (currentRank != previousRank)
        {
            // 초기화 완료 여부 확인
            if (!isInitialized)
            {
                // 첫 번째 랭크 설정: 초기화로 간주
                // ignoreInitialRank가 true이면 효과 표시 안 함
                if (ignoreInitialRank)
                {
                    // 초기화만 하고 효과는 트리거하지 않음
                    previousRank = currentRank;
                    isInitialized = true;
                    return;
                }
                // ignoreInitialRank가 false이면 정상적으로 처리 (초기화 후에도 효과 표시)
                isInitialized = true;
            }
            
            // 랭크가 올라갔는지 확인 (D < C < B < A < S)
            if (IsRankUp(previousRank, currentRank))
            {
                TriggerEffect(currentRank);
            }
            
            previousRank = currentRank;
        }
        else if (!isInitialized)
        {
            // 랭크가 변경되지 않았지만 아직 초기화되지 않은 경우
            // 초기화 완료로 표시 (다음 변경부터 효과 표시)
            isInitialized = true;
        }
    }
    
    private void OnDestroy()
    {
        // Material 정리
        if (effectMaterial != null)
        {
            Destroy(effectMaterial);
        }
    }
    
    #endregion
    
    #region 효과 트리거
    
    /// <summary>
    /// 효과를 트리거합니다.
    /// </summary>
    /// <param name="rank">현재 랭크</param>
    public void TriggerEffect(SkillRank rank)
    {
        if (effectData == null)
        {
            Debug.LogWarning("[CourageRankEdgeEffectController] 효과 데이터가 설정되지 않았습니다!");
            return;
        }
        
        // 기존 효과가 실행 중이면 중지
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }
        
        // 효과 코루틴 시작
        effectCoroutine = StartCoroutine(PlayEffectCoroutine(rank));
    }
    
    /// <summary>
    /// 효과 재생 코루틴
    /// </summary>
    private IEnumerator PlayEffectCoroutine(SkillRank rank)
    {
        isEffectActive = true;
        
        // Material 생성 또는 가져오기
        if (effectMaterial == null)
        {
            // 쉐이더 찾기
            Shader shader = Shader.Find("Custom/CourageRankEdgeEffect");
            if (shader == null)
            {
                Debug.LogError("[CourageRankEdgeEffectController] 쉐이더를 찾을 수 없습니다! 'Custom/CourageRankEdgeEffect'");
                isEffectActive = false;
                yield break;
            }
            
            if (!shader.isSupported)
            {
                Debug.LogError("[CourageRankEdgeEffectController] 쉐이더가 현재 플랫폼에서 지원되지 않습니다!");
                isEffectActive = false;
                yield break;
            }
            
            effectMaterial = new Material(shader);
            effectMaterial.hideFlags = HideFlags.DontSave; // 씬 저장 시 Material 저장 안 함
            effectImage.material = effectMaterial;
            
            // Image 컴포넌트 설정 확인
            if (effectImage.color.a <= 0f)
            {
                Debug.LogWarning("[CourageRankEdgeEffectController] Image 컴포넌트의 색상 알파 값이 0입니다! 흰색으로 설정합니다.");
                effectImage.color = Color.white;
            }
            
#if UNITY_EDITOR
            Debug.Log("[CourageRankEdgeEffectController] Material 생성 완료");
#endif
        }
        
        // 랭크별 색상 적용
        Color rankColor = effectData.GetColorForRank(rank);
        
        // 색상 알파 값 검증 (0이면 기본값으로 설정)
        if (rankColor.a <= 0f)
        {
            Debug.LogWarning($"[CourageRankEdgeEffectController] 랭크 {rank}의 색상 알파 값이 0입니다! 기본 색상으로 대체합니다.");
            rankColor = effectData.edgeColor;
        }
        
        // Material 속성 설정 (랭크별 색상 포함)
        effectMaterial.SetColor("_EdgeColor", rankColor);
        effectMaterial.SetFloat("_EdgeWidth", effectData.edgeWidth);
        effectMaterial.SetFloat("_SparkleIntensity", effectData.sparkleIntensity);
        effectMaterial.SetFloat("_SparkleSpeed", effectData.sparkleSpeed);
        effectMaterial.SetFloat("_NoiseScale", effectData.noiseScale);
        
        // Material 업데이트 강제 (UI Image에서 Material 변경 시 필요)
        // Material 인스턴스를 다시 할당하여 변경사항 반영
        effectImage.material = effectMaterial;
        
        // 효과 활성화
        effectImage.enabled = true;
        effectMaterial.SetFloat("_EffectIntensity", 1f);
        
        // 디버그 로그 (랭크별 색상 적용 확인)
#if UNITY_EDITOR
        bool isOverrideColor = false;
        if (effectData.rankColorOverrides != null && effectData.rankColorOverrides.Length > 0)
        {
            foreach (var overrideData in effectData.rankColorOverrides)
            {
                if (overrideData != null && overrideData.rank == rank)
                {
                    isOverrideColor = true;
                    break;
                }
            }
        }
        
        string colorSource = isOverrideColor ? "랭크별 설정" : "기본 색상";
        Debug.Log($"[CourageRankEdgeEffectController] 효과 트리거 - 랭크: {rank}, 색상 소스: {colorSource}, 색상: RGBA({rankColor.r:F2}, {rankColor.g:F2}, {rankColor.b:F2}, {rankColor.a:F2})");
#endif
        
        // 효과 지속 시간 대기
        float elapsedTime = 0f;
        while (elapsedTime < effectData.effectDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 페이드 아웃
        float fadeStartTime = Time.time;
        while (Time.time - fadeStartTime < effectData.fadeOutDuration)
        {
            float fadeProgress = (Time.time - fadeStartTime) / effectData.fadeOutDuration;
            float intensity = 1f - fadeProgress;
            effectMaterial.SetFloat("_EffectIntensity", intensity);
            yield return null;
        }
        
        // 효과 비활성화
        effectMaterial.SetFloat("_EffectIntensity", 0f);
        effectImage.enabled = false;
        isEffectActive = false;
        effectCoroutine = null;
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 랭크가 올라갔는지 확인합니다.
    /// </summary>
    /// <param name="previousRank">이전 랭크</param>
    /// <param name="currentRank">현재 랭크</param>
    /// <returns>랭크가 올라갔으면 true</returns>
    private bool IsRankUp(SkillRank previousRank, SkillRank currentRank)
    {
        // D < C < B < A < S 순서
        int previousValue = GetRankValue(previousRank);
        int currentValue = GetRankValue(currentRank);
        
        return currentValue > previousValue;
    }
    
    /// <summary>
    /// 랭크를 숫자 값으로 변환합니다.
    /// </summary>
    private int GetRankValue(SkillRank rank)
    {
        switch (rank)
        {
            case SkillRank.D: return 0;
            case SkillRank.C: return 1;
            case SkillRank.B: return 2;
            case SkillRank.A: return 3;
            case SkillRank.S: return 4;
            default: return 0;
        }
    }
    
    #endregion
    
    #region 디버그
    
    [ContextMenu("테스트: 효과 트리거 (현재 랭크)")]
    private void DebugTriggerEffect()
    {
        if (playerController != null)
        {
            TriggerEffect(playerController.CurrentSkillRank);
        }
        else
        {
            Debug.LogWarning("[CourageRankEdgeEffectController] 플레이어 컨트롤러가 없습니다!");
        }
    }
    
    #endregion
}

