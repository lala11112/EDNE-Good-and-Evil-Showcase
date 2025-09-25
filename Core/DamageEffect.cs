using System.Collections;
using UnityEngine;

[AddComponentMenu("Game/Health/DOT-HOT Effect")]
public class DamageEffect : MonoBehaviour
{
    public enum EffectKind
    {
        Damage,
        Heal
    }

    public enum Scaling
    {
        Flat,
        PercentMax,
        PercentCurrent
    }

    [Header("Target")]
    [SerializeField, Tooltip("효과를 적용할 대상. 비워두면 자신 또는 부모에서 자동 검색")]
    private HealthBase target;

    [Header("Effect Setup")]
    [SerializeField, Tooltip("데미지 DOT인지 힐 HOT인지 선택")]
    private EffectKind kind = EffectKind.Damage;

    [SerializeField, Tooltip("수치 스케일 방식: 고정/최대HP비율/현재HP비율")]
    private Scaling scaling = Scaling.Flat;

    [SerializeField, Min(0f), Tooltip("틱마다 적용되는 기본 값. Percent 계열이면 % (예: 1은 1%)")]
    private float amountPerTick = 5f;

    [SerializeField, Min(0.01f), Tooltip("틱 간격(초)")]
    private float tickInterval = 1f;

    [SerializeField, Min(0.01f), Tooltip("총 지속 시간(초)")]
    private float durationSeconds = 5f;

    [SerializeField, Tooltip("같은 효과가 있을 때 중첩 허용 여부. 비활성 시 기존 코루틴 종료 후 재시작")]
    private bool allowStacks = true;

    private Coroutine runningRoutine;

    void Awake()
    {
        if (target == null)
        {
            // 대상 HealthBase 컴포넌트 자동 검색
            target = GetComponent<HealthBase>();
            if (target == null) target = GetComponentInParent<HealthBase>();
        }
        
        // --- 테스트를 위한 자동 시작 로직 추가 ---
        if (target != null)
        {
            // 오브젝트가 활성화되면 바로 Play()를 호출하여 효과 시작
            Play();
        }
    }

    public void Play()
    {
        // 인스펙터에 설정된 기본값으로 효과 시작
        StartEffect(kind, scaling, amountPerTick, tickInterval, durationSeconds);
    }

    public void StartEffect(EffectKind effectKind, Scaling effectScaling, float perTickAmount, float intervalSeconds, float totalSeconds)
    {
        if (target == null) return;
        if (!allowStacks && runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        runningRoutine = StartCoroutine(RunEffect(effectKind, effectScaling,
            Mathf.Max(0f, perTickAmount), Mathf.Max(0.01f, intervalSeconds), Mathf.Max(0.01f, totalSeconds)));
    }

    public void StopEffect()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }
    }

    private IEnumerator RunEffect(EffectKind effectKind, Scaling effectScaling, float perTickAmount, float intervalSeconds, float totalSeconds)
    {
        // 종료 시간 기반으로 변경 (더 정확함)
        float endTime = Time.time + totalSeconds;
        WaitForSeconds wait = new WaitForSeconds(intervalSeconds);
        
        while (Time.time < endTime)
        {
            // 실행 중 대상이 파괴되었는지 확인
            if (target == null) 
            {
                runningRoutine = null;
                yield break;
            }
            
            // 데미지 효과일 경우 대상이 죽었는지 확인 (힐 효과는 죽어도 가능하게 유지)
            if (target.IsDead && effectKind == EffectKind.Damage)
            {
                runningRoutine = null;
                yield break;
            }

            int amount = CalculateAmount(effectScaling, perTickAmount, target);
            if (amount > 0)
            {
                if (effectKind == EffectKind.Damage) target.Damage(amount);
                else target.Heal(amount);
            }

            yield return wait;
        }
        // 코루틴 정상 종료 시 runningRoutine 해제
        runningRoutine = null;
    }

    private static int CalculateAmount(Scaling effectScaling, float perTickAmount, HealthBase t)
    {
        switch (effectScaling)
        {
            case Scaling.Flat:
                return Mathf.Max(0, Mathf.RoundToInt(perTickAmount));
            case Scaling.PercentMax:
                return Mathf.Max(0, Mathf.RoundToInt(t.Max * (perTickAmount * 0.01f)));
            case Scaling.PercentCurrent:
                return Mathf.Max(0, Mathf.RoundToInt(t.Current * (perTickAmount * 0.01f)));
            default:
                return 0;
        }
    }

    public int PreviewTickAmount()
    {
        if (target == null) return 0;
        return CalculateAmount(scaling, amountPerTick, target);
    }
}