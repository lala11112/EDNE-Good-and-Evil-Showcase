using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 빈 게임 오브젝트에서 보스 HP UI를 제어하는 매니저.
/// - 보스 스폰 연출(spawnAnimationDuration) 종료 시 UI 활성화
/// - 체력 변화 시 빨간 바(Image.fillAmount) 즉시 갱신 (오른쪽→왼쪽)
/// - 보스 사망 시 UI 비활성화
/// </summary>
public class BossHealthBarManager : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField, Tooltip("보스 HP UI 루트 (초기 비활성 권장)")]
	private GameObject bossHpUiRoot;
	[SerializeField, Tooltip("빨간 바 Image")]
	private Image bossHpFillImage;

	[Header("Behavior")]
	[SerializeField, Tooltip("Start 시 UI를 숨깁니다.")]
	private bool hideOnStart = true;
	[SerializeField, Tooltip("Start 시 씬에서 보스를 자동으로 찾아 연결합니다(단일 보스 가정).")]
	private bool autoFindBossOnStart = false;

	[Header("Mapping")]
	[SerializeField, Tooltip("인스펙터에서 직접 매핑할 BossController (권장)")]
	private BossController boss;


	private void Awake()
	{
		// 빨간 바를 오른쪽→왼쪽으로 줄어들도록 설정
		if (bossHpFillImage != null)
		{
			bossHpFillImage.type = Image.Type.Filled;
			bossHpFillImage.fillMethod = Image.FillMethod.Horizontal;
			// 왼쪽을 기준으로 채워지도록 설정 (감소 시 오른쪽부터 깎임)
			bossHpFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
			bossHpFillImage.fillAmount = 1f;
		}

		if (hideOnStart && bossHpUiRoot != null)
		{
			bossHpUiRoot.SetActive(false);
		}
	}

	private void Start()
	{
		// 인스펙터에 BossController가 매핑되어 있다면 우선 연결
		if (boss != null)
		{
			AttachToBoss(boss);
			return;
		}

		// 매핑이 없다면(옵션) 자동 탐색
		if (autoFindBossOnStart && boss == null)
		{
			var found = FindObjectOfType<BossController>();
			if (found != null)
			{
				AttachToBoss(found);
			}
		}
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	/// <summary>
	/// 스폰된 보스를 연결합니다. (스폰 트리거에서 호출 권장)
	/// </summary>
	public void AttachToBoss(BossController targetBoss)
	{
		if (targetBoss == null || boss == targetBoss) return;

		Unsubscribe();
		boss = targetBoss;

		// 초기값 즉시 반영
		UpdateFillImmediate();

		// 이벤트 구독
		boss.OnSpawnSequenceEnded += OnBossSpawnSequenceEnded;
		boss.OnHealthChanged += OnBossHealthChanged;
		boss.OnBossDied += OnBossDied;
	}

	private void Unsubscribe()
	{
		if (boss == null) return;
		boss.OnSpawnSequenceEnded -= OnBossSpawnSequenceEnded;
		boss.OnHealthChanged -= OnBossHealthChanged;
		boss.OnBossDied -= OnBossDied;
	}

	private void OnBossSpawnSequenceEnded(BossController _)
	{
		// 스폰 연출 종료 → UI 활성화
		if (bossHpUiRoot != null) bossHpUiRoot.SetActive(true);
		UpdateFillImmediate();
	}

	private void OnBossHealthChanged(int current, int max)
	{
		if (bossHpFillImage == null) return;
		max = Mathf.Max(1, max);
		current = Mathf.Clamp(current, 0, max);
		bossHpFillImage.fillAmount = (float)current / max;
	}

	private void OnBossDied(BossController _)
	{
		if (bossHpUiRoot != null) bossHpUiRoot.SetActive(false);
	}

	private void UpdateFillImmediate()
	{
		if (boss == null || bossHpFillImage == null) return;
		int max = Mathf.Max(1, boss.MaxHealth);
		int cur = Mathf.Clamp(boss.CurrentHealth, 0, max);
		bossHpFillImage.fillAmount = (float)cur / max;
	}
}


