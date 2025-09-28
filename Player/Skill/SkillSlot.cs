using UnityEngine;


public class SkillSlot : MonoBehaviour
{
    [Header("현재 스킬")]
    [SerializeField] private BaseSkillData currentSkillData;  // 현재 장착된 스킬
    
    [Header("상태")]
    [SerializeField] private bool isOnCooldown;
    [SerializeField] private float currentCooldown;
    
    // 현재 스킬 데이터 접근용 프로퍼티
    public BaseSkillData CurrentSkillData => currentSkillData;
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
    public void SetSkillData(BaseSkillData newSkillData)
    {
        if (newSkillData == null)
        {
            Debug.LogWarning("스킬 데이터가 null입니다!");
            return;
        }
        
        currentSkillData = newSkillData;
        
        // 스킬 변경 시 쿨타임 초기화
        isOnCooldown = false;
        currentCooldown = 0f;
        
        // 기존 핸들러 정리
        if (handlerGO != null) Destroy(handlerGO);
        handler = null;

        // 핸들러 프리팹이 있으면 생성하고 Setup
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
