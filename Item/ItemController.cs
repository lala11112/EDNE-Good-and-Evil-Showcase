using UnityEngine;
using System.Collections;

/// <summary>
/// 아이템 컨트롤러 (싱글톤)
/// 아이템 효과를 중앙에서 관리합니다.
/// - 아이템 등록/제거
/// - 아이템별 효과 데이터 관리
/// - 게임 이벤트 구독 및 효과 적용
/// </summary>
public class ItemController : MonoBehaviour
{
    #region 싱글톤 패턴
    
    private static ItemController instance;
    
    /// <summary>
    /// ItemController 인스턴스 (싱글톤)
    /// </summary>
    public static ItemController Instance
    {
        get
        {
            if (instance == null)
            {
                // 씬에서 ItemController 찾기
                instance = FindObjectOfType<ItemController>();
                
                // 없으면 새로 생성
                if (instance == null)
                {
                    GameObject go = new GameObject("ItemController");
                    instance = go.AddComponent<ItemController>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    #endregion
    
    #region 아이템 활성화 상태
    
    /// <summary>S랭크 운명의 주사위 활성화 여부</summary>
    private bool isDiceActive = false;
    
    /// <summary>A랭크 확신의 사슬 활성화 여부</summary>
    private bool isChainActive = false;
    
    /// <summary>B랭크 스텔라 프로토콜 활성화 여부</summary>
    private bool isStellaActive = false;
    
    /// <summary>C랭크 심작박동 활성화 여부</summary>
    private bool isHeartbeatActive = false;
    
    #endregion
    
    #region 아이템 효과 데이터
    
    /// <summary>운명의 주사위 효과 데이터</summary>
    private DiceEffectData diceEffectData;
    
    /// <summary>확신의 사슬 효과 데이터</summary>
    private ChainEffectData chainEffectData;
    
    /// <summary>스텔라 프로토콜 효과 데이터</summary>
    private StellaEffectData stellaEffectData;
    
    /// <summary>심작박동 효과 데이터</summary>
    private HeartbeatEffectData heartbeatEffectData;
    
    #endregion
    
    #region 아이템 데이터 참조
    
    /// <summary>운명의 주사위 아이템 데이터</summary>
    private DiceItemDataSO diceItemData;
    
    /// <summary>확신의 사슬 아이템 데이터</summary>
    private ChainItemDataSO chainItemData;
    
    /// <summary>스텔라 프로토콜 아이템 데이터</summary>
    private StellaItemDataSO stellaItemData;
    
    /// <summary>심작박동 아이템 데이터</summary>
    private HeartbeatItemDataSO heartbeatItemData;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (instance != this)
        {
            Debug.LogWarning("[ItemController] 중복된 ItemController 인스턴스를 제거합니다.");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 게임 이벤트 구독
        SubscribeToGameEvents();
    }
    
    private void OnDestroy()
    {
        // 게임 이벤트 구독 해제
        UnsubscribeFromGameEvents();
    }
    
    private void Update()
    {
        // 확신의 사슬: 연속 처치 타이머 업데이트
        if (isChainActive)
        {
            UpdateChainTimer();
        }
    }
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// ItemController 초기화
    /// </summary>
    private void Initialize()
    {
        // 효과 데이터 초기화
        diceEffectData = new DiceEffectData(0f, 0f, 0f);
        chainEffectData = new ChainEffectData(0, 0f);
        stellaEffectData = new StellaEffectData(false);
        heartbeatEffectData = new HeartbeatEffectData(0, false);
        
        Debug.Log("[ItemController] 초기화 완료");
    }
    
    /// <summary>
    /// 게임 이벤트 구독
    /// </summary>
    private void SubscribeToGameEvents()
    {
        // TODO: RoomController의 방 클리어 이벤트 구독
        // RoomController.OnRoomCleared += OnRoomCleared;
        
        // MonsterController의 몬스터 처치 이벤트 구독
        // 각 몬스터의 OnMonsterDied 이벤트를 개별적으로 구독해야 함
        // 또는 전역 이벤트 시스템 사용
        
        // TODO: PlayerController의 피격 이벤트 구독
        // PlayerController.OnPlayerDamaged += OnPlayerDamaged;
        
        Debug.Log("[ItemController] 게임 이벤트 구독 완료 (TODO: 실제 이벤트 연결 필요)");
    }
    
    /// <summary>
    /// 게임 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromGameEvents()
    {
        // TODO: 이벤트 구독 해제
        // RoomController.OnRoomCleared -= OnRoomCleared;
        // PlayerController.OnPlayerDamaged -= OnPlayerDamaged;
    }
    
    /// <summary>
    /// 몬스터 처치 이벤트 핸들러 (외부에서 호출)
    /// MonsterController의 OnMonsterDied 이벤트에서 호출됩니다.
    /// </summary>
    /// <param name="monster">처치된 몬스터</param>
    public void OnMonsterKilled(MonsterController monster)
    {
        // 확신의 사슬: 스택 증가
        OnMonsterKilledForChain(monster);
        
        // 심작박동: 연속 처치 카운트 증가
        OnMonsterKilledForHeartbeat(monster);
    }
    
    #endregion
    
    #region 아이템 등록/제거
    
    /// <summary>
    /// 아이템 등록
    /// </summary>
    /// <param name="itemData">아이템 데이터</param>
    public void RegisterItem(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("[ItemController] itemData가 null입니다!");
            return;
        }
        
        switch (itemData.itemIdentifier)
        {
            case ItemIdentifier.Dice:
                RegisterDiceItem(itemData as DiceItemDataSO);
                break;
                
            case ItemIdentifier.Chain:
                RegisterChainItem(itemData as ChainItemDataSO);
                break;
                
            case ItemIdentifier.Stella:
                RegisterStellaItem(itemData as StellaItemDataSO);
                break;
                
            case ItemIdentifier.Heartbeat:
                RegisterHeartbeatItem(itemData as HeartbeatItemDataSO);
                break;
                
            default:
                Debug.LogWarning($"[ItemController] 알 수 없는 아이템 식별자: {itemData.itemIdentifier}");
                break;
        }
    }
    
    /// <summary>
    /// 아이템 제거
    /// </summary>
    /// <param name="itemIdentifier">아이템 식별자</param>
    public void UnregisterItem(ItemIdentifier itemIdentifier)
    {
        switch (itemIdentifier)
        {
            case ItemIdentifier.Dice:
                UnregisterDiceItem();
                break;
                
            case ItemIdentifier.Chain:
                UnregisterChainItem();
                break;
                
            case ItemIdentifier.Stella:
                UnregisterStellaItem();
                break;
                
            case ItemIdentifier.Heartbeat:
                UnregisterHeartbeatItem();
                break;
                
            default:
                Debug.LogWarning($"[ItemController] 알 수 없는 아이템 식별자: {itemIdentifier}");
                break;
        }
    }
    
    #endregion
    
    #region 운명의 주사위 (Dice)
    
    /// <summary>
    /// 운명의 주사위 아이템 등록
    /// </summary>
    private void RegisterDiceItem(DiceItemDataSO data)
    {
        if (data == null)
        {
            Debug.LogError("[ItemController] DiceItemDataSO가 null입니다!");
            return;
        }
        
        diceItemData = data;
        isDiceActive = true;
        
        // 방 진입 시 효과 생성
        GenerateDiceEffect();
        
        Debug.Log($"[ItemController] 운명의 주사위 등록 완료");
    }
    
    /// <summary>
    /// 운명의 주사위 아이템 제거
    /// </summary>
    private void UnregisterDiceItem()
    {
        isDiceActive = false;
        diceItemData = null;
        diceEffectData = new DiceEffectData(0f, 0f, 0f);
        
        Debug.Log("[ItemController] 운명의 주사위 제거 완료");
    }
    
    /// <summary>
    /// 운명의 주사위 효과 생성 (방 진입 시 호출)
    /// </summary>
    private void GenerateDiceEffect()
    {
        if (diceItemData == null) return;
        
        // 랜덤 효과 생성
        float attackPower = Random.Range(
            diceItemData.AttackPowerRange.x,
            diceItemData.AttackPowerRange.y
        );
        
        float moveSpeed = Random.Range(
            diceItemData.MoveSpeedRange.x,
            diceItemData.MoveSpeedRange.y
        );
        
        float mentalRecovery = Random.Range(
            diceItemData.MentalRecoverySpeedRange.x,
            diceItemData.MentalRecoverySpeedRange.y
        );
        
        diceEffectData = new DiceEffectData(attackPower, moveSpeed, mentalRecovery);
        
        // TODO: UI에 효과 표시
        ShowDiceEffectUI();
        
        Debug.Log($"[ItemController] 운명의 주사위 효과 생성: 공격력 {attackPower:F1}%, 이동속도 {moveSpeed:F1}%, 정신력회복 {mentalRecovery:F1}%");
    }
    
    /// <summary>
    /// 운명의 주사위 효과 UI 표시
    /// </summary>
    private void ShowDiceEffectUI()
    {
        // TODO: UI에 효과 표시
        Debug.Log($"[ItemController] 운명의 주사위 효과 UI 표시 필요");
    }
    
    /// <summary>
    /// 방 진입 시 호출 (운명의 주사위 효과 재생성)
    /// RoomController에서 호출됩니다.
    /// 새로운 방에 진입할 때마다 효과가 재생성됩니다.
    /// 방 클리어 시에는 효과가 유지되며, 다음 방 진입 시에만 새로운 효과가 생성됩니다.
    /// </summary>
    public void OnRoomEntered()
    {
        if (isDiceActive)
        {
            // 기존 효과 초기화 및 새로운 효과 생성
            GenerateDiceEffect();
            Debug.Log("[ItemController] 방 진입: 운명의 주사위 효과 재생성");
        }
    }
    
    /// <summary>
    /// 방 클리어 시 호출
    /// RoomController에서 호출됩니다.
    /// 운명의 주사위 효과는 방 진입 시에만 재생성되며, 방 클리어 시에는 유지됩니다.
    /// </summary>
    public void OnRoomCleared()
    {
        // 운명의 주사위 효과는 방 진입 시에만 재생성
        // 방 클리어 시에는 효과를 유지합니다.
        Debug.Log("[ItemController] 방 클리어: 운명의 주사위 효과 유지");
    }
    
    #endregion
    
    #region 확신의 사슬 (Chain)
    
    /// <summary>
    /// 확신의 사슬 아이템 등록
    /// </summary>
    private void RegisterChainItem(ChainItemDataSO data)
    {
        if (data == null)
        {
            Debug.LogError("[ItemController] ChainItemDataSO가 null입니다!");
            return;
        }
        
        chainItemData = data;
        isChainActive = true;
        chainEffectData = new ChainEffectData(0, 0f);
        
        Debug.Log($"[ItemController] 확신의 사슬 등록 완료");
    }
    
    /// <summary>
    /// 확신의 사슬 아이템 제거
    /// </summary>
    private void UnregisterChainItem()
    {
        isChainActive = false;
        chainItemData = null;
        chainEffectData = new ChainEffectData(0, 0f);
        
        Debug.Log("[ItemController] 확신의 사슬 제거 완료");
    }
    
    /// <summary>
    /// 확신의 사슬 타이머 업데이트
    /// </summary>
    private void UpdateChainTimer()
    {
        if (chainEffectData.killChainTimer > 0f)
        {
            float newTimer = chainEffectData.killChainTimer - Time.deltaTime;
            
            if (newTimer <= 0f)
            {
                // 연속 처치 시간 초과: 스택 초기화
                chainEffectData = new ChainEffectData(0, 0f);
                Debug.Log("[ItemController] 확신의 사슬: 연속 처치 시간 초과, 스택 초기화");
                
                // TODO: UI 업데이트
            }
            else
            {
                // 타이머 업데이트
                chainEffectData = new ChainEffectData(chainEffectData.currentStack, newTimer);
            }
        }
    }
    
    /// <summary>
    /// 몬스터 처치 시 호출 (확신의 사슬 스택 증가)
    /// </summary>
    private void OnMonsterKilledForChain(MonsterController monster)
    {
        if (!isChainActive || chainItemData == null) return;
        
        // 스택 증가 (최대값 제한)
        int newStack = chainEffectData.currentStack;
        if (newStack < chainItemData.MaxStack)
        {
            newStack++;
        }
        
        // 연속 처치 타이머 리셋 및 스택 업데이트
        chainEffectData = new ChainEffectData(newStack, chainItemData.KillChainDuration);
        
        Debug.Log($"[ItemController] 확신의 사슬: 스택 증가 ({chainEffectData.currentStack}/{chainItemData.MaxStack})");
        
        // TODO: UI 업데이트
    }
    
    #endregion
    
    #region 스텔라 프로토콜 (Stella)
    
    /// <summary>
    /// 스텔라 프로토콜 아이템 등록
    /// </summary>
    private void RegisterStellaItem(StellaItemDataSO data)
    {
        if (data == null)
        {
            Debug.LogError("[ItemController] StellaItemDataSO가 null입니다!");
            return;
        }
        
        stellaItemData = data;
        isStellaActive = true;
        stellaEffectData = new StellaEffectData(true);
        
        Debug.Log($"[ItemController] 스텔라 프로토콜 등록 완료");
    }
    
    /// <summary>
    /// 스텔라 프로토콜 아이템 제거
    /// </summary>
    private void UnregisterStellaItem()
    {
        isStellaActive = false;
        stellaItemData = null;
        stellaEffectData = new StellaEffectData(false);
        
        Debug.Log("[ItemController] 스텔라 프로토콜 제거 완료");
    }
    
    /// <summary>
    /// 플레이어 피격 시 호출 (스텔라 프로토콜 파괴)
    /// PlayerController 또는 EntityController에서 호출됩니다.
    /// </summary>
    /// <param name="damageAmount">받은 데미지</param>
    public void OnPlayerDamaged(int damageAmount)
    {
        if (!isStellaActive || !stellaEffectData.isActive) return;
        
        // 아이템 파괴
        stellaEffectData = new StellaEffectData(false);
        isStellaActive = false;
        
        Debug.Log("[ItemController] 스텔라 프로토콜: 피격으로 인한 아이템 파괴");
        
        // TODO: 아이템 제거 이벤트 발생 (EquipmentItem에서 제거 처리)
        // TODO: UI 업데이트
    }
    
    #endregion
    
    #region 심작박동 (Heartbeat)
    
    /// <summary>
    /// 심작박동 아이템 등록
    /// </summary>
    private void RegisterHeartbeatItem(HeartbeatItemDataSO data)
    {
        if (data == null)
        {
            Debug.LogError("[ItemController] HeartbeatItemDataSO가 null입니다!");
            return;
        }
        
        heartbeatItemData = data;
        isHeartbeatActive = true;
        heartbeatEffectData = new HeartbeatEffectData(0, false);
        
        Debug.Log($"[ItemController] 심작박동 등록 완료");
    }
    
    /// <summary>
    /// 심작박동 아이템 제거
    /// </summary>
    private void UnregisterHeartbeatItem()
    {
        isHeartbeatActive = false;
        heartbeatItemData = null;
        heartbeatEffectData = new HeartbeatEffectData(0, false);
        
        Debug.Log("[ItemController] 심작박동 제거 완료");
    }
    
    /// <summary>
    /// 몬스터 처치 시 호출 (심작박동 연속 처치 카운트 증가)
    /// </summary>
    private void OnMonsterKilledForHeartbeat(MonsterController monster)
    {
        if (!isHeartbeatActive || heartbeatItemData == null) return;
        
        // 연속 처치 카운트 증가
        int newKillCount = heartbeatEffectData.killCount + 1;
        bool hasBonus = false;
        
        // 필요한 처치 수 달성 시 다음 공격 보너스 활성화
        if (newKillCount >= heartbeatItemData.RequiredKillCount)
        {
            hasBonus = true;
            newKillCount = 0; // 카운트 리셋
            
            Debug.Log($"[ItemController] 심작박동: 다음 공격 데미지 보너스 활성화 (+{heartbeatItemData.NextAttackDamageBonus}%)");
            
            // TODO: UI 업데이트
        }
        
        // 효과 데이터 업데이트
        heartbeatEffectData = new HeartbeatEffectData(newKillCount, hasBonus);
    }
    
    /// <summary>
    /// 공격 시 호출 (심작박동 다음 공격 보너스 적용)
    /// </summary>
    /// <returns>데미지 증가량 (%)</returns>
    public float GetHeartbeatDamageBonus()
    {
        if (!isHeartbeatActive || !heartbeatEffectData.hasNextAttackBonus) return 0f;
        
        // 보너스 적용 후 플래그 리셋
        heartbeatEffectData = new HeartbeatEffectData(heartbeatEffectData.killCount, false);
        
        float bonus = heartbeatItemData != null ? heartbeatItemData.NextAttackDamageBonus : 0f;
        Debug.Log($"[ItemController] 심작박동: 데미지 보너스 적용 (+{bonus}%)");
        
        return bonus;
    }
    
    #endregion
    
    #region 효과 조회 (외부 접근용)
    
    /// <summary>
    /// 운명의 주사위 효과 조회
    /// </summary>
    public DiceEffectData GetDiceEffect()
    {
        return isDiceActive ? diceEffectData : new DiceEffectData(0f, 0f, 0f);
    }
    
    /// <summary>
    /// 확신의 사슬 효과 조회
    /// </summary>
    public ChainEffectData GetChainEffect()
    {
        return isChainActive ? chainEffectData : new ChainEffectData(0, 0f);
    }
    
    /// <summary>
    /// 확신의 사슬 공격력 증가량 조회 (스택당 %)
    /// </summary>
    public float GetChainAttackPowerPerKill()
    {
        return isChainActive && chainItemData != null ? chainItemData.AttackPowerPerKill : 0f;
    }
    
    /// <summary>
    /// 스텔라 프로토콜 효과 조회
    /// </summary>
    public StellaEffectData GetStellaEffect()
    {
        return isStellaActive ? stellaEffectData : new StellaEffectData(false);
    }
    
    /// <summary>
    /// 심작박동 효과 조회
    /// </summary>
    public HeartbeatEffectData GetHeartbeatEffect()
    {
        return isHeartbeatActive ? heartbeatEffectData : new HeartbeatEffectData(0, false);
    }
    
    /// <summary>
    /// 아이템 활성화 여부 조회
    /// </summary>
    public bool IsItemActive(ItemIdentifier itemIdentifier)
    {
        switch (itemIdentifier)
        {
            case ItemIdentifier.Dice: return isDiceActive;
            case ItemIdentifier.Chain: return isChainActive;
            case ItemIdentifier.Stella: return isStellaActive;
            case ItemIdentifier.Heartbeat: return isHeartbeatActive;
            default: return false;
        }
    }
    
    #endregion
}

