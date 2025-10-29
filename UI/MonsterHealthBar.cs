using UnityEngine;

/// <summary>
/// 몬스터 공통 체력바 (Sprite 기반)
/// - 자식 Transform의 X 스케일로 채우기 표현
/// - 콜라이더 상단을 따라가며, 빌보드/자동 숨김/부드러운 보간 지원
/// </summary>
public class MonsterHealthBar : MonoBehaviour
{
	[Header("위치/빌보드 대상")]
	[Tooltip("월드에서 위치/회전을 적용할 체력바 Transform (자식 오브젝트를 지정하세요)")]
	[SerializeField] private Transform barTransform;     // 위치/회전 적용 대상 (반드시 몬스터 자식)
	[Header("Sprite 참조")]
	[Tooltip("체력바 루트 오브젝트 (활성/비활성 토글 대상)")]
	[SerializeField] private GameObject spriteRoot;      // 배경/테두리 루트 (활성 토글용)
	[Tooltip("채움 바 Transform (X 스케일 0~1로 채우기 비율을 표현)")]
	[SerializeField] private Transform fillTransform;    // X 스케일을 0~1로 조절할 Transform
	[Tooltip("피벗이 가운데여도 좌->우로만 깎이도록 왼쪽 가장자리를 고정")]
	[SerializeField] private bool keepLeftAnchored = true;

	[Header("팔로우 옵션")]
	[Tooltip("콜라이더 상단을 따라다니도록 위치를 매 프레임 갱신")]
	[SerializeField] private bool followColliderTop = true;
	[Tooltip("콜라이더 상단 기준 위쪽(Y) 오프셋 (유닛)")]
	[SerializeField] private float verticalOffset = 0.2f; // 콜라이더 위로 오프셋

	[Header("표시 옵션")]
	[Tooltip("카메라를 바라보도록 회전 (2D에서 Z축 정렬)")]
	[SerializeField] private bool billboard = true;      // 카메라 바라보기
	[Tooltip("씬 시작 시 체력바를 숨긴 상태로 시작")]
	[SerializeField] private bool startHidden = true;     // 처음에는 숨김
	[Tooltip("체력이 가득 찼을 때 일정 시간 뒤 자동으로 숨김")]
	[SerializeField] private bool autoHideAtFull = true; // 가득 찼을 때 자동 숨김
	[Tooltip("마지막 체력 변화 이후 숨김까지 대기 시간(초)")]
	[SerializeField] private float hideDelay = 1.0f;     // 체력 변화 후 숨김 지연

	[Header("애니메이션")]
	[Tooltip("체력 표시 보간 속도 (값이 클수록 빠르게 따라감)")]
	[SerializeField] private float smoothSpeed = 8f;     // 보간 속도
	[Tooltip("체력 표시를 즉시 갱신(보간 없음)")]
	[SerializeField] private bool instantUpdate = true;

	[Header("툴팁")]
	[SerializeField] private bool enableTooltip = true;
	[SerializeField] private string tooltipFormat = "{0}\nHP {1}/{2}"; // {name}, {cur}, {max}
	[SerializeField] private Vector3 tooltipOffset = new Vector3(0f, 0.3f, 0f);

	private EntityController entity;
	private Collider2D col2d;
	private Collider col3d;
	private float displayed01 = 1f;
	private float target01 = 1f;
	private float hideTimer = 0f;
	private float initialFillScaleX = 1f;
	// 왼쪽 고정을 위한 값들
	private SpriteRenderer fillSpriteRenderer;
	private float baseSpriteWidthLocal = 0f; // sprite.bounds.size.x (local) * 1
	private float initialLocalPosX = 0f;
	private float initialLeftEdgeLocalX = 0f;

	private void Awake()
	{
		entity = GetComponentInParent<EntityController>();
		col2d = GetComponentInParent<Collider2D>();
		col3d = (col2d == null) ? GetComponentInParent<Collider>() : null;

		// 위치/회전 적용 대상 기본값: 자신의 Transform
		if (barTransform == null)
		{
			barTransform = transform;
		}

		// 안전 장치: 콜라이더가 붙은 동일 오브젝트를 이동하면 물리와 상호작용하여 튕김 현상이 발생할 수 있음
		bool sameAsColliderHolder =
			(col2d != null && col2d.transform == barTransform) ||
			(col3d != null && col3d.transform == barTransform);
		if (sameAsColliderHolder)
		{
			Debug.LogError(
				$"[MonsterHealthBar] '{gameObject.name}'에서 barTransform이 콜라이더가 붙은 동일 오브젝트입니다. 자식 오브젝트(체력바) Transform을 barTransform에 지정하세요. followColliderTop을 임시로 비활성화합니다.");
			followColliderTop = false; // 루트(콜라이더 보유) 이동 방지
		}

		if (fillTransform != null)
		{
			initialFillScaleX = Mathf.Abs(fillTransform.localScale.x) > 0.0001f ? fillTransform.localScale.x : 1f;
			if (keepLeftAnchored)
			{
				fillSpriteRenderer = fillTransform.GetComponent<SpriteRenderer>();
				if (fillSpriteRenderer == null)
				{
					fillSpriteRenderer = fillTransform.GetComponentInChildren<SpriteRenderer>();
				}
				if (fillSpriteRenderer != null && fillSpriteRenderer.sprite != null)
				{
					// 스프라이트 로컬 폭 (피벗 무관), 초기 로컬 위치 및 왼쪽 가장자리 계산
					baseSpriteWidthLocal = fillSpriteRenderer.sprite.bounds.size.x; // 로컬 단위
					initialLocalPosX = fillTransform.localPosition.x;
					float initialLocalWidth = baseSpriteWidthLocal * initialFillScaleX;
					initialLeftEdgeLocalX = initialLocalPosX - (initialLocalWidth * 0.5f);
				}
			}
		}
	}

	private void OnEnable()
	{
		RefreshImmediate();
		if (startHidden)
		{
			SetVisible(false);
			// 시작 시 강제로 숨긴 뒤, 이후 체력 변화가 생기면 로직에 의해 자동으로 표시됨
		}
	}

	private void Update()
	{
		if (entity == null)
		{
			return;
		}

		// 콜라이더 상단 위치 추적 (barTransform만 이동)
		if (followColliderTop && barTransform != null)
		{
			Vector3 topPos = transform.position;
			if (col2d != null)
			{
				Bounds b = col2d.bounds;
				topPos = new Vector3(b.center.x, b.max.y + verticalOffset, barTransform.position.z);
			}
			else if (col3d != null)
			{
				Bounds b = col3d.bounds;
				topPos = new Vector3(b.center.x, b.max.y + verticalOffset, barTransform.position.z);
			}
			barTransform.position = topPos;
		}

		int max = Mathf.Max(1, entity.MaxHealth);
		int cur = Mathf.Clamp(entity.CurrentHealth, 0, max);
		target01 = (float)cur / max;

		// 즉시 갱신 또는 보간 선택
		displayed01 = (instantUpdate || smoothSpeed <= 0f)
			? target01
			: Mathf.MoveTowards(displayed01, target01, smoothSpeed * Time.deltaTime);

		if (fillTransform != null)
		{
			Vector3 ls = fillTransform.localScale;
			ls.x = Mathf.Max(0f, displayed01 * initialFillScaleX);
			fillTransform.localScale = ls;

			if (keepLeftAnchored && fillSpriteRenderer != null && baseSpriteWidthLocal > 0.000001f)
			{
				// 새로운 로컬 폭에 맞춰 중심을 왼쪽 기준으로 재배치
				float newLocalWidth = baseSpriteWidthLocal * ls.x;
				Vector3 lp = fillTransform.localPosition;
				lp.x = initialLeftEdgeLocalX + (newLocalWidth * 0.5f);
				fillTransform.localPosition = lp;
			}
		}

		bool isDead = cur <= 0;
		if (isDead)
		{
			SetVisible(false);
		}
		else
		{
			if (!Mathf.Approximately(displayed01, target01) || target01 < 1f)
			{
				SetVisible(true);
				hideTimer = (autoHideAtFull && target01 >= 1f) ? 0.25f : hideDelay;
			}

			if (hideTimer > 0f)
			{
				hideTimer -= Time.deltaTime;
				if (hideTimer <= 0f && autoHideAtFull && target01 >= 1f)
				{
					SetVisible(false);
				}
			}
		}

		if (billboard && Camera.main != null && barTransform != null)
		{
			barTransform.forward = Camera.main.transform.forward;
		}
	}

	private void RefreshImmediate()
	{
		if (entity == null) return;
		int max = Mathf.Max(1, entity.MaxHealth);
		int cur = Mathf.Clamp(entity.CurrentHealth, 0, max);
		target01 = (float)cur / max;
		displayed01 = target01;

		if (fillTransform != null)
		{
			Vector3 ls = fillTransform.localScale;
			ls.x = Mathf.Max(0f, displayed01 * initialFillScaleX);
			fillTransform.localScale = ls;

			if (keepLeftAnchored && fillSpriteRenderer != null && baseSpriteWidthLocal > 0.000001f)
			{
				float newLocalWidth = baseSpriteWidthLocal * ls.x;
				Vector3 lp = fillTransform.localPosition;
				lp.x = initialLeftEdgeLocalX + (newLocalWidth * 0.5f);
				fillTransform.localPosition = lp;
			}
		}

		SetVisible(autoHideAtFull ? displayed01 < 1f : true);
	}

	private void SetVisible(bool v)
	{
		if (spriteRoot != null) spriteRoot.SetActive(v);
	}
}


