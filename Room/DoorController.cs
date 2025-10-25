using UnityEngine;
using System.Collections;

/// <summary>
/// 문 컨트롤러
/// 방의 문을 열고 닫는 기능을 관리합니다.
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("문 설정")]
    [Tooltip("문 이름")]
    public string doorName = "Door";
    
    [Tooltip("문 열기/닫기 애니메이션 시간")]
    [Range(0.1f, 2f)]
    public float animationDuration = 0.5f;
    
    [Tooltip("문 열기/닫기 사운드")]
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("문 상태")]
    [Tooltip("문이 열려있는지")]
    [SerializeField] private bool isOpen = true;
    
    [Tooltip("문이 잠겨있는지")]
    [SerializeField] private bool isLocked = false;

    [Header("시각적 요소")]
    [Tooltip("문 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer doorSprite;
    
    [Tooltip("문 애니메이터")]
    [SerializeField] private Animator doorAnimator;
    
    [Tooltip("문 콜라이더")]
    [SerializeField] private Collider2D doorCollider;

    [Header("이펙트")]
    [Tooltip("문 열기 이펙트")]
    [SerializeField] private GameObject openEffect;
    
    [Tooltip("문 닫기 이펙트")]
    [SerializeField] private GameObject closeEffect;

    // 애니메이션 관련
    private Coroutine doorAnimationCoroutine;
    private Vector3 originalPosition;
    private bool isAnimating = false;

    // 이벤트
    public System.Action<DoorController> OnDoorOpened;
    public System.Action<DoorController> OnDoorClosed;

    /// <summary>
    /// 문 초기화
    /// </summary>
    private void Awake()
    {
        InitializeDoor();
    }

    /// <summary>
    /// 문 초기화
    /// </summary>
    private void InitializeDoor()
    {
        // 컴포넌트 자동 할당
        if (doorSprite == null)
        {
            doorSprite = GetComponent<SpriteRenderer>();
        }
        
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
        }
        
        if (doorCollider == null)
        {
            doorCollider = GetComponent<Collider2D>();
        }

        // 원래 위치 저장
        originalPosition = transform.position;

        // 초기 상태 설정
        SetDoorState(isOpen);
        
        // 애니메이터 초기 상태 설정
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("isOpen", isOpen);
        }
        
        Debug.Log($"[DoorController] {doorName} 초기화 완료");
    }

    /// <summary>
    /// 문 열기
    /// </summary>
    public void OpenDoor()
    {
        if (isLocked)
        {
            Debug.LogWarning($"[DoorController] {doorName}이 잠겨있어 열 수 없습니다.");
            return;
        }

        if (isOpen || isAnimating) return;

        isOpen = true;
        StartDoorAnimation(true);
        
        OnDoorOpened?.Invoke(this);
        Debug.Log($"[DoorController] {doorName} 열림");
    }

    /// <summary>
    /// 문 닫기
    /// </summary>
    public void CloseDoor()
    {
        if (isOpen == false || isAnimating) return;

        isOpen = false;
        StartDoorAnimation(false);
        
        OnDoorClosed?.Invoke(this);
        Debug.Log($"[DoorController] {doorName} 닫힘");
    }

    /// <summary>
    /// 문 애니메이션 시작
    /// </summary>
    private void StartDoorAnimation(bool opening)
    {
        if (doorAnimationCoroutine != null)
        {
            StopCoroutine(doorAnimationCoroutine);
        }

        doorAnimationCoroutine = StartCoroutine(AnimateDoor(opening));
    }

    /// <summary>
    /// 문 애니메이션
    /// </summary>
    private IEnumerator AnimateDoor(bool opening)
    {
        isAnimating = true;
        
        // 사운드 재생
        PlayDoorSound(opening);
        
        // 이펙트 재생
        PlayDoorEffect(opening);

        // 애니메이터 상태 변경
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("isOpen", opening);
        }

        // 콜라이더 트리거 설정 (통과 가능성 제어)
        if (doorCollider != null)
        {
            doorCollider.isTrigger = opening;  // 열리면 통과 가능, 닫히면 통과 불가
        }

        // 애니메이션 시간 대기
        yield return new WaitForSeconds(animationDuration);

        isAnimating = false;
        doorAnimationCoroutine = null;
    }

    /// <summary>
    /// 문 상태 즉시 설정
    /// </summary>
    private void SetDoorState(bool open)
    {
        isOpen = open;
        
        // 애니메이터 상태 설정
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("isOpen", open);
        }
        
        if (doorCollider != null)
        {
            doorCollider.isTrigger = open;  // 열리면 통과 가능, 닫히면 통과 불가
        }
    }

    /// <summary>
    /// 문 사운드 재생
    /// </summary>
    private void PlayDoorSound(bool opening)
    {
        AudioClip soundToPlay = opening ? openSound : closeSound;
        if (soundToPlay != null)
        {
            // TODO: 오디오 소스로 사운드 재생
            Debug.Log($"[DoorController] {doorName} {(opening ? "열기" : "닫기")} 사운드 재생");
        }
    }

    /// <summary>
    /// 문 이펙트 재생
    /// </summary>
    private void PlayDoorEffect(bool opening)
    {
        GameObject effectToPlay = opening ? openEffect : closeEffect;
        if (effectToPlay != null)
        {
            // 이펙트 인스턴스 생성
            GameObject effectInstance = Instantiate(effectToPlay, transform.position, transform.rotation);
            
            // 이펙트 자동 파괴
            Destroy(effectInstance, 2f);
        }
    }

    /// <summary>
    /// 문 잠금
    /// </summary>
    public void LockDoor()
    {
        isLocked = true;
        Debug.Log($"[DoorController] {doorName} 잠김");
    }

    /// <summary>
    /// 문 잠금 해제
    /// </summary>
    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log($"[DoorController] {doorName} 잠금 해제");
    }

    /// <summary>
    /// 문이 열려있는지 확인
    /// </summary>
    public bool IsOpen()
    {
        return isOpen;
    }

    /// <summary>
    /// 문이 잠겨있는지 확인
    /// </summary>
    public bool IsLocked()
    {
        return isLocked;
    }

    /// <summary>
    /// 문이 애니메이션 중인지 확인
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }

    /// <summary>
    /// 문 강제 열기 (잠금 무시)
    /// </summary>
    public void ForceOpenDoor()
    {
        bool wasLocked = isLocked;
        isLocked = false;
        OpenDoor();
        if (wasLocked)
        {
            isLocked = true;
        }
    }

    /// <summary>
    /// 문 강제 닫기
    /// </summary>
    public void ForceCloseDoor()
    {
        if (isAnimating)
        {
            StopCoroutine(doorAnimationCoroutine);
            isAnimating = false;
        }
        
        isOpen = false;
        SetDoorState(false);
        
        OnDoorClosed?.Invoke(this);
        Debug.Log($"[DoorController] {doorName} 강제 닫힘");
    }
}
