using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 일반 Animator(블렌드 미사용) 기반 몬스터 애니메이션 드라이버
/// - 상태: Idle/Walk/Attack/Death
/// - 걷기 애니메이션은 "측면(옆)" 전용 한 가지 클립을 사용한다고 가정
/// - 좌/우 이동 시 SpriteRenderer.flipX로 방향 전환
/// - 상/하 이동 시에도 동일한 측면 걷기 애니메이션을 재생
/// </summary>
public class MonsterAnimatorSimple : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private Animator animator;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private Rigidbody2D rb;
	private NavMeshAgent navAgent;

	[Header("Movement Settings")]
	[SerializeField] private float startMoveSpeed = 0.05f; // 이동 시작 임계값
	[SerializeField] private float fullStopSpeed = 0.001f; // 완전 정지 임계값

	[Header("Animator Parameters (Discrete)")]
	[SerializeField] private string paramIsMove = "isMove";          // Idle/Walk 전환용
	[SerializeField] private string paramAttackTrigger = "Attack";    // 공격 트리거
	[SerializeField] private string paramDeathTrigger = "Death";      // 사망 트리거
	[SerializeField] private string paramIsDead = "IsDead";           // 사망 상태

	[Header("Facing Settings")]
	[SerializeField] private bool invertFlipX = false; // 리소스 방향이 반대라면 true

	private Vector2 previousPosition;
	private bool isMovingSmoothed = false;
	private int lastHorizontalSign = 1; // -1(왼쪽), +1(오른쪽)

	private void Awake()
	{
		if (animator == null)
		{
			animator = GetComponentInChildren<Animator>();
		}
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}
		if (rb == null)
		{
			rb = GetComponent<Rigidbody2D>();
		}
		if (navAgent == null)
		{
			navAgent = GetComponent<NavMeshAgent>();
			if (navAgent != null)
			{
				navAgent.updateRotation = false;
				navAgent.updateUpAxis = false;
			}
		}

		previousPosition = rb != null ? rb.position : (Vector2)transform.position;
	}

	private void Update()
	{
		if (animator == null) return;

		// 1) 이동 벡터/속도 계산
		Vector2 moveVec = GetMovementVector(out _);
		float actualSpeed = GetActualSpeed();

		// 2) 히스테리시스: 시작/정지 임계값을 구분하여 안정적인 Idle/Walk 전환
		if (isMovingSmoothed)
		{
			if (actualSpeed <= fullStopSpeed)
			{
				isMovingSmoothed = false;
			}
		}
		else
		{
			if (actualSpeed >= startMoveSpeed)
			{
				isMovingSmoothed = true;
			}
		}

		// 3) 좌/우 향 보정: 상/하 이동도 측면 애니메이션을 사용하므로
		//    수평 성분이 더 크면 해당 방향으로, 수직이 더 크면 기존 좌우 유지
		if (isMovingSmoothed && moveVec.sqrMagnitude > 0.000001f)
		{
			Vector2 dir = moveVec.normalized;
			if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
			{
				lastHorizontalSign = dir.x >= 0f ? 1 : -1;
			}
			// 수직이 클 경우 lastHorizontalSign 유지 (옆 걷기 애니메이션 그대로)
		}

		// 4) Sprite flipX로 좌/우 반전
		if (spriteRenderer != null)
		{
			bool wantFlip = (lastHorizontalSign < 0);
			spriteRenderer.flipX = invertFlipX ? !wantFlip : wantFlip;
		}

		// 5) Animator에 이동 상태만 전달 (비블렌드: 측면 Idle/Walk만 사용)
		animator.SetBool(paramIsMove, isMovingSmoothed);

		// 6) 다음 프레임 대비 위치 저장
		previousPosition = rb != null ? rb.position : (Vector2)transform.position;
	}

	private Vector2 GetMovementVector(out bool agentWantsStop)
	{
		agentWantsStop = false;

		// NavMeshAgent 우선
		if (navAgent != null && navAgent.enabled)
		{
			bool agentStoppedFlag = navAgent.isStopped || !navAgent.isOnNavMesh;
			bool reached = !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + 0.01f;
			agentWantsStop = agentStoppedFlag || reached;

			Vector2 vel = new Vector2(navAgent.velocity.x, navAgent.velocity.y);
			if (vel.sqrMagnitude > 0.000001f) return vel;

			if (!agentWantsStop)
			{
				Vector2 desired = new Vector2(navAgent.desiredVelocity.x, navAgent.desiredVelocity.y);
				if (desired.sqrMagnitude > 0.000001f) return desired;

				// 최후 수단: 타겟 방향
				Vector3 target = navAgent.steeringTarget;
				Vector2 pos = (Vector2)transform.position;
				Vector2 toTarget = new Vector2(target.x - pos.x, target.y - pos.y);
				if (toTarget.sqrMagnitude > 0.000001f) return toTarget;
			}

			// 폴백: NavMeshAgent가 있지만 실제 이동은 RB/Transform으로 이뤄지는 경우
			Vector2 currentPositionAgent = rb != null ? rb.position : (Vector2)transform.position;
			Vector2 frameDeltaAgent = currentPositionAgent - previousPosition;
			float invDtAgent = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
			Vector2 fallbackFromDelta = frameDeltaAgent * invDtAgent;
			if (fallbackFromDelta.sqrMagnitude > 0.000001f) return fallbackFromDelta;

			return Vector2.zero;
	}

		// Rigidbody/Transform 델타 기반
		Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
		Vector2 frameDelta = currentPosition - previousPosition;
		float invDt = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
		return frameDelta * invDt;
	}

	private float GetActualSpeed()
	{
		if (navAgent != null && navAgent.enabled)
		{
			Vector2 v = new Vector2(navAgent.velocity.x, navAgent.velocity.y);
			float agentMag = v.magnitude;
			// 폴백: RB/Transform 델타와 비교하여 더 큰 값을 사용
			Vector2 currentPositionAgent = rb != null ? rb.position : (Vector2)transform.position;
			Vector2 frameDeltaAgent = currentPositionAgent - previousPosition;
			float invDtAgent = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
			float rbMag = (frameDeltaAgent * invDtAgent).magnitude;
			return Mathf.Max(agentMag, rbMag);
		}
		Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
		Vector2 frameDelta = currentPosition - previousPosition;
		float invDt = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
		return (frameDelta * invDt).magnitude;
	}

	/// <summary>
	/// 외부(컨트롤러/AI)에서 호출하는 공격 트리거
	/// </summary>
	public void TriggerAttack()
	{
		if (animator == null) return;

		bool hasParam = false;
		foreach (var p in animator.parameters)
		{
			if (p.name == paramAttackTrigger && p.type == AnimatorControllerParameterType.Trigger)
			{
				hasParam = true;
				break;
			}
		}
		if (!hasParam)
		{
			Debug.LogWarning($"[MonsterAnimatorSimple] Attack trigger '{paramAttackTrigger}' 가 Animator에 없습니다. " +
				$"(controller='{animator.runtimeAnimatorController?.name ?? "null"}')", animator);
			return;
		}

		animator.SetTrigger(paramAttackTrigger);
		Debug.Log("Attack트리거 호출됨!", this);
	}

	/// <summary>
	/// 외부(컨트롤러/AI)에서 호출하는 사망 트리거
	/// </summary>
	public void TriggerDeath()
	{
		if (animator == null) return;

		bool hasIsDead = false, hasDeath = false;
		foreach (var p in animator.parameters)
		{
			if (p.name == paramIsDead && p.type == AnimatorControllerParameterType.Bool) hasIsDead = true;
			if (p.name == paramDeathTrigger && p.type == AnimatorControllerParameterType.Trigger) hasDeath = true;
		}
		if (!hasIsDead || !hasDeath)
		{
			Debug.LogWarning($"[MonsterAnimatorSimple] Death 파라미터 누락. IsDead({hasIsDead}) DeathTrigger({hasDeath}) " +
				$"(controller='{animator.runtimeAnimatorController?.name ?? "null"}')", animator);
			return;
		}

		animator.SetBool(paramIsDead, true);
		animator.SetTrigger(paramDeathTrigger);
		Debug.Log("Death 트리거 호출됨!", this);
	}
}


