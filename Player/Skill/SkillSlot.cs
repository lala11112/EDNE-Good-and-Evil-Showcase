using UnityEngine;


public class SkillSlot : MonoBehaviour
{
    [Header("현재 스킬")]
    [SerializeField] private PlayerSkillData currentSkillData;  // 현재 장착된 스킬
    
    [Header("상태")]
    [SerializeField] private bool isOnCooldown;
    [SerializeField] private float currentCooldown;
    
    // 현재 스킬 데이터 접근용 프로퍼티
    public PlayerSkillData CurrentSkillData => currentSkillData;
    public float CurrentCooldown => currentCooldown;
    private bool IsOnCooldown;

    private SkillHandlerInterface handler;        // 캐시
    private GameObject handlerGO;         // 인스턴스
    [SerializeField] private GameObject owner; // 플레이어 오브젝트 참조(없으면 transform.root 등)


    private void Update()
    {
        // 쿨타임 관리
        if (isOnCooldown && currentSkillData != null)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0)
            {
                isOnCooldown = false;
                currentCooldown = 0f;
            }
        }
    }
    
    // 스킬 데이터 설정 (변경 시에만 호출)
    public void SetSkillData(PlayerSkillData newSkillData)
    {
        if (newSkillData == null)
        {
            Debug.LogWarning("스킬 데이터가 null입니다!");
            return;
        }
        
        // 스킬이 바뀌지 않았으면 무시
        if (currentSkillData == newSkillData && handler != null)
        {
            Debug.Log($"{currentSkillData.skillName} 스킬이 이미 장착되어 있습니다.");
            return;
        }
        
        // ⭐ 기존 핸들러 정리 (스킬 변경 시에만)
        if (handler != null)
        {
            handler.Cleanup(); // 상태 초기화
            Destroy(handlerGO); // 오브젝트 파괴
            handler = null;
            handlerGO = null;
        }
        
        currentSkillData = newSkillData;
        
        // 스킬 변경 시 쿨타임 초기화
        isOnCooldown = false;
        currentCooldown = 0f;

        // ⭐ 새 핸들러 생성 및 Setup (스킬 변경 시에만)
        if (currentSkillData != null && currentSkillData.handlerPrefab != null)
        {
            handlerGO = Instantiate(currentSkillData.handlerPrefab, transform);
            handler = handlerGO.GetComponent<SkillHandlerInterface>();
            if (handler == null)
            {
                Debug.LogError("핸들러 프리팹에 SkillHandlerInterface가 없습니다.");
                return;
            }
            var ownerGO = owner != null ? owner : gameObject;
            handler.Setup(currentSkillData, ownerGO);
        }
        
        Debug.Log($"{currentSkillData.skillName} 스킬이 슬롯에 장착되었습니다!");
    }
    
    // 스킬 실행 (저장된 데이터 사용)
    public void ExecuteSkill(SkillRank rank = SkillRank.D)
    {
        if (currentSkillData == null)
        {
            Debug.LogWarning("장착된 스킬이 없습니다!");
            return;
        }
        
        if (isOnCooldown)
        {
            Debug.Log($"{currentSkillData.skillName} 스킬이 쿨타임 중입니다! ({currentCooldown:F1}초 남음)");
            return;
        }
        
        if (handler == null)
        {
            Debug.LogWarning("핸들러가 없습니다.");
            return;
        }

        handler.Use(rank);
        StartCooldown();
    }
    
    private void StartCooldown()
    {
        isOnCooldown = true;
        currentCooldown = currentSkillData.cooldown;
        
        Debug.Log($"{currentSkillData.skillName} 쿨타임 시작: {currentCooldown}초");
    }
}
