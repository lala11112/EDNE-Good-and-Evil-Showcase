using UnityEngine;

/// <summary>
/// 간단한 파이어볼 투사체
/// ISkillProjectile 구현으로 MonsterController와 호환
/// </summary>
public class SimpleFireball : MonoBehaviour, ISkillProjectile
{
    [Header("설정")]
    [SerializeField] private float lifetime = 5f; // 자동 파괴 시간

    [Header("VFX (투사체 크기에 정합)")]
    [SerializeField] private bool useProjectileVFX = false;
    [SerializeField] private GameObject projectileVFXPrefab;
    [Tooltip("프리팹이 Scale=1일 때의 로컬 기준 크기 (가로, 세로). 콜라이더 로컬 size를 이 값으로 나눠 최종 스케일 계산")]
    [SerializeField] private Vector2 vfxUnitLocalSize = new Vector2(1f, 1f);
    [Tooltip("VFX 사용 시 투사체 스프라이트를 숨깁니다")]
    [SerializeField] private bool hideSpriteWhenUsingVFX = false;
	[Tooltip("CircleCollider2D에 맞출 때 지름에 곱할 배수 (1=동일)")]
	[SerializeField] private float circleScaleMultiplier = 1f;
	[Tooltip("CircleCollider2D에 맞출 때 추가 패딩(유닛, 양쪽 합산)")]
	[SerializeField] private float circlePadding = 0f;
	[Tooltip("원형일 때 균등 스케일(가로=세로)을 강제합니다. (엿가락처럼 늘어짐 방지)")]
	[SerializeField] private bool uniformScaleForCircle = true;
	[Tooltip("VFX를 투사체 진행 방향(속도)으로 회전시킵니다")]
	[SerializeField] private bool orientVFXToVelocity = true;
	[Tooltip("프리팹의 진행축 (왼->오라면 (1,0), 위로라면 (0,1))")]
	[SerializeField] private Vector2 vfxForwardAxis = Vector2.right;

	[Header("정지 시 대체 VFX")]
	[Tooltip("정지(저속) 상태일 때 다른 VFX를 사용합니다")]
	[SerializeField] private bool useIdleVFXWhenStationary = false;
	[Tooltip("정지(저속) 상태에서 표시할 VFX 프리팹")]
	[SerializeField] private GameObject idleVFXPrefab;
	[Tooltip("이 속도(유닛/초) 이하이면 정지로 간주")]
	[SerializeField] private float stationarySpeedThreshold = 0.05f;
	[Tooltip("정지 VFX만 별도의 로컬 스케일을 적용합니다 (피벗 스케일에 추가 곱)")]
	[SerializeField] private bool idleVFXOverrideScale = false;
	[SerializeField] private Vector2 idleVFXLocalScaleMultiplier = Vector2.one;
	[Tooltip("정지 VFX 전용 Circle 지름 배수를 사용합니다 (일반 배수와 분리)")]
	[SerializeField] private bool idleCircleScaleOverride = false;
	[Tooltip("정지 VFX에만 적용할 CircleCollider2D 지름 배수 (1=동일)")]
	[SerializeField] private float idleCircleScaleMultiplier = 1f;

	[Header("SFX")]
	[Tooltip("파이어볼이 이동하기 시작할 때 재생할 효과음")]
	[SerializeField] private AudioClip moveStartSfx;
	[Range(0f, 2f)]
	[SerializeField] private float moveStartSfxVolume = 1f;
	[Tooltip("효과음을 2D로 재생하여 거리 감쇠를 제거합니다")]
	[SerializeField] private bool moveStartSfx2D = true;
    
    private float damage;
    private float speed;
    private EntityType attackerType;
    private MonoBehaviour ownerController;
    private bool isPiercing; // 관통 여부
    
    #region ISkillProjectile 구현
    
    public void SetSkillData(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        
        Debug.Log($"[SimpleFireball] 데미지: {damage}, 속도: {speed}");
    }
    
    public void SetAttackerType(EntityType type)
    {
        this.attackerType = type;
        
        Debug.Log($"[SimpleFireball] 공격자 타입: {type}");
    }
    
    public void SetOwnerController(MonoBehaviour owner)
    {
        this.ownerController = owner;
    }
    
    public void SetPiercing(bool isPiercing)
    {
        this.isPiercing = isPiercing;
        
        Debug.Log($"[SimpleFireball] 관통 여부: {(isPiercing ? "관통형" : "충돌 시 파괴")}");
    }
    
	/// <summary>
	/// 스킬 슬롯 타입 가져오기 (ISkillProjectile 인터페이스 구현)
	/// 몬스터 스킬은 플레이어 스킬이 아니므로 null 반환
	/// </summary>
	public SkillSlotType? GetSkillSlotType()
	{
		return null; // 몬스터 투사체는 슬롯 타입이 없음
	}
	
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifetime);

		if (useProjectileVFX && (projectileVFXPrefab != null || (useIdleVFXWhenStationary && idleVFXPrefab != null)))
        {
            // 피벗 생성: 방향/크기는 피벗에 적용, 프리팹은 로컬 정렬 유지
			var pivot = new GameObject("ProjectileVFXPivot");
			pivot.transform.SetParent(transform, false);
			pivot.transform.localPosition = Vector3.zero;
			pivot.transform.localRotation = Quaternion.identity;
			pivot.transform.localScale = Vector3.one;
			_vfxPivot = pivot.transform;

            // 콜라이더 기반 크기/오프셋 계산 (가능하면)
            Vector2 localSize = Vector2.one;
            Vector2 localOffset = Vector2.zero;
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                if (col is BoxCollider2D bc)
                {
                    localSize = bc.size;
                    localOffset = bc.offset;
                }
                else if (col is CircleCollider2D cc)
                {
					float d = cc.radius * 2f;
					// 배수 및 패딩 적용 (원형 기준으로 동일하게 가로/세로에 반영)
					d = (d * Mathf.Max(0.001f, circleScaleMultiplier)) + Mathf.Max(0f, circlePadding);
                    localSize = new Vector2(d, d);
                    localOffset = cc.offset;
                }
                else if (col is CapsuleCollider2D cap)
                {
                    localSize = cap.size;
                    localOffset = cap.offset;
                }
            }
            else
            {
                // 스프라이트 기반 근사 (월드 크기를 로컬로 환산)
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Vector2 worldSize = sr.bounds.size;
                    Vector3 lossy = transform.lossyScale;
                    float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
                    float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;
                    localSize = new Vector2(worldSize.x / sx, worldSize.y / sy);
                }
            }

			pivot.transform.localPosition = (Vector3)localOffset;
			float unitX = Mathf.Max(0.001f, vfxUnitLocalSize.x);
			float unitY = Mathf.Max(0.001f, vfxUnitLocalSize.y);

			// 원형이면 균등 스케일 강제 (엿가락 방지)
			if (Mathf.Approximately(localSize.x, localSize.y) && uniformScaleForCircle)
			{
				// 프리팹 기준 최대 축을 사용해 정사각형으로 스케일
				float unitMax = Mathf.Max(unitX, unitY);
				float s = localSize.x / unitMax;
				pivot.transform.localScale = new Vector3(s, s, 1f);
			}
			else
			{
				pivot.transform.localScale = new Vector3(localSize.x / unitX, localSize.y / unitY, 1f);
			}

			// 이동 VFX 인스턴스 (있으면)
			if (projectileVFXPrefab != null)
			{
				var moveVfx = Instantiate(projectileVFXPrefab, pivot.transform);
				moveVfx.transform.localPosition = Vector3.zero;
				moveVfx.transform.localRotation = Quaternion.identity;
				moveVfx.transform.localScale = Vector3.one;
				_vfxObject = moveVfx;
				SetupVFXObject(moveVfx);
			}

			// 정지 VFX 인스턴스 (옵션)
			if (useIdleVFXWhenStationary && idleVFXPrefab != null)
			{
				var idleVfx = Instantiate(idleVFXPrefab, pivot.transform);
				idleVfx.transform.localPosition = Vector3.zero;
				idleVfx.transform.localRotation = Quaternion.identity;

				// 기본은 1배, 필요 시 로컬 배수 적용
				Vector3 idleScale = idleVFXOverrideScale ? new Vector3(idleVFXLocalScaleMultiplier.x, idleVFXLocalScaleMultiplier.y, 1f) : Vector3.one;

				// Circle 전용 배수 분리 적용: 피벗은 일반 배수로 스케일되어 있으므로
				// 자식에 (idleMultiplier / 일반Multiplier) 비율을 추가 곱해 정지 VFX만 분리
				if (idleCircleScaleOverride && col is CircleCollider2D)
				{
					float baseMult = Mathf.Max(0.001f, circleScaleMultiplier);
					float idleMult = Mathf.Max(0.001f, idleCircleScaleMultiplier);
					float ratio = idleMult / baseMult;
					idleScale = new Vector3(idleScale.x * ratio, idleScale.y * ratio, 1f);
				}

				idleVfx.transform.localScale = idleScale;
				_idleVfxObject = idleVfx;
				SetupVFXObject(idleVfx);
			}

			// 초기 활성 상태: 속도 기준으로 결정
			UpdateActiveVFX(initialEvaluate: true);

            if (hideSpriteWhenUsingVFX)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
        }

		_rb2d = GetComponent<Rigidbody2D>();
    }
    
	private void LateUpdate()
	{
		// 이동/정지 상태에 따라 VFX 토글
		UpdateActiveVFX();

		// 이동 시작 시 효과음 재생
		if (!_hasPlayedMoveSound && _rb2d != null && _rb2d.linearVelocity.sqrMagnitude > 0.0001f)
		{
			_hasPlayedMoveSound = true;
			PlayMoveStartSound();
		}

		if (!orientVFXToVelocity || _vfxPivot == null) return;

		Vector2 dirWorld;
		if (_rb2d != null && _rb2d.linearVelocity.sqrMagnitude > 0.0001f)
		{
			dirWorld = _rb2d.linearVelocity.normalized;
		}
		else
		{
			// 폴백: 현재 오른쪽을 진행축으로 사용
			dirWorld = transform.right;
		}

		// 부모 공간 기준으로 변환해 로컬 회전 적용
		Vector3 localDir = transform.InverseTransformDirection(new Vector3(dirWorld.x, dirWorld.y, 0f));
		if (localDir.sqrMagnitude < 0.0001f) return;
		localDir.Normalize();
		Vector3 fwd = new Vector3(vfxForwardAxis.x, vfxForwardAxis.y, 0f);
		_vfxPivot.localRotation = Quaternion.FromToRotation(fwd, localDir);
	}

	private void PlayMoveStartSound()
	{
		if (moveStartSfx == null) return;

		if (moveStartSfx2D)
		{
			// 2D 사운드: 거리 감쇠 없음
			var tempAudioSource = gameObject.AddComponent<AudioSource>();
			tempAudioSource.clip = moveStartSfx;
			tempAudioSource.volume = moveStartSfxVolume;
			tempAudioSource.spatialBlend = 0f; // 2D
			tempAudioSource.playOnAwake = false;
			tempAudioSource.Play();
			Destroy(tempAudioSource, moveStartSfx.length + 0.1f);
		}
		else
		{
			// 3D 사운드: 위치 기반
			AudioSource.PlayClipAtPoint(moveStartSfx, transform.position, moveStartSfxVolume);
		}
	}

	// private state for orientation
	private Transform _vfxPivot;
	private GameObject _vfxObject;
	private GameObject _idleVfxObject;
	private Rigidbody2D _rb2d;
	private bool _hasPlayedMoveSound = false;

	private void SetupVFXObject(GameObject vfx)
	{
		// 파티클 로컬 기준으로 강제
		var pss = vfx.GetComponentsInChildren<ParticleSystem>(true);
		for (int i = 0; i < pss.Length; i++)
		{
			var ps = pss[i];
			var main = ps.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Local;
			main.scalingMode = ParticleSystemScalingMode.Hierarchy;
			var vel = ps.velocityOverLifetime;
			vel.space = ParticleSystemSimulationSpace.Local;
			// 선형으로 늘어지는 현상 방지: Trails/Stretch 영향 제거
			var trails = ps.trails;
			if (trails.enabled) { trails.enabled = false; }
			ps.Play(true);
		}
		var psrs = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
		for (int i = 0; i < psrs.Length; i++)
		{
			try
			{
				psrs[i].alignment = ParticleSystemRenderSpace.Local;
				psrs[i].renderMode = ParticleSystemRenderMode.Billboard;
				psrs[i].velocityScale = 0f;
				psrs[i].lengthScale = 1f;
				psrs[i].cameraVelocityScale = 0f;
			}
			catch { }
		}
		// TrailRenderer/LineRenderer가 있으면 비활성화 (길쭉한 선 방지)
		var trs = vfx.GetComponentsInChildren<TrailRenderer>(true);
		for (int i = 0; i < trs.Length; i++) { trs[i].enabled = false; }
		var lrs = vfx.GetComponentsInChildren<LineRenderer>(true);
		for (int i = 0; i < lrs.Length; i++) { lrs[i].enabled = false; }
	}

	private void UpdateActiveVFX(bool initialEvaluate = false)
	{
		if (!useProjectileVFX) return;
		if (_vfxObject == null && _idleVfxObject == null) return;

		float speedMag = 0f;
		if (_rb2d != null)
		{
			speedMag = _rb2d.linearVelocity.magnitude;
		}

		bool shouldUseIdle = useIdleVFXWhenStationary && _idleVfxObject != null && speedMag <= Mathf.Max(0f, stationarySpeedThreshold);

		// 초기에 둘 중 하나만 활성화
		if (initialEvaluate)
		{
			if (_vfxObject != null) _vfxObject.SetActive(!shouldUseIdle);
			if (_idleVfxObject != null) _idleVfxObject.SetActive(shouldUseIdle);
			_lastIdleActive = shouldUseIdle;
			return;
		}

		// 상태 변화시에만 토글
		if (shouldUseIdle != _lastIdleActive)
		{
			if (_vfxObject != null) _vfxObject.SetActive(!shouldUseIdle);
			if (_idleVfxObject != null) _idleVfxObject.SetActive(shouldUseIdle);
			_lastIdleActive = shouldUseIdle;
		}
	}

	private bool _lastIdleActive = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // EntityController가 있는 대상과 충돌 시
        EntityController target = collision.GetComponent<EntityController>();
        if (target != null)
        {
            // 데미지 적용 시도 (팀킬 방지 포함)
            bool damageApplied = target.ApplyDamage((int)damage, attackerType);
            
            if (damageApplied)
            {
                // ✅ 적에게 데미지를 줬음 -> 관통 여부에 따라 파괴 결정
                Debug.Log($"[SimpleFireball] {target.name}에게 {damage} 데미지!");
                
                if (!isPiercing)
                {
                    // 비관통형: 적에게 맞았으므로 파괴
                    Debug.Log($"[SimpleFireball] 비관통형 - 충돌로 인해 파괴됨");
                    Destroy(gameObject);
                }
                else
                {
                    // 관통형: 적을 뚫고 계속 날아감
                    Debug.Log($"[SimpleFireball] 관통형 - 파괴되지 않고 계속 날아감");
                }
            }
            else
            {
                // ❌ 아군이라 데미지가 안 들어감 -> 무조건 통과 (파괴 안함)
                Debug.Log($"[SimpleFireball] {target.name}은 아군이라 통과!");
            }
        }
    }
    
    #endregion
}

