/*
 * ========================================
 * KnockbackTestTrap.cs
 * ========================================
 * [설명]
 * 넉백 시스템을 테스트하기 위한 테스트용 트랩입니다.
 * 원형 스프라이트를 가진 오브젝트에 부착하여 사용합니다.
 * 
 * [주요 기능]
 * - 플레이어 충돌 감지 (트리거)
 * - 플레이어를 닿은 방향으로 넉백 적용
 * - 넉백 힘, 시간, 방향 커스터마이징 가능
 * 
 * [사용 방법]
 * 1. 빈 GameObject 생성
 * 2. SpriteRenderer 추가 (원형 스프라이트 할당)
 * 3. CircleCollider2D 추가 (IsTrigger = true)
 * 4. Rigidbody2D 추가 (Body Type = Kinematic, Gravity Scale = 0)
 * 5. 이 스크립트 추가
 * 6. Inspector에서 넉백 힘, 시간 설정
 * ========================================
 */

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KnockbackTestTrap : TrapBase
{
	#region 넉백 설정

	[Header("넉백 설정")]
	[Tooltip("넉백 힘 (강도)")]
	[SerializeField] private float knockbackForce = 10f;

	[Tooltip("넉백 지속 시간 (초)")]
	[SerializeField] private float knockbackDuration = 0.5f;

	[Tooltip("넉백 방향 계산 방식")]
	[SerializeField] private KnockbackDirectionType directionType = KnockbackDirectionType.FromTrapToPlayer;

	[Tooltip("트랩 시작 시 자동 활성화 여부")]
	[SerializeField] private bool enableOnStart = true;

	#endregion

	#region 디버그 설정

	[Header("디버그 설정")]
	[Tooltip("디버그 로그 출력 여부")]
	[SerializeField] private bool showDebugLog = true;

	[Tooltip("넉백 방향을 시각적으로 표시 (Gizmos)")]
	[SerializeField] private bool showDirectionGizmo = true;

	[Tooltip("Gizmo 색상")]
	[SerializeField] private Color gizmoColor = Color.red;

	#endregion

	#region Unity Lifecycle

	/// <summary>
	/// 에디터에서 컴포넌트 추가 시 자동 설정
	/// </summary>
	private void Reset()
	{
		// Collider2D 자동 설정
		Collider2D col = GetComponent<Collider2D>();
		if (col != null)
		{
			col.isTrigger = true; // 트리거로 설정
		}

		// Rigidbody2D 자동 설정 (없으면 추가)
		Rigidbody2D rb = GetComponent<Rigidbody2D>();
		if (rb == null)
		{
			rb = gameObject.AddComponent<Rigidbody2D>();
		}
		
		if (rb != null)
		{
			rb.bodyType = RigidbodyType2D.Kinematic; // Kinematic 설정
			rb.gravityScale = 0f; // 중력 제거
		}
	}

	/// <summary>
	/// 트리거 충돌 시작 시 호출
	/// 플레이어가 트랩에 닿으면 넉백 적용
	/// </summary>
	/// <param name="other">충돌한 콜라이더</param>
	private void OnTriggerEnter2D(Collider2D other)
	{
		// 플레이어인지 확인
		if (other.CompareTag("Player"))
		{
			PlayerController player = other.GetComponent<PlayerController>();
			
			if (player != null)
			{
				ApplyKnockbackToPlayer(player);
			}
			else
			{
				Debug.LogWarning($"[KnockbackTestTrap] {other.name}에 PlayerController가 없습니다.");
			}
		}
	}

	#endregion

	#region 넉백 적용

	/// <summary>
	/// 플레이어에 넉백 적용
	/// </summary>
	/// <param name="player">넉백을 적용할 플레이어</param>
	private void ApplyKnockbackToPlayer(PlayerController player)
	{
		if (player == null) return;

		// 넉백 방향 계산
		Vector2 knockbackDirection = CalculateKnockbackDirection(player.transform.position);

		// 넉백 적용
		player.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);

		// 디버그 로그
		if (showDebugLog)
		{
			Debug.Log($"[KnockbackTestTrap] 플레이어 넉백 적용 - 방향: {knockbackDirection}, 힘: {knockbackForce}, 시간: {knockbackDuration}초");
		}
	}

	/// <summary>
	/// 넉백 방향 계산
	/// 플레이어가 닿은 방향으로 넉백 방향을 계산합니다.
	/// </summary>
	/// <param name="playerPosition">플레이어 위치</param>
	/// <returns>정규화된 넉백 방향 벡터</returns>
	private Vector2 CalculateKnockbackDirection(Vector3 playerPosition)
	{
		Vector2 direction = Vector2.zero;

		switch (directionType)
		{
			case KnockbackDirectionType.FromTrapToPlayer:
				// 트랩 중심 → 플레이어 위치 (플레이어를 트랩에서 멀리 밀어냄)
				direction = (playerPosition - transform.position).normalized;
				break;

			case KnockbackDirectionType.FromPlayerToTrap:
				// 플레이어 위치 → 트랩 중심 (플레이어를 트랩으로 끌어당김 - 일반적으로 사용 안 함)
				direction = (transform.position - playerPosition).normalized;
				break;

			case KnockbackDirectionType.CenterToPlayer:
				// 트랩 중심에서 플레이어 방향 (FromTrapToPlayer와 동일하지만 의미론적 차이)
				Vector2 trapCenter = transform.position;
				Vector2 playerPos = playerPosition;
				direction = (playerPos - trapCenter).normalized;
				break;

			case KnockbackDirectionType.RadialOutward:
				// 트랩 중심에서 외부로 방사형 (원형 트랩에 적합)
				Vector2 trapPos = transform.position;
				Vector2 playerPos2 = playerPosition;
				direction = (playerPos2 - trapPos).normalized;
				break;
		}

		return direction;
	}

	#endregion

	#region TrapBase 구현

	/// <summary>
	/// 트랩 시작 시 호출
	/// TrapBase의 추상 메서드 구현
	/// </summary>
	protected override void OnTrapStarted()
	{
		// 트랩 시작 시 콜라이더 활성화
		if (enableOnStart)
		{
			Collider2D col = GetComponent<Collider2D>();
			if (col != null)
			{
				col.enabled = true;
			}
		}

		Debug.Log($"[KnockbackTestTrap] {trapName} 트랩 활성화됨");
	}

	/// <summary>
	/// 트랩 정지 시 호출
	/// TrapBase의 추상 메서드 구현
	/// </summary>
	protected override void OnTrapStopped()
	{
		// 트랩 정지 시 콜라이더 비활성화 (선택사항)
		Collider2D col = GetComponent<Collider2D>();
		if (col != null)
		{
			col.enabled = false;
		}

		Debug.Log($"[KnockbackTestTrap] {trapName} 트랩 비활성화됨");
	}

	#endregion

	#region 디버그 및 유틸리티

	/// <summary>
	/// 에디터에서 Gizmo 그리기 (넉백 방향 시각화)
	/// </summary>
	private void OnDrawGizmos()
	{
		if (!showDirectionGizmo) return;

		// 트랩 중심 위치
		Vector3 trapCenter = transform.position;

		// 원형 트랩의 반지름 추정 (Collider2D에서 가져오기)
		float radius = 0.5f; // 기본값
		CircleCollider2D circleCol = GetComponent<CircleCollider2D>();
		if (circleCol != null)
		{
			radius = circleCol.radius;
		}

		// 트랩 영역 표시 (원)
		Gizmos.color = gizmoColor;
		Gizmos.DrawWireSphere(trapCenter, radius);

		// 넉백 방향 화살표 표시 (4방향 예시)
		Gizmos.color = Color.yellow;
		float arrowLength = radius + 0.5f;
		
		// 상하좌우 방향 화살표
		Vector3[] directions = new Vector3[]
		{
			Vector3.up * arrowLength,
			Vector3.down * arrowLength,
			Vector3.left * arrowLength,
			Vector3.right * arrowLength
		};

		foreach (Vector3 dir in directions)
		{
			Gizmos.DrawRay(trapCenter, dir);
		}

		// 플레이어가 있다면 플레이어 방향도 표시
		if (PlayerController.Instance != null)
		{
			Vector3 playerPos = PlayerController.Instance.transform.position;
			Vector2 knockbackDir = CalculateKnockbackDirection(playerPos);
			
			Gizmos.color = Color.red;
			Gizmos.DrawRay(trapCenter, (Vector3)knockbackDir * (radius + 1f));
		}
	}

	/// <summary>
	/// 넉백 설정 정보를 문자열로 반환 (디버깅용)
	/// </summary>
	public override string GetDebugInfo()
	{
		string baseInfo = base.GetDebugInfo();
		return $"{baseInfo}\n넉백 힘: {knockbackForce}, 시간: {knockbackDuration}초, 방향 타입: {directionType}";
	}

	#endregion
}

/// <summary>
/// 넉백 방향 계산 방식 열거형
/// </summary>
public enum KnockbackDirectionType
{
	/// <summary>
	/// 트랩 중심 → 플레이어 위치 (플레이어를 트랩에서 멀리 밀어냄) - 추천
	/// </summary>
	FromTrapToPlayer,

	/// <summary>
	/// 플레이어 위치 → 트랩 중심 (플레이어를 트랩으로 끌어당김 - 거의 사용 안 함)
	/// </summary>
	FromPlayerToTrap,

	/// <summary>
	/// 트랩 중심에서 플레이어 방향 (FromTrapToPlayer와 동일하지만 의미론적 차이)
	/// </summary>
	CenterToPlayer,

	/// <summary>
	/// 트랩 중심에서 외부로 방사형 (원형 트랩에 적합)
	/// </summary>
	RadialOutward
}

