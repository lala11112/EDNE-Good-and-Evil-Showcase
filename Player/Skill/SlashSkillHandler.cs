using UnityEngine;

public class SlashSkillHandler : MonoBehaviour, SkillHandlerInterface
{
	private BaseSkillData data;	// SO에서 전달된 스킬 데이터
	private GameObject owner;	// 스킬 소유자(플레이어 등)

	// [AOE 설정]
	// - aoePrefab: 선택 사항(비주얼/이펙트용). CircleCollider2D(Trigger)가 붙어있으면 사용, 없으면 자동 추가.
	// - aoeRadius: 원형 범위 반경
	// - aoeDuration: 유지 시간(초)
	// - spawnLocalOffset: 오너 기준 생성 위치 보정(x: 오른쪽, y: 위)
	// - attachToOwner: true면 지속 시간 동안 오너를 따라다님
	[SerializeField] private GameObject aoePrefab;
	[SerializeField] private float aoeRadius = 0.8f;  // 반경을 1.5에서 0.8로 줄임
	[SerializeField] private float aoeDuration = 0.2f;  // 지속시간을 0.5에서 0.2로 줄임
	[SerializeField] private Vector2 spawnLocalOffset = Vector2.zero;
	[SerializeField] private bool attachToOwner = false;

	public void Setup(BaseSkillData data, GameObject owner)
	{
		this.data = data;
		this.owner = owner;
	}

	public void Use(SkillRank rank)
	{
		// 1) 생성 위치 계산(2D)
		var ownerTf = owner != null ? owner.transform : transform;
		Vector3 spawnPos = ownerTf.position + ownerTf.TransformVector(new Vector3(spawnLocalOffset.x, spawnLocalOffset.y, 0f));

		// 2) AOE 생성(프리팹이 있으면 사용, 없으면 런타임 생성)
		GameObject aoeGO = null;

		if (aoePrefab != null)
		{
			aoeGO = Instantiate(aoePrefab, spawnPos, Quaternion.identity);

			// CircleCollider2D 보장 및 반경 설정
			if (!aoeGO.TryGetComponent<CircleCollider2D>(out var cc2d))
			{
				cc2d = aoeGO.AddComponent<CircleCollider2D>();
				cc2d.isTrigger = true;
			}
			cc2d.radius = aoeRadius;
		}
		else
		{
			aoeGO = new GameObject("Slash_AOE");
			aoeGO.transform.position = spawnPos;

			var cc2d = aoeGO.AddComponent<CircleCollider2D>();
			cc2d.isTrigger = true;
			cc2d.radius = aoeRadius;

			// 필요 시 간단한 비주얼을 붙이고 싶다면 SpriteRenderer를 추가하세요.
			// var sr = aoeGO.AddComponent<SpriteRenderer>(); ...
		}

		// 3) 오너를 따라가게 할지 여부
		if (attachToOwner && aoeGO != null)
		{
			aoeGO.transform.SetParent(ownerTf, true);
		}

		// ⭐ 4) AOE 데미지 처리 컴포넌트 추가
		if (aoeGO != null)
		{
			// 플레이어 컨트롤러 가져오기 (owner 또는 부모에서 찾기)
			PlayerController playerController = null;
			if (owner != null)
			{
				// owner에서 직접 찾기
				playerController = owner.GetComponent<PlayerController>();
				
				// 없으면 부모에서 찾기
				if (playerController == null)
				{
					playerController = owner.GetComponentInParent<PlayerController>();
				}
			}
			
			// PlayerSkillData 캐스팅
			PlayerSkillData playerSkillData = data as PlayerSkillData;
			
			if (playerController != null && playerSkillData != null)
			{
				// 플레이어 공격력 기반 데미지 계산
				float calculatedDamage = playerController.CalculateSkillDamage(playerSkillData.damagePercent);
				
				// AOEDamage 컴포넌트 추가 및 설정
				AOEDamage aoeDamage = aoeGO.AddComponent<AOEDamage>();
				aoeDamage.Setup(calculatedDamage, EntityType.Player);
				
				Debug.Log($"[SlashSkillHandler] AOE 데미지 설정 완료 - {calculatedDamage} (공격력: {playerController.GetAttackPower()}, 퍼센트: {playerSkillData.damagePercent}%)");
			}
			else
			{
				Debug.LogError($"[SlashSkillHandler] PlayerController 또는 PlayerSkillData를 찾을 수 없습니다! " +
					$"(PlayerController: {playerController != null}, PlayerSkillData: {playerSkillData != null}, owner: {owner?.name})");
			}
		}

		// 5) 일정 시간 후 제거
		Destroy(aoeGO, aoeDuration);
		
		// 영구 핸들러: Use() 후에도 핸들러는 유지됨 (Cleanup 호출 안 함)
	}
	
	/// <summary>
	/// 스킬 정리 (스킬 변경 시 호출)
	/// </summary>
	public void Cleanup()
	{
		// 영구 핸들러 방식: 상태만 초기화, 파괴하지 않음
		// (파괴는 SkillSlot에서 처리)
		
		// 진행 중인 코루틴 정리
		StopAllCoroutines();
		
		// 상태 초기화
		data = null;
		owner = null;
		
		Debug.Log("[SlashSkillHandler] Cleanup 완료 (상태 초기화)");
	}
}
