using UnityEngine;

/// <summary>
/// 아이템 베이스 클래스
/// 모든 아이템의 기본 기능을 제공합니다.
/// - 감지 범위 체크 (Trigger 사용)
/// - UI 표시/숨김
/// - 아이템 획득 처리
/// - 아이템 삭제 기능
/// 
/// ⭐ E키 입력은 PlayerController에서 중앙화 처리됩니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class ItemBase : MonoBehaviour
{
    [Header("아이템 데이터")]
    [Tooltip("아이템 데이터 (ScriptableObject)")]
    [SerializeField] protected ItemDataSO itemData;
    
    [Header("UI 참조")]
    [Tooltip("아이템 정보 UI (ItemInfoUI 컴포넌트가 있는 GameObject, null이면 씬에서 자동으로 찾습니다)")]
    [SerializeField] protected ItemInfoUI itemInfoUI;
    
    // 컴포넌트 참조
    protected Collider2D itemCollider;
    
    // 상태
    protected bool isPlayerInRange = false;
    protected bool isAcquired = false;
    protected PlayerController playerController;
    
    // UI 찾기 캐시 (성능 최적화)
    private static ItemInfoUI cachedItemInfoUI = null;
    
    #region Unity Lifecycle
    
    protected virtual void Awake()
    {
        // 컴포넌트 자동 할당
        itemCollider = GetComponent<Collider2D>();
        if (itemCollider == null)
        {
            itemCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        // Collider를 Trigger로 설정
        itemCollider.isTrigger = true;
        
        // Collider 크기를 interactionRange에 맞춰 자동 설정
        if (itemData != null && itemCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = itemData.interactionRange;
        }
        
        // PlayerController 찾기
        playerController = PlayerController.Instance;
        
        // 아이템 SO의 스폰 이펙트를 자식으로 소환
        SpawnItemEffect();
    }
    
    /// <summary>
    /// 아이템 SO의 스폰 이펙트를 자식으로 소환
    /// </summary>
    private void SpawnItemEffect()
    {
        if (itemData == null || itemData.spawnEffect == null) return;
        
        // 이펙트를 아이템의 자식으로 소환
        GameObject effect = Instantiate(itemData.spawnEffect, transform);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        
        // 이펙트는 아이템과 함께 삭제되므로 별도 삭제 로직 불필요
#if UNITY_EDITOR
        Debug.Log($"[ItemBase] 아이템 스폰 이펙트 생성: {itemData.itemName} - {itemData.spawnEffect.name}");
#endif
    }
    
    protected virtual void Update()
    {
        if (isAcquired) return;
        
        // 플레이어가 범위 내에 있으면 UI 표시, 없으면 숨김
        if (isPlayerInRange)
        {
            ShowItemInfo();
        }
        else
        {
            // 플레이어와의 거리가 10 이하일 때만 UI를 숨김
            if (playerController != null)
            {
                float distance = Vector2.Distance(transform.position, playerController.transform.position);
                if (distance <= 10f)
                {
                    HideItemInfo();
                }
            }
        }
    }
    
    #endregion
    
    #region Trigger 이벤트 (PlayerController 등록/해제)
    
    /// <summary>
    /// 플레이어가 Trigger 범위 내에 진입했을 때 호출
    /// </summary>
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어인지 확인
        if (playerController == null || other.gameObject != playerController.gameObject) return;
        
        // 이미 획득한 아이템이면 무시
        // if (isAcquired) return;
        
        // 거리 체크 (interactionRange 내에 있는지 확인)
        if (itemData != null)
        {
            float distance = Vector2.Distance(transform.position, playerController.transform.position);
            if (distance > itemData.interactionRange) return;
        }
        
        // 범위 내 진입 처리
        isPlayerInRange = true;
        
        // PlayerController에 등록
        if (playerController != null)
        {
            playerController.RegisterNearbyItem(this);
        }
    }
    
    /// <summary>
    /// 플레이어가 Trigger 범위 밖으로 나갔을 때 호출
    /// </summary>
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        // 플레이어인지 확인
        if (playerController == null || other.gameObject != playerController.gameObject) return;
        
        // 범위 밖 이탈 처리
        isPlayerInRange = false;
        
        // PlayerController에서 해제
        if (playerController != null)
        {
            playerController.UnregisterNearbyItem(this);
        }
    }
    
    #endregion
    
    #region UI 표시/숨김
    
    /// <summary>
    /// ItemInfoUI 찾기 (캐싱 사용)
    /// </summary>
    private ItemInfoUI FindItemInfoUI()
    {
        // 먼저 Inspector에서 할당된 UI 확인
        if (itemInfoUI != null) return itemInfoUI;
        
        // 싱글톤 인스턴스 확인 (가장 빠름)
        if (ItemInfoUI.Instance != null)
        {
            itemInfoUI = ItemInfoUI.Instance;
            cachedItemInfoUI = itemInfoUI;
            return itemInfoUI;
        }
        
        // 캐시된 UI 확인
        if (cachedItemInfoUI != null) return cachedItemInfoUI;
        
        // 자식 오브젝트에서 찾기 (비활성화된 오브젝트 포함)
        itemInfoUI = GetComponentInChildren<ItemInfoUI>(true);
        if (itemInfoUI != null)
        {
            cachedItemInfoUI = itemInfoUI;
            return itemInfoUI;
        }
        
        // 씬에서 찾기 (비활성화된 오브젝트 포함)
        itemInfoUI = FindObjectOfType<ItemInfoUI>(true);
        if (itemInfoUI != null)
        {
            cachedItemInfoUI = itemInfoUI;
            return itemInfoUI;
        }
        
        return null;
    }
    
    /// <summary>
    /// 아이템 정보 UI 표시
    /// 플레이어가 아이템 범위 내에 진입했을 때 호출됩니다.
    /// 
    /// UI에 표시할 정보:
    /// - 아이템 이름: itemData.itemName
    /// - 아이템 랭크: itemData.itemRank
    /// - 아이템 설명: itemData.itemDescription
    /// - 아이템 아이콘: itemData.itemIcon
    /// - 획득 안내: "아이템 획득 (E)"
    /// </summary>
    protected virtual void ShowItemInfo()
    {
        // ItemInfoUI 찾기
        ItemInfoUI ui = FindItemInfoUI();
        
        if (ui == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ItemBase] ItemInfoUI 컴포넌트를 찾을 수 없습니다. {itemData?.itemName ?? "Unknown"} 아이템의 UI가 표시되지 않습니다. 씬에 ItemInfoUI 컴포넌트가 있는 GameObject가 있는지 확인하세요.");
#endif
            return;
        }
        
        // 아이템 데이터가 있으면 UI에 정보 표시
        if (itemData != null)
        {
            ui.ShowItemInfo(itemData);
        }
    }
    
    /// <summary>
    /// 아이템 정보 UI 숨김
    /// 플레이어가 아이템 범위 밖으로 나갔을 때 호출됩니다.
    /// </summary>
    protected virtual void HideItemInfo()
    {
        // ItemInfoUI 찾기
        ItemInfoUI ui = FindItemInfoUI();
        
        if (ui != null && itemData != null)
        {
            // 이 아이템이 현재 표시 중인 아이템인 경우에만 UI를 숨김
            ui.HideItemInfo(itemData);
        }
    }
    
    #endregion
    
    #region 아이템 획득/삭제
    
    /// <summary>
    /// 아이템 획득 처리
    /// PlayerController에서 호출됩니다.
    /// 하위 클래스에서 오버라이드하여 개별 로직 구현
    /// </summary>
    public virtual void AcquireItem()
    {
        if (isAcquired) return;
        
        isAcquired = true;
        
        // UI 숨김 처리 (아이템 획득 시 즉시 UI 사라지게 함)
        HideItemInfo();
        
        // 플레이어 범위 상태 초기화
        isPlayerInRange = false;
        
        // PlayerController에서 아이템 등록 해제
        if (playerController != null)
        {
            playerController.UnregisterNearbyItem(this);
        }
        
        // 아이템 획득 이펙트 재생
        PlayAcquireEffect();
        
        // 아이템 획득 사운드 재생
        PlayAcquireSound();
        
        // 하위 클래스의 획득 로직 실행
        OnItemAcquired();
        
        // 아이템 삭제 (소비 아이템은 즉시 삭제, 장비 아이템은 인벤토리에 등록 후 삭제)
        DestroyItem();
    }
    
    /// <summary>
    /// 아이템 획득 시 하위 클래스에서 구현할 로직
    /// </summary>
    protected abstract void OnItemAcquired();
    
    /// <summary>
    /// 아이템 삭제 (기본 구현)
    /// 아이템이 사라질 때 호출됩니다.
    /// 하위 클래스에서 오버라이드하여 추가 로직 구현 가능
    /// </summary>
    public virtual void DestroyItem()
    {
        if (isAcquired)
        {
            // 아이템 삭제 시 하위 클래스의 정리 로직 실행
            OnItemDestroyed();
        }
        
        // RoomController에서 아이템 제거
        UnregisterFromRoomController();
        
        // 오브젝트 파괴
        Destroy(gameObject);
    }
    
    /// <summary>
    /// RoomController에서 아이템 등록 해제
    /// </summary>
    protected virtual void UnregisterFromRoomController()
    {
        // 현재 방의 RoomController 찾기
        RoomController roomController = FindRoomController();
        if (roomController != null)
        {
            roomController.UnregisterItem(this);
        }
    }
    
    /// <summary>
    /// 현재 방의 RoomController 찾기
    /// </summary>
    protected virtual RoomController FindRoomController()
    {
        // 부모 오브젝트에서 RoomController 찾기
        RoomController roomController = GetComponentInParent<RoomController>();
        if (roomController == null)
        {
            // 씬에서 가장 가까운 RoomController 찾기
            RoomController[] allRooms = FindObjectsOfType<RoomController>();
            float minDistance = float.MaxValue;
            RoomController closestRoom = null;
            
            foreach (var room in allRooms)
            {
                float distance = Vector2.Distance(transform.position, room.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestRoom = room;
                }
            }
            
            roomController = closestRoom;
        }
        
        return roomController;
    }
    
    /// <summary>
    /// 아이템 삭제 시 하위 클래스에서 구현할 로직
    /// 효과 제거 등 정리 작업 수행
    /// </summary>
    protected virtual void OnItemDestroyed()
    {
        // 하위 클래스에서 오버라이드하여 구현
    }
    
    #endregion
    
    #region 이펙트/사운드
    
    /// <summary>
    /// 아이템 획득 이펙트 재생
    /// </summary>
    protected virtual void PlayAcquireEffect()
    {
        if (itemData == null || itemData.acquireEffect == null) return;
        
        GameObject effect = Instantiate(itemData.acquireEffect, transform.position, Quaternion.identity);
        
        // 이펙트 자동 삭제 (파티클 시스템이 끝나면 자동 삭제되도록)
        ParticleSystem particles = effect.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            Destroy(effect, particles.main.duration + particles.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(effect, 2f); // 기본 2초 후 삭제
        }
    }
    
    /// <summary>
    /// 아이템 획득 사운드 재생
    /// </summary>
    protected virtual void PlayAcquireSound()
    {
        if (itemData == null || itemData.acquireSound == null) return;
        
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D 사운드
        }
        
        audioSource.PlayOneShot(itemData.acquireSound);
    }
    
    #endregion
    
    #region 프로퍼티
    
    /// <summary>아이템 데이터</summary>
    public ItemDataSO ItemData => itemData;
    
    /// <summary>아이템이 획득되었는지 여부</summary>
    public bool IsAcquired => isAcquired;
    
    /// <summary>플레이어가 범위 내에 있는지 여부</summary>
    public bool IsPlayerInRange => isPlayerInRange;
    
    #endregion
}

