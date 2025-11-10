using UnityEngine;

/// <summary>
/// 아이템 데이터 ScriptableObject
/// 모든 아이템의 기본 데이터를 담습니다.
/// 기본값은 SO에서 관리하고, 개별 아이템의 특수 값은 인스펙터에서 설정합니다.
/// </summary>
[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Item/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 ID (중복 불가)")]
    public string uniqueID;
    
    [Tooltip("아이템 이름 (UI 표시용)")]
    public string itemName;
    
    [Tooltip("아이템 랭크 (UI 표시용)")]
    public ItemRank itemRank = ItemRank.D;
    
    [Tooltip("아이템 설명 (UI 표시용)")]
    [TextArea(3, 5)]
    public string itemDescription;
    
    [Tooltip("아이템 아이콘 (UI 표시용)")]
    public Sprite itemIcon;
    
    [Header("아이템 타입")]
    [Tooltip("아이템 타입 (소비/장비)")]
    public ItemType itemType = ItemType.Consumable;
    
    [Header("상호작용 설정")]
    [Tooltip("상호작용 가능 거리 (플레이어가 이 거리 내에 들어오면 UI 표시)")]
    [Range(0.5f, 5f)]
    public float interactionRange = 2f;
    
    [Tooltip("아이템 스폰 시 이펙트 (선택적)")]
    public GameObject spawnEffect;
    
    [Tooltip("아이템 획득 시 이펙트 (선택적)")]
    public GameObject acquireEffect;
    
    [Tooltip("아이템 획득 시 사운드 (선택적)")]
    public AudioClip acquireSound;
}

