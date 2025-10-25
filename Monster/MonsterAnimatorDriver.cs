using UnityEngine;
using UnityEngine.AI;

public class MonsterAnimatorDriver : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    private NavMeshAgent navAgent;
    

    [Header("Movement Settings")]
    [SerializeField] private float startMoveSpeed = 0.05f; // 이동 시작 임계값 (멈춤 -> 이동)
    [SerializeField] private float fullStopSpeed = 0.001f; // 완전 정지 임계값 (Idle 전환)

    [Header("Animator Parameters (must match Animator)")]
    [SerializeField] private string paramIsMove = "isMove";
    [SerializeField] private string paramInputX = "InputX";
    [SerializeField] private string paramInputY = "InputY";

    [Header("Optional Combat Parameters")]
    [SerializeField] private bool enableCombatParameters = true;
    [SerializeField] private string paramAttackTrigger = "Attack";
    [SerializeField] private string paramDeathTrigger = "Death";
    [SerializeField] private string paramIsDead = "IsDead";

    private Vector2 previousPosition;
    private Vector2 lastDirection = Vector2.down; // 기본 방향은 아래로 설정
    private bool isMovingSmoothed = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
            // 2D 게임에서 NavMeshAgent 사용 시 Z축 이동을 막기 위해 설정
            if (navAgent != null)
            {
                navAgent.updateRotation = false; 
                navAgent.updateUpAxis = false;
            }
        }

        // 초기 위치 설정 (Awake에서 초기화)
        previousPosition = rb != null ? rb.position : (Vector2)transform.position;
    }

    private void Update()
    {
        if (animator == null) return;

        // 1. 이동 벡터 계산: NavMeshAgent 우선, 없으면 리짓바디/트랜스폼 델타
        Vector2 moveVec = GetMovementVector(out bool agentWantsStop);
        float speed = moveVec.magnitude;

        // 2. 실제 속도 기반 히스테리시스: 완전히 멈췄을 때만 Idle 전환
        float actualSpeed = GetActualSpeed();
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

        // 3. 마지막 이동 방향 업데이트 및 애니메이터 파라미터 전달
        if (isMovingSmoothed && actualSpeed > 0.0001f)
        {
            // 실제 움직임이 있을 때만 방향 업데이트
            lastDirection = moveVec.normalized;
        }

        Vector2 animDirection = lastDirection;

        // 애니메이터에 값 전달
        animator.SetBool(paramIsMove, isMovingSmoothed);
        animator.SetFloat(paramInputX, animDirection.x);
        animator.SetFloat(paramInputY, animDirection.y);

        // 다음 프레임을 위한 위치 업데이트
        previousPosition = rb != null ? rb.position : (Vector2)transform.position;
    }

    private Vector2 GetMovementVector(out bool agentWantsStop)
    {
        agentWantsStop = false;

        // NavMeshAgent 기반 (2D NavMeshPlus)
        if (navAgent != null && navAgent.enabled)
        {
            // 에이전트가 멈추려는 상황 판단: isStopped 또는 정지 거리 도달
            bool agentStoppedFlag = navAgent.isStopped || !navAgent.isOnNavMesh;
            bool reached = !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + 0.01f;
            agentWantsStop = agentStoppedFlag || reached;

            Vector2 vel = new Vector2(navAgent.velocity.x, navAgent.velocity.y);
            
            // 실제 속도가 있다면 사용
            if (vel.sqrMagnitude > 0.000001f)
            {
                return vel;
            }

            // 속도가 0이지만 에이전트가 아직 목표를 향해 가고 있다면 desiredVelocity 사용
            if (!agentWantsStop)
            {
                Vector2 desired = new Vector2(navAgent.desiredVelocity.x, navAgent.desiredVelocity.y);
                if (desired.sqrMagnitude > 0.000001f)
                    return desired;

                // 목표를 향한 방향으로 사용 (최후의 수단)
                Vector3 target = navAgent.steeringTarget;
                Vector2 pos = (Vector2)transform.position;
                Vector2 toTarget = new Vector2(target.x - pos.x, target.y - pos.y);
                return toTarget;
            }

            return Vector2.zero;
        }

        // 리짓바디/트랜스폼 델타 기반 (NavMeshAgent가 없을 때)
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
            return v.magnitude;
        }

        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 frameDelta = currentPosition - previousPosition;
        float invDt = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
        return (frameDelta * invDt).magnitude;
    }

    // Public API for AI/Controller to trigger combat animations later (기존 코드 유지)
    public void TriggerAttack(Vector2? facingDirection = null)
    {
        if (animator == null || !enableCombatParameters) return;

        Vector2 dir = facingDirection.HasValue ? facingDirection.Value.normalized : lastDirection;
        animator.SetFloat(paramInputX, dir.x);
        animator.SetFloat(paramInputY, dir.y);
        animator.SetTrigger(paramAttackTrigger);
    }

    public void TriggerDeath(Vector2? facingDirection = null)
    {
        if (animator == null || !enableCombatParameters) return;

        Vector2 dir = facingDirection.HasValue ? facingDirection.Value.normalized : lastDirection;
        animator.SetFloat(paramInputX, dir.x);
        animator.SetFloat(paramInputY, dir.y);
        animator.SetBool(paramIsDead, true);
        animator.SetTrigger(paramDeathTrigger);
    }
}