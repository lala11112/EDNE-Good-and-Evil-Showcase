using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;

/// <summary>
/// Player 태그의 콜라이더가 트리거에 닿으면 보스 프리팹을 지정 위치에 소환합니다.
/// 사용법:
/// - 이 스크립트를 트리거 콜라이더가 있는 오브젝트에 추가
/// - bossPrefab, spawnPoint 지정
/// - OnTriggerEnter2D가 동작하려면 해당 트리거 오브젝트 또는 플레이어 중 하나에 Rigidbody2D가 있어야 합니다.
/// </summary>
public class BossSpawnTrigger : MonoBehaviour
{
	[Header("Spawn Settings")]
	[SerializeField] private GameObject bossPrefab;
	[SerializeField] private Transform spawnPoint;
	[SerializeField] private Transform parentOnSpawn;

	[Header("Trigger Settings")]
	[SerializeField] private string playerTag = "Player";
	[SerializeField] private bool spawnOnce = true;
	[SerializeField] private bool destroyTriggerAfterSpawn = false;

	[Header("Camera Shake (ProCamera2D)")]
	[SerializeField] private bool enableCameraShakeOnSpawn = true;
	[SerializeField] private float shakeDelaySeconds = 3f;
	[SerializeField] private float shakeDuration = 0.3f;
	[SerializeField] private Vector2 shakeStrength = new Vector2(0.8f, 0.8f);
	[SerializeField] private int shakeVibrato = 30;
	[SerializeField] private float shakeRandomness = 0.5f;

	[Header("Camera Shake SFX")]
	[SerializeField, Tooltip("쉐이크와 동시에 재생할 효과음 (선택)")]
	private AudioClip shakeSfx;
	[SerializeField, Range(0f, 1f)] private float shakeSfxVolume = 1f;

	private bool hasSpawned = false;

	private void Reset()
	{
		// 콜라이더가 있으면 자동으로 트리거화
		var col = GetComponent<Collider2D>();
		if (col != null) col.isTrigger = true;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!other.CompareTag(playerTag)) return;
		TrySpawnBoss();
	}

	/// <summary>
	/// 수동으로도 호출 가능 (예: 이벤트에서)
	/// </summary>
	public void TrySpawnBoss()
	{
		if (spawnOnce && hasSpawned) return;

		if (bossPrefab == null)
		{
			Debug.LogWarning("[BossSpawnTrigger] bossPrefab이 비어 있습니다.", this);
			return;
		}

		Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
		Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

		GameObject boss = Instantiate(bossPrefab, pos, rot, parentOnSpawn);
		Debug.Log($"[BossSpawnTrigger] 보스 소환: {boss.name} at {pos}");

		// 스폰 연출 시작 (무적/공격불가 → 연출 종료 후 해제)
		var bossController = boss.GetComponent<BossController>();
		if (bossController != null)
		{
			bossController.StartSpawnSequence();
		}

		// 카메라 쉐이크 예약
		if (enableCameraShakeOnSpawn)
		{
			Debug.Log($"[BossSpawnTrigger] 카메라 쉐이크 예약됨: {shakeDelaySeconds}초 후 실행 예정");
			// 트리거를 소멸시키려면 쉐이크 이후로 지연 파괴
			StartCoroutine(CoShakeAfterDelay(destroyTriggerAfterSpawn));
		}

		hasSpawned = true;

		// 쉐이크를 사용하지 않을 때만 즉시 파괴
		if (destroyTriggerAfterSpawn && !enableCameraShakeOnSpawn)
		{
			Destroy(gameObject);
		}
	}

	private System.Collections.IEnumerator CoShakeAfterDelay(bool destroyAfter = false)
	{
		Debug.Log("[BossSpawnTrigger] 카메라 쉐이크 대기 시작");
		if (shakeDelaySeconds > 0f)
		{
			yield return new WaitForSeconds(shakeDelaySeconds);
		}

		Debug.Log("[BossSpawnTrigger] 카메라 쉐이크 딜레이 종료, 실행 시도");
		var shaker = FindObjectOfType<ProCamera2DShake>();
		if (shaker != null)
		{
			shaker.Shake(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness);
			Debug.Log($"[BossSpawnTrigger] 카메라 쉐이크 실행 (duration={shakeDuration}, strength={shakeStrength}, vibrato={shakeVibrato}, randomness={shakeRandomness})");

			// 쉐이크 동안만 유지되는 SFX 재생 (루프 후 종료)
			GameObject audioObj = null;
			AudioSource src = null;
			if (shakeSfx != null)
			{
				Transform parent = Camera.main != null ? Camera.main.transform : transform;
				audioObj = new GameObject("ShakeSFX_Audio");
				audioObj.transform.SetParent(parent, false);
				src = audioObj.AddComponent<AudioSource>();
				src.clip = shakeSfx;
				src.volume = Mathf.Clamp01(shakeSfxVolume);
				src.loop = true;
				src.playOnAwake = false;
				src.spatialBlend = 0f; // 2D
				src.Stop();
				src.Play();
				Debug.Log($"[BossSpawnTrigger] 쉐이크 SFX 재생 시작: {shakeSfx.name} (volume={shakeSfxVolume})");
			}

			// 쉐이크 지속 시간 대기 후 SFX 정지
			if (shakeDuration > 0f)
			{
				yield return new WaitForSeconds(shakeDuration);
			}
			if (src != null)
			{
				src.Stop();
				Debug.Log("[BossSpawnTrigger] 쉐이크 종료: SFX 정지");
			}
			if (audioObj != null)
			{
				Destroy(audioObj);
			}
		}
		else
		{
			Debug.LogWarning("[BossSpawnTrigger] ProCamera2DShake를 찾을 수 없어 카메라 쉐이크를 실행하지 못했습니다.");
		}

		if (destroyAfter)
		{
			Debug.Log("[BossSpawnTrigger] 쉐이크 완료 후 트리거 오브젝트 제거");
			Destroy(gameObject);
		}
	}
}


