using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 아이템 베이스 클래스
/// 모든 아이템의 기본 기능을 제공합니다.
/// - 감지 범위 체크
/// - F키 입력 감지
/// - UI 표시/숨김
/// - 아이템 획득 처리
/// - 아이템 삭제 기능
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class ItemBase : MonoBehaviour
{
    [Header("아이템 데이터")]
    [Tooltip("아이템 데이터 (ScriptableObject)")]
    [SerializeField] protected ItemDataSO itemData;
    
    [Header("입력 시스템")]
    [Tooltip("Input Action Asset (F키 입력 감지용)")]
    [SerializeField] protected InputActionAsset inputActions;
    
    [Header("UI 참조")]
    [Tooltip("아이템 설명 UI (선택적, 없으면 자동 생성)")]
    [SerializeField] protected GameObject itemInfoUI;
    
    // 컴포넌트 참조
    protected Collider2D itemCollider;
    
    // 입력 액션
    protected InputAction interactAction;
    
    // 상태
    protected bool isPlayerInRange = false;
    protected bool isAcquired = false;
    protected PlayerController playerController;
    
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
        
        // Input Actions 초기화
        if (inputActions != null)
        {
            interactAction = inputActions.FindAction("Interact");
        }
        
        // PlayerController 찾기
        playerController = PlayerController.Instance;
    }
    
    protected virtual void OnEnable()
    {
        // Input Actions 활성화
        if (inputActions != null)
        {
            inputActions.Enable();
        }
        
        // 이벤트 등록
        if (interactAction != null)
        {
            interactAction.performed += OnInteractPerformed;
        }
    }
    
    protected virtual void OnDisable()
    {
        // 이벤트 해제
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
        }
        
        // Input Actions 비활성화
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }
    
    protected virtual void Update()
    {
        if (isAcquired) return;
        
        // 플레이어가 범위 내에 있는지 체크
        CheckPlayerInRange();
        
        // 범위 내에 있으면 UI 표시, 없으면 숨김
        if (isPlayerInRange)
        {
            ShowItemInfo();
            
            // F키 입력 체크 (InputActionAsset이 없을 때 직접 키 입력 감지)
            if (interactAction == null)
            {
                CheckDirectKeyInput();
            }
        }
        else
        {
            HideItemInfo();
        }
    }
    
    #endregion
    
    #region 상호작용 범위 체크
    
    /// <summary>
    /// 플레이어가 상호작용 범위 내에 있는지 체크
    /// </summary>
    protected virtual void CheckPlayerInRange()
    {
        if (playerController == null || itemData == null) return;
        
        Vector2 playerPosition = playerController.transform.position;
        Vector2 itemPosition = transform.position;
        float distance = Vector2.Distance(playerPosition, itemPosition);
        
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distance <= itemData.interactionRange;
        
        // 범위 진입/이탈 시 로그 (디버그용)
        if (wasInRange != isPlayerInRange)
        {
#if UNITY_EDITOR
            if (isPlayerInRange)
            {
                Debug.Log($"[ItemBase] {itemData.itemName} - 플레이어가 상호작용 범위 내 진입 (거리: {distance:F2})");
            }
            else
            {
                Debug.Log($"[ItemBase] {itemData.itemName} - 플레이어가 상호작용 범위 밖으로 이탈 (거리: {distance:F2})");
            }
#endif
        }
    }
    
    #endregion
    
    #region UI 표시/숨김
    
    /// <summary>
    /// 아이템 정보 UI 표시
    /// </summary>
    protected virtual void ShowItemInfo()
    {
        // TODO: UI 표시 로직 구현 (나중에 UI 시스템과 연동)
        // 현재는 로그만 출력
#if UNITY_EDITOR
        // Debug.Log($"[ItemBase] {itemData.itemName} UI 표시");
#endif
    }
    
    /// <summary>
    /// 아이템 정보 UI 숨김
    /// </summary>
    protected virtual void HideItemInfo()
    {
        // TODO: UI 숨김 로직 구현 (나중에 UI 시스템과 연동)
        // 현재는 로그만 출력
#if UNITY_EDITOR
        // Debug.Log($"[ItemBase] {itemData.itemName} UI 숨김");
#endif
    }
    
    #endregion
    
    #region 입력 처리
    
    /// <summary>
    /// F키 입력 처리 (InputActionAsset 사용 시)
    /// </summary>
    protected virtual void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // 이미 획득했거나 플레이어가 범위 내에 없으면 무시
        if (isAcquired || !isPlayerInRange) return;
        
        // 아이템 획득 처리
        AcquireItem();
    }
    
    /// <summary>
    /// 직접 키 입력 감지 (InputActionAsset이 없을 때)
    /// F키를 직접 감지합니다.
    /// </summary>
    protected virtual void CheckDirectKeyInput()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // 새로운 Input System 사용
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            AcquireItem();
        }
#else
        // 레거시 Input System 사용
        if (Input.GetKeyDown(KeyCode.F))
        {
            AcquireItem();
        }
#endif
    }
    
    #endregion
    
    #region 아이템 획득/삭제
    
    /// <summary>
    /// 아이템 획득 처리
    /// 하위 클래스에서 오버라이드하여 개별 로직 구현
    /// </summary>
    protected virtual void AcquireItem()
    {
        if (isAcquired) return;
        
        isAcquired = true;
        
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

