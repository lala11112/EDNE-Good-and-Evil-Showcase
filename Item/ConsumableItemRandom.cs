using UnityEngine;

/// <summary>
/// 랜덤 회복 소비 아이템
/// HP를 최소 회복 비율 ~ 최대 회복 비율 사이에서 랜덤으로 회복시킵니다.
/// </summary>
public class ConsumableItemRandom : ItemBase
{
    /// <summary>
    /// 아이템 획득 시 처리
    /// 랜덤 회복량을 계산하여 플레이어의 HP를 회복시킵니다.
    /// </summary>
    protected override void OnItemAcquired()
    {
        if (playerController == null)
        {
            Debug.LogError("[ConsumableItemRandom] PlayerController를 찾을 수 없습니다!");
            return;
        }
        
        if (itemData == null)
        {
            Debug.LogError("[ConsumableItemRandom] ItemData가 null입니다!");
            return;
        }
        
        // RandomHealItemDataSO로 캐스팅
        RandomHealItemDataSO randomHealData = itemData as RandomHealItemDataSO;
        if (randomHealData == null)
        {
            Debug.LogError($"[ConsumableItemRandom] {itemData.itemName}은 RandomHealItemDataSO가 아닙니다!");
            return;
        }
        
        // 최대 체력 가져오기
        int maxHealth = playerController.MaxHealth;
        if (maxHealth <= 0)
        {
            Debug.LogWarning("[ConsumableItemRandom] 플레이어의 최대 체력이 0 이하입니다!");
            return;
        }
        
        // 랜덤 회복 비율 계산 (최소 ~ 최대)
        float minPercent = randomHealData.MinHealPercent;
        float maxPercent = randomHealData.MaxHealPercent;
        
        // 최소값이 최대값보다 크면 교환
        if (minPercent > maxPercent)
        {
            float temp = minPercent;
            minPercent = maxPercent;
            maxPercent = temp;
            Debug.LogWarning($"[ConsumableItemRandom] 최소 회복 비율이 최대 회복 비율보다 큽니다. 자동으로 교환합니다.");
        }
        
        // 랜덤 회복 비율 생성 (최소 ~ 최대)
        float randomPercent = Random.Range(minPercent, maxPercent);
        
        // 회복량 계산 (최대 체력의 비율만큼)
        int healAmount = Mathf.RoundToInt(maxHealth * (randomPercent / 100f));
        
        // 회복량이 0 이하면 최소 1 회복
        if (healAmount <= 0)
        {
            healAmount = 1;
        }
        
        // 플레이어 회복
        playerController.Heal(healAmount);
        
        Debug.Log($"[ConsumableItemRandom] {itemData.itemName} - 체력 {healAmount} 회복 " +
                  $"(최대 체력의 {randomPercent:F1}%, 범위: {minPercent}% ~ {maxPercent}%)");
    }
    
    /// <summary>
    /// 아이템 삭제 시 처리
    /// 소비 아이템은 즉시 사용되므로 효과 제거가 필요 없습니다.
    /// </summary>
    protected override void OnItemDestroyed()
    {
        // 소비 아이템은 즉시 사용되므로 효과 제거 불필요
    }
}

