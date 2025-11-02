using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("방향 제한 설정")]
    [Tooltip("방향 제한을 사용할지")]
    [SerializeField] private bool useDirectionRestriction = false;
    
    [Tooltip("허용할 방향 (Vector2)")]
    [SerializeField] private Vector2 allowedDirection = Vector2.right;
    
    [Tooltip("방향 허용 오차 각도 (도)")]
    [Range(0f, 180f)]
    [SerializeField] private float directionTolerance = 45f;

    [Header("시각적 요소")]
    [Tooltip("문 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer doorSprite;
    
    [Tooltip("문 애니메이터")]
    [SerializeField] private Animator doorAnimator;
    
    [Tooltip("문 콜라이더")]
    [SerializeField] private Collider2D doorCollider;
    
    [Tooltip("오디오 소스")]
    [SerializeField] private AudioSource audioSource;

    [Header("이펙트")]
    [Tooltip("문 열기 이펙트")]
    [SerializeField] private GameObject openEffect;
    
    [Tooltip("문 닫기 이펙트")]
    [SerializeField] private GameObject closeEffect;

    // 애니메이션 관련
    private Coroutine doorAnimationCoroutine;
    private Vector3 originalPosition;
    private bool isAnimating = false;

    // 방향 감지 관련
    private Dictionary<Collider2D, Vector2> lastPositions = new Dictionary<Collider2D, Vector2>();

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
        
        // AudioSource 컴포넌트 자동 할당
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            // AudioSource가 없으면 추가
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                // 기본 설정
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D 사운드
            }
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
        // 재생할 사운드 선택 (열기 또는 닫기)
        AudioClip soundToPlay = opening ? openSound : closeSound;
        
        // 사운드가 설정되어 있고 오디오 소스가 있으면 재생
        if (soundToPlay != null && audioSource != null)
        {
            // PlayOneShot을 사용하여 사운드 재생 (중복 재생 가능)
            audioSource.PlayOneShot(soundToPlay);
            Debug.Log($"[DoorController] {doorName} {(opening ? "열기" : "닫기")} 사운드 재생");
        }
        else if (soundToPlay == null)
        {
            Debug.LogWarning($"[DoorController] {doorName}의 {(opening ? "열기" : "닫기")} 사운드가 설정되지 않았습니다.");
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

    /// <summary>
    /// 방향이 허용되는지 확인
    /// </summary>
    /// <param name="movementDirection">이동 방향</param>
    /// <returns>허용되는 방향인지</returns>
    private bool IsDirectionAllowed(Vector2 movementDirection)
    {
        if (!useDirectionRestriction) return true;

        // 방향 벡터 정규화
        Vector2 normalizedMovement = movementDirection.normalized;
        Vector2 normalizedAllowed = allowedDirection.normalized;

        // 내적을 사용해서 각도 계산
        float dot = Vector2.Dot(normalizedMovement, normalizedAllowed);
        float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

        return angle <= directionTolerance;
    }

    /// <summary>
    /// 이동 방향 계산
    /// </summary>
    /// <param name="collider">충돌체</param>
    /// <returns>이동 방향</returns>
    private Vector2 CalculateMovementDirection(Collider2D collider)
    {
        Vector2 currentPosition = collider.transform.position;
        
        if (lastPositions.ContainsKey(collider))
        {
            Vector2 lastPosition = lastPositions[collider];
            Vector2 movementDirection = currentPosition - lastPosition;
            lastPositions[collider] = currentPosition;
            return movementDirection;
        }
        else
        {
            lastPositions[collider] = currentPosition;
            return Vector2.zero;
        }
    }

    /// <summary>
    /// 트리거 진입 시 호출
    /// </summary>
    /// <param name="other">충돌한 오브젝트</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen) return;

        // 플레이어 태그 확인 (필요에 따라 수정)
        if (other.CompareTag("Player"))
        {
            Vector2 movementDirection = CalculateMovementDirection(other);
            
            if (!IsDirectionAllowed(movementDirection))
            {
                // 허용되지 않는 방향이면 통과 차단
                Debug.Log($"[DoorController] {doorName} - 허용되지 않는 방향으로 접근 시도: {movementDirection}");
                
                // 콜라이더를 일시적으로 비활성화하여 통과 차단
                if (doorCollider != null)
                {
                    doorCollider.isTrigger = false;
                    
                    // 짧은 시간 후 다시 트리거로 설정
                    StartCoroutine(ReEnableTriggerAfterDelay());
                }
            }
        }
    }

    /// <summary>
    /// 트리거에서 나갈 때 호출
    /// </summary>
    /// <param name="other">충돌한 오브젝트</param>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어가 나가면 위치 기록 초기화
            if (lastPositions.ContainsKey(other))
            {
                lastPositions.Remove(other);
            }
        }
    }

    /// <summary>
    /// 트리거 재활성화 지연 코루틴
    /// </summary>
    private IEnumerator ReEnableTriggerAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (doorCollider != null && isOpen)
        {
            doorCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// 방향 제한 설정
    /// </summary>
    /// <param name="direction">허용할 방향</param>
    /// <param name="tolerance">허용 오차 각도</param>
    public void SetDirectionRestriction(Vector2 direction, float tolerance = 45f)
    {
        useDirectionRestriction = true;
        allowedDirection = direction;
        directionTolerance = tolerance;
        
        Debug.Log($"[DoorController] {doorName} 방향 제한 설정: {direction}, 허용 오차: {tolerance}도");
    }

    /// <summary>
    /// 방향 제한 해제
    /// </summary>
    public void RemoveDirectionRestriction()
    {
        useDirectionRestriction = false;
        Debug.Log($"[DoorController] {doorName} 방향 제한 해제");
    }
}
