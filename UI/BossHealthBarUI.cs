using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 HP UI
/// - 테두리(Image)와 빨간바(Image)를 사용
/// - 스폰 연출 종료 시(OnSpawnSequenceEnded) UI 표시
/// - 보스 체력 변화에 따라 빨간바가 오른쪽→왼쪽으로 서서히 줄어듭니다.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Image borderImage;
	[SerializeField] private Image fillImage; // 빨간 바
	[SerializeField] private CanvasGroup canvasGroup;     // 선택: 비활성 대신 페이드 토글용

	[Header("Behavior")]
	[SerializeField, Tooltip("시작 시 UI를 숨긴 상태로 유지합니다.")]
	private bool startHidden = true;
	[SerializeField, Tooltip("표시/감춤 시 알파(페이드)를 사용합니다. CanvasGroup이 없으면 무시됩니다.")]
	private bool useCanvasGroup = true;
	[SerializeField, Tooltip("체력 표시 보간 속도(클수록 빠름)")]
	private float lerpSpeed = 5f;

	[Header("Boss Binding")]
	[SerializeField, Tooltip("직접 보스를 연결할 수 있습니다. 비워두면 런타임에 AttachToBoss를 호출하세요.")]
	private BossController boss;

	private float displayed = 1f;
	private float target = 1f;
	private bool isVisible = true;

	private void Awake()
	{
		// Fill 타입 강제: 오른쪽에서 왼쪽으로 줄어들게 설정
		if (fillImage != null)
		{
			fillImage.type = Image.Type.Filled;
			fillImage.fillMethod = Image.FillMethod.Horizontal;
			fillImage.fillOrigin = (int)Image.OriginHorizontal.Right;
			fillImage.fillAmount = 1f;
		}

		if (useCanvasGroup && canvasGroup == null)
		{
			canvasGroup = GetComponent<CanvasGroup>();
		}

		if (startHidden)
		{
			HideImmediate();
		}

		// 인스펙터로 연결된 보스가 있으면 즉시 바인드
		if (boss != null)
		{
			AttachToBoss(boss);
		}
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	/// <summary>
	/// 외부에서 보스를 연결합니다. (스폰 직후 호출 권장)
	/// </summary>
	public void AttachToBoss(BossController targetBoss)
	{
		if (targetBoss == null) return;
		if (boss == targetBoss) return;

		Unsubscribe();
		boss = targetBoss;

		// 초기 값 설정
		int max = Mathf.Max(1, boss.MaxHealth);
		int cur = Mathf.Clamp(boss.CurrentHealth, 0, max);
		target = displayed = (float)cur / max;
		if (fillImage != null) fillImage.fillAmount = displayed;

		// 이벤트 구독
		boss.OnHealthChanged += OnBossHealthChanged;
		boss.OnBossDied += OnBossDied;
		boss.OnSpawnSequenceEnded += OnBossSpawnSequenceEnded;
	}

	private void Unsubscribe()
	{
		if (boss == null) return;
		boss.OnHealthChanged -= OnBossHealthChanged;
		boss.OnBossDied -= OnBossDied;
		boss.OnSpawnSequenceEnded -= OnBossSpawnSequenceEnded;
	}

	private void OnBossSpawnSequenceEnded(BossController _)
	{
		Show();
	}

	private void OnBossDied(BossController _)
	{
		// 보스가 죽으면 UI 숨김(필요 시 파괴로 변경 가능)
		Hide();
	}

	private void OnBossHealthChanged(int current, int max)
	{
		max = Mathf.Max(1, max);
		current = Mathf.Clamp(current, 0, max);
		target = (float)current / max;
	}

	private void Update()
	{
		// 체력 보간
		displayed = Mathf.MoveTowards(displayed, target, Mathf.Max(0.01f, lerpSpeed) * Time.deltaTime);
		if (fillImage != null)
		{
			fillImage.fillAmount = displayed;
		}
	}

	private void Show()
	{
		if (useCanvasGroup && canvasGroup != null)
		{
			canvasGroup.alpha = 1f;
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
		}
		else
		{
			if (borderImage != null) borderImage.enabled = true;
			if (fillImage != null) fillImage.enabled = true;
		}
		isVisible = true;
	}

	private void Hide()
	{
		if (useCanvasGroup && canvasGroup != null)
		{
			canvasGroup.alpha = 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
		}
		else
		{
			if (borderImage != null) borderImage.enabled = false;
			if (fillImage != null) fillImage.enabled = false;
		}
		isVisible = false;
	}

	private void HideImmediate()
	{
		Hide();
	}
}


