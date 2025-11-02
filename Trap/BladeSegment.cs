/*
 * ========================================
 * BladeSegment.cs
 * ========================================
 * [설명]
 * 커터 블레이드의 개별 칼날 세그먼트입니다.
 * 플레이어와의 충돌을 감지하고 데미지 + 넉백을 적용합니다.
 * 
 * [주요 기능]
 * - 플레이어 충돌 감지
 * - 데미지 적용
 * - 넉백 적용 (PlayerController.ApplyKnockback 사용)
 * - 데미지 간격 제어
 * 
 * [사용 방법]
 * 1. RotatingCutterBlade의 자식 오브젝트로 배치
 * 2. SpriteRenderer 추가 (칼날 스프라이트)
 * 3. BoxCollider2D 또는 EdgeCollider2D 추가 (IsTrigger = true)
 * 4. 이 스크립트 추가
 * 5. RotatingCutterBlade에서 자동으로 찾아 활성화/비활성화
 * ========================================
 */

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class BladeSegment : MonoBehaviour
{
	#region 공격 설정

	[Header("공격 설정")]
	[Tooltip("플레이어에게 입히는 데미지")]
	[SerializeField] private int damage = 10;

	[Tooltip("넉백 힘")]
	[SerializeField] private float knockbackForce = 15f;

	[Tooltip("넉백 지속 시간 (초)")]
	[SerializeField] private float knockbackDuration = 0.3f;

	[Tooltip("데미지 간격 (초) - 같은 플레이어에게 연속으로 데미지를 주는 시간")]
	[SerializeField] private float damageInterval = 0.5f;

	#endregion

	#region 컴포넌트

	private Collider2D segmentCollider;

	/// <summary>
	/// 연결된 RotatingCutterBlade (회전 방향 정보 참조용)
	/// </summary>
	private RotatingCutterBlade parentCutterBlade;

	#endregion

	#region 런타임 상태

	/// <summary>
	/// 칼날이 활성화되어 있는지 여부
	/// </summary>
	private bool isActive = false;

	/// <summary>
	/// 데미지를 받은 플레이어 추적 (간격 제어용)
	/// </summary>
	private Dictionary<PlayerController, float> lastDamageTime = new Dictionary<PlayerController, float>();

	#endregion

	#region Unity Lifecycle

	/// <summary>
	/// 칼날 초기화
	/// </summary>
	private void Awake()
	{
		// Collider2D 자동 할당
		segmentCollider = GetComponent<Collider2D>();
		if (segmentCollider != null)
		{
			segmentCollider.isTrigger = true;
			segmentCollider.enabled = false; // 처음에는 비활성화
			Debug.Log($"[BladeSegment] {name} Collider2D 설정: isTrigger={segmentCollider.isTrigger}, enabled={segmentCollider.enabled}");
		}
		else
		{
			Debug.LogError($"[BladeSegment] {name}에 Collider2D가 없습니다!");
		}

		// 부모에서 RotatingCutterBlade 찾기 (회전 방향 정보용)
		parentCutterBlade = GetComponentInParent<RotatingCutterBlade>();
		if (parentCutterBlade != null)
		{
			Debug.Log($"[BladeSegment] {name} 부모 RotatingCutterBlade 찾기 성공: {parentCutterBlade.name}");
		}
		else
		{
			Debug.LogWarning($"[BladeSegment] {name} 부모에서 RotatingCutterBlade를 찾을 수 없습니다. 넉백 방향 계산 시 단순 방향을 사용합니다.");
		}
	}

	/// <summary>
	/// 에디터에서 컴포넌트 추가 시 자동 설정
	/// </summary>
	private void Reset()
	{
		// Collider2D 자동 설정
		Collider2D col = GetComponent<Collider2D>();
		if (col == null)
		{
			col = gameObject.AddComponent<BoxCollider2D>();
		}

		if (col != null)
		{
			col.isTrigger = true;
		}
	}

	#endregion

	#region 칼날 활성화/비활성화

	/// <summary>
	/// 칼날 활성화
	/// RotatingCutterBlade에서 호출합니다.
	/// </summary>
	public void ActivateSegment()
	{
		if (isActive)
		{
			Debug.LogWarning($"[BladeSegment] {name}이 이미 활성화되어 있습니다.");
			return;
		}

		isActive = true;

		if (segmentCollider != null)
		{
			segmentCollider.enabled = true;
		}

		Debug.Log($"[BladeSegment] {name} 활성화됨");
	}

	/// <summary>
	/// 칼날 비활성화
	/// RotatingCutterBlade에서 호출합니다.
	/// </summary>
	public void DeactivateSegment()
	{
		if (!isActive)
		{
			Debug.LogWarning($"[BladeSegment] {name}이 이미 비활성화되어 있습니다.");
			return;
		}

		isActive = false;

		if (segmentCollider != null)
		{
			segmentCollider.enabled = false;
		}

		// 데미지 추적 초기화
		lastDamageTime.Clear();

		Debug.Log($"[BladeSegment] {name} 비활성화됨");
	}

	/// <summary>
	/// 칼날 활성화 여부
	/// </summary>
	public bool IsActive => isActive;

	#endregion

	#region 충돌 처리

	/// <summary>
	/// 트리거 충돌 시작 시 호출
	/// </summary>
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!isActive)
		{
			Debug.Log($"[BladeSegment] {name} OnTriggerEnter2D 호출되었지만 칼날이 비활성화됨 (isActive={isActive})");
			return;
		}

		Debug.Log($"[BladeSegment] {name} OnTriggerEnter2D: {other.name} 충돌 감지");

		// 플레이어 감지 (몬스터는 무시)
		PlayerController player = other.GetComponent<PlayerController>();
		if (player != null)
		{
			Debug.Log($"[BladeSegment] {name} PlayerController 발견: {player.name}");
			HandlePlayerHit(player);
		}
		else
		{
			Debug.Log($"[BladeSegment] {name} {other.name}에 PlayerController가 없습니다. (몬스터 또는 기타 오브젝트 - 무시)");
		}
	}

	/// <summary>
	/// 트리거 충돌 유지 시 호출 (연속 충돌 처리)
	/// </summary>
	private void OnTriggerStay2D(Collider2D other)
	{
		if (!isActive) return;

		// 플레이어 감지 (몬스터는 무시)
		PlayerController player = other.GetComponent<PlayerController>();
		if (player != null)
		{
			HandlePlayerHit(player);
		}
	}

	/// <summary>
	/// 플레이어 충돌 처리
	/// </summary>
	/// <param name="player">충돌한 플레이어</param>
	private void HandlePlayerHit(PlayerController player)
	{
		Debug.Log($"[BladeSegment] {name} HandlePlayerHit 호출: {player?.name ?? "null"}");

		if (player == null)
		{
			Debug.LogWarning($"[BladeSegment] {name} player가 null입니다.");
			return;
		}

		if (!player.IsAlive)
		{
			Debug.Log($"[BladeSegment] {name} {player.name}이 이미 사망했습니다. 무시됩니다.");
			return;
		}

		Debug.Log($"[BladeSegment] {name} {player.name} PlayerController 찾음 - 현재 체력: {player.CurrentHealth}/{player.MaxHealth}");

		// 데미지 간격 체크
		if (lastDamageTime.ContainsKey(player))
		{
			float timeSinceLastDamage = Time.time - lastDamageTime[player];
			if (timeSinceLastDamage < damageInterval)
			{
				Debug.Log($"[BladeSegment] {name} {player.name} 데미지 간격 대기 중... ({timeSinceLastDamage:F2}초 / {damageInterval}초)");
				return; // 아직 간격이 안 지났음
			}
			else
			{
				Debug.Log($"[BladeSegment] {name} {player.name} 데미지 간격 충족됨 ({timeSinceLastDamage:F2}초 경과)");
			}
		}

		// 데미지 적용 (플레이어에게 환경 데미지 적용)
		Debug.Log($"[BladeSegment] {name} {player.name}에게 {damage} 데미지 적용 시도...");
		bool damageApplied = player.ApplyDamage(damage, EntityType.Environment, this);

		if (damageApplied)
		{
			// 데미지 시간 기록
			lastDamageTime[player] = Time.time;

			// 넉백 적용 (플레이어의 ApplyKnockback 메서드 사용)
			Debug.Log($"[BladeSegment] {name} {player.name} 넉백 적용 시도...");
			ApplyKnockbackToPlayer(player);

			Debug.Log($"[BladeSegment] {name} {player.name}에게 {damage} 데미지 + 넉백 적용 완료! (남은 체력: {player.CurrentHealth}/{player.MaxHealth})");
		}
		else
		{
			Debug.LogWarning($"[BladeSegment] {name} {player.name}에게 데미지 적용 실패 (무적 상태이거나 이미 사망)");
		}
	}

	/// <summary>
	/// 플레이어에게 넉백 적용
	/// PlayerController의 ApplyKnockback 메서드를 사용합니다.
	/// 넉백 방향은 회전 방향을 고려하여 계산됩니다.
	/// </summary>
	/// <param name="player">넉백을 적용할 플레이어</param>
	private void ApplyKnockbackToPlayer(PlayerController player)
	{
		if (player == null)
		{
			Debug.LogWarning($"[BladeSegment] {name} ApplyKnockbackToPlayer: player가 null입니다.");
			return;
		}

		// 넉백 방향 계산 (회전 방향 고려)
		Vector2 direction = CalculateKnockbackDirection(player);
		
		Debug.Log($"[BladeSegment] {name} {player.name} 넉백 방향 계산: {direction}, 힘: {knockbackForce}, 시간: {knockbackDuration}초");

		// 플레이어의 ApplyKnockback 메서드 사용
		player.ApplyKnockback(direction, knockbackForce, knockbackDuration);
		Debug.Log($"[BladeSegment] {name} {player.name}에게 PlayerController.ApplyKnockback 호출 완료");
	}

	/// <summary>
	/// 넉백 방향 계산 (회전 방향 고려)
	/// 회전하는 칼날이 플레이어를 치는 방향으로 밀어냅니다.
	/// </summary>
	/// <param name="player">넉백을 적용할 플레이어</param>
	/// <returns>정규화된 넉백 방향 벡터</returns>
	private Vector2 CalculateKnockbackDirection(PlayerController player)
	{
		// 커터 블레이드 중심 위치 (부모가 있으면 중심, 없으면 칼날 위치)
		Vector2 centerPosition = GetCutterCenter();
		
		// 칼날 위치에서 플레이어 방향 벡터
		Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
		
		// 칼날 위치에서 중심 방향 벡터
		Vector2 toCenter = (Vector2)centerPosition - (Vector2)transform.position;
		
		// 회전 방향에 따른 탄젠트 벡터 계산
		// 시계 방향: toCenter를 90도 시계 방향으로 회전
		// 반시계 방향: toCenter를 90도 반시계 방향으로 회전
		Vector2 tangentDirection;
		
		if (parentCutterBlade != null)
		{
			// 회전 방향에 따라 탄젠트 방향 결정
			bool clockwise = parentCutterBlade.GetRotationDirection() == RotationDirection.Clockwise;
			
			// toCenter 벡터를 90도 회전 (회전 방향에 따라)
			// 2D에서 90도 회전: (x, y) -> (-y, x) (반시계), (y, -x) (시계)
			if (clockwise)
			{
				// 시계 방향: 탄젠트는 중심에서 멀어지는 방향
				tangentDirection = new Vector2(toCenter.y, -toCenter.x).normalized;
			}
			else
			{
				// 반시계 방향: 탄젠트는 중심에서 멀어지는 방향
				tangentDirection = new Vector2(-toCenter.y, toCenter.x).normalized;
			}
			
			// 탄젠트 방향과 플레이어 방향을 조합
			// 칼날이 플레이어를 치는 방향 = 탄젠트 방향의 성분 + 플레이어를 밀어내는 방향
			Vector2 knockbackDirection = toPlayer.normalized;
			
			// 탄젠트 성분을 일부 반영 (칼날이 회전하면서 치는 느낌)
			float tangentWeight = 0.3f; // 탄젠트 성분의 비율 (0.0 ~ 1.0)
			knockbackDirection = (knockbackDirection * (1f - tangentWeight) + tangentDirection * tangentWeight).normalized;
			
			Debug.Log($"[BladeSegment] {name} 회전 방향 고려 넉백 계산:");
			Debug.Log($"  - 중심 위치: {centerPosition}");
			Debug.Log($"  - 플레이어 방향: {toPlayer.normalized}");
			Debug.Log($"  - 탄젠트 방향: {tangentDirection}");
			Debug.Log($"  - 최종 넉백 방향: {knockbackDirection}");
			
			return knockbackDirection;
		}
		else
		{
			// 부모가 없으면 단순히 칼날에서 플레이어 방향
			Debug.Log($"[BladeSegment] {name} 부모 없음 - 단순 방향 계산");
			return toPlayer.normalized;
		}
	}

	/// <summary>
	/// 커터 블레이드 중심 위치 가져오기
	/// </summary>
	/// <returns>중심 위치</returns>
	private Vector2 GetCutterCenter()
	{
		if (parentCutterBlade != null)
		{
			return parentCutterBlade.transform.position;
		}
		
		// 부모가 없으면 칼날 위치를 중심으로 사용 (Fallback)
		return transform.position;
	}

	#endregion

	#region 디버그 및 유틸리티

	/// <summary>
	/// 칼날 정보를 문자열로 반환 (디버깅용)
	/// </summary>
	public string GetDebugInfo()
	{
		return $"칼날 이름: {name}, 활성화: {isActive}, 데미지: {damage}, 넉백 힘: {knockbackForce}";
	}

	#endregion
}

