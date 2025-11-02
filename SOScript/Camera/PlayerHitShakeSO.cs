using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;

/// <summary>
/// 플레이어 피격 시 데미지 크기에 따라 카메라 쉐이크 강도를 산출하는 SO
/// ProCamera2DShake.Shake(duration, strength, vibrato, randomness) 사용
/// </summary>
[CreateAssetMenu(fileName = "PlayerHitShakeSO", menuName = "Camera/Player Hit Shake")]
public class PlayerHitShakeSO : ScriptableObject
{
	[Header("기본 프리셋 (옵션)")]
	[Tooltip("최대값 기준으로 사용할 ProCamera2D ShakePreset. 비우면 아래 Max 값을 사용")]
	public ShakePreset preset;

	[Header("데미지 입력 범위")]
	[Tooltip("이 범위를 0~1로 정규화하여 곡선에 넣습니다")] 
	public float minDamage = 1f;
	public float maxDamage = 50f;

	[Header("Duration (초)")]
	[Min(0f)] public float durationMin = 0.08f;
	[Min(0f)] public float durationMax = 0.25f;
	[Tooltip("0~1 입력에 대한 지속시간 스케일 곡선")] public AnimationCurve durationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Strength (진폭)")]
	[Tooltip("축별 진폭 최소값 (x: 가로, y: 세로)")]
	public Vector2 strengthMin = new Vector2(0.1f, 0.1f);
	[Tooltip("축별 진폭 최대값 (x: 가로, y: 세로)")]
	public Vector2 strengthMax = new Vector2(0.6f, 0.6f);
	[Tooltip("0~1 입력에 대한 강도 스케일 곡선")] public AnimationCurve strengthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Vibrato (진동 수)")]
	[Min(1)] public int vibratoMin = 15;
	[Min(1)] public int vibratoMax = 50;
	[Tooltip("0~1 입력에 대한 진동 수 스케일 곡선")] public AnimationCurve vibratoCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Randomness (무작위성)")]
	[Range(0f, 1f)] public float randomnessMin = 0.2f;
	[Range(0f, 1f)] public float randomnessMax = 0.8f;
	[Tooltip("0~1 입력에 대한 무작위성 스케일 곡선")] public AnimationCurve randomnessCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	/// <summary>
	/// 데미지를 0~1로 정규화합니다.
	/// </summary>
	private float NormalizeDamage(float damage)
	{
		if (Mathf.Approximately(minDamage, maxDamage)) return 1f;
		return Mathf.Clamp01((damage - minDamage) / Mathf.Max(0.0001f, (maxDamage - minDamage)));
	}

	/// <summary>
	/// 데미지에 따른 쉐이크 파라미터를 계산합니다.
	/// </summary>
	public void GetShakeParams(float damage, out float duration, out Vector2 strength, out int vibrato, out float randomness)
	{
		float t = NormalizeDamage(damage);
		float td = Mathf.Clamp01(durationCurve.Evaluate(t));
		float ts = Mathf.Clamp01(strengthCurve.Evaluate(t));
		float tv = Mathf.Clamp01(vibratoCurve.Evaluate(t));
		float tr = Mathf.Clamp01(randomnessCurve.Evaluate(t));

		// 프리셋이 있으면 Max 값을 프리셋에서 가져와 보강
		float maxDur = preset != null ? preset.Duration : durationMax;
		Vector2 maxStr = preset != null ? new Vector2(preset.Strength.x, preset.Strength.y) : strengthMax;
		int maxVib = preset != null ? preset.Vibrato : vibratoMax;
		float maxRnd = preset != null ? preset.Randomness : randomnessMax;

		duration = Mathf.Lerp(durationMin, maxDur, td);
		strength = Vector2.Lerp(strengthMin, maxStr, ts);
		vibrato = Mathf.RoundToInt(Mathf.Lerp(vibratoMin, maxVib, tv));
		randomness = Mathf.Lerp(randomnessMin, maxRnd, tr);
	}

	/// <summary>
	/// ProCamera2DShake에 즉시 적용합니다.
	/// </summary>
	public void ApplyShake(ProCamera2DShake shaker, float damage)
	{
		if (shaker == null) return;
		GetShakeParams(damage, out float duration, out Vector2 strength, out int vibrato, out float randomness);
		shaker.Shake(duration, strength, vibrato, randomness);
	}
}


