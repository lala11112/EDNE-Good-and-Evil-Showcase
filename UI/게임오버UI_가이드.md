# 게임 오버 UI 시스템 가이드

## 📋 목차
1. [개요](#개요)
2. [시스템 구조](#시스템-구조)
3. [설치 및 설정](#설치-및-설정)
4. [사용 방법](#사용-방법)
5. [커스터마이징](#커스터마이징)
6. [예제 코드](#예제-코드)
7. [트러블슈팅](#트러블슈팅)

---

## 개요

게임 오버 UI 시스템은 플레이어 사망 시 게임 오버 화면을 표시하고 관리하는 시스템입니다.

### 주요 기능
- ✅ 옵저버 패턴을 사용한 이벤트 기반 시스템
- ✅ 자동 게임 오버 화면 표시
- ✅ 계속하기 (부활) 기능
- ✅ 타이틀 화면으로 돌아가기
- ✅ 페이드 인 애니메이션
- ✅ 사운드 재생 지원

### 구성 요소
- **PlayerController**: 플레이어 사망/부활 이벤트 제공
- **GameOverUIController**: 게임 오버 UI 표시 및 관리
- **UIManager**: 씬 전환 및 전역 UI 관리 (기존)

---

## 시스템 구조

### 옵저버 패턴 흐름

```
[플레이어 체력 0]
    ↓
[PlayerController.Die()]
    ↓
[PlayerController.OnPlayerDied 이벤트 발생]
    ↓
[GameOverUIController가 이벤트 구독 중]
    ↓
[게임 오버 UI 표시]
```

### 클래스 다이어그램

```
┌─────────────────────┐
│  PlayerController   │ (싱글톤)
│  - OnPlayerDied     │ (static event)
│  - OnPlayerRevived  │ (static event)
│  - Revive()         │
└──────────┬──────────┘
           │ 이벤트 발행
           ↓
┌─────────────────────┐
│ GameOverUIController│
│ - ShowGameOverPanel │
│ - HideGameOverPanel │
│ - OnContinueClicked │
│ - OnMainMenuClicked │
└──────────┬──────────┘
           │ 씬 전환 / 부활 요청
           ↓
┌─────────────────────┐
│     UIManager       │ (싱글톤)
│ - LoadTitleScene()  │
└─────────────────────┘
```

---

## 설치 및 설정

### 1단계: 프리팹 구조 생성

#### 게임 오버 UI 계층 구조
```
Canvas (게임 UI)
└── GameOverPanel
    ├── Background (Image - 어두운 배경)
    ├── Panel (Image - 게임 오버 박스)
    │   ├── TitleText (TextMeshPro - "Game Over")
    │   ├── MessageText (TextMeshPro - "당신은 사망했습니다.")
    │   └── ButtonGroup
    │       ├── ContinueButton (Button - "계속하기")
    │       └── MainMenuButton (Button - "타이틀로")
```

### 2단계: GameOverUIController 설정

1. **빈 GameObject 생성**
   - 씬에 빈 GameObject 생성 (`GameObject` → `Create Empty`)
   - 이름을 `GameOverManager`로 변경
   - `Add Component` → `GameOverUIController` 추가

2. **인스펙터 설정**

#### UI 패널
- **Game Over Panel**: GameOverPanel 오브젝트 할당

#### 텍스트
- **Title Text**: TitleText (TextMeshProUGUI) 할당
- **Message Text**: MessageText (TextMeshProUGUI) 할당

#### 버튼
- **Continue Button**: 계속하기 버튼 할당
- **Main Menu Button**: 타이틀 버튼 할당

#### 애니메이션 설정
- **Fade In Duration**: 페이드 인 시간 (기본: 1초)
- **Show Delay**: 게임 오버 표시 지연 (기본: 1초)

#### 효과음
- **Game Over Sound**: 게임 오버 사운드 할당 (선택)
- **Button Click Sound**: 버튼 클릭 사운드 할당 (선택)

### 3단계: 완료!

이제 설정이 완료되었습니다. 
- PlayerController는 자동으로 사망 이벤트를 발생시킵니다.
- GameOverUIController가 자동으로 이벤트를 구독하여 UI를 표시합니다.
- **별도의 추가 설정이 필요 없습니다!**

---

## 사용 방법

### 자동 작동

**PlayerController**가 이미 사망 이벤트를 발생시키도록 구현되어 있습니다!
별도의 코드 작성이 필요 없으며, 플레이어 체력이 0이 되면 자동으로 게임 오버 UI가 표시됩니다.

```csharp
// PlayerController.cs에 이미 구현됨
protected override void Die()
{
    base.Die();
    
    // 플레이어 사망 이벤트 자동 발생
    OnPlayerDied?.Invoke(); // GameOverUIController가 이를 감지
}
```

### 수동으로 게임 오버 표시 (옵션)

특별한 경우 수동으로 게임 오버 UI를 표시하려면:

```csharp
public class CustomGameManager : MonoBehaviour
{
    [SerializeField] private GameOverUIController gameOverUI;
    
    private void OnSpecialCondition()
    {
        // 직접 게임 오버 UI 표시
        gameOverUI.ShowGameOver();
    }
}
```

### 플레이어 부활 처리

"계속하기" 버튼을 누르면 **PlayerController.Revive()**가 자동으로 호출됩니다.
부활 로직도 이미 구현되어 있어 **별도의 코드 작성이 필요 없습니다!**

```csharp
// PlayerController.cs에 이미 구현됨
public void Revive()
{
    // 체력 완전 회복 (최대로 설정)
    currentHealth = MaxHealth;
    
    // 정신력 완전 회복 (최대로 설정)
    currentMentalPower = maxMentalPower;
    
    // 생존 상태로 변경
    isAlive = true;
    
    // 입력 활성화
    inputActions.Enable();
    
    // 애니메이터 리셋
    animator.SetTrigger("Revive");
    
    // 부활 이벤트 발생
    OnPlayerRevived?.Invoke();
    
    // UI 업데이트 (최대 체력/정신력으로 업데이트)
    PlayerStatusEvents.InvokeHealthChanged(currentHealth, MaxHealth);
    PlayerStatusEvents.InvokeMentalPowerChanged(currentMentalPower, maxMentalPower);
}
```

부활 시 추가 동작이 필요하다면 이벤트를 구독하세요:

```csharp
public class CustomRespawnHandler : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    
    private void OnEnable()
    {
        // 부활 이벤트 구독
        PlayerController.OnPlayerRevived += OnPlayerRevived;
    }
    
    private void OnDisable()
    {
        PlayerController.OnPlayerRevived -= OnPlayerRevived;
    }
    
    private void OnPlayerRevived()
    {
        // 부활 위치로 이동
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.transform.position = respawnPoint.position;
        }
    }
}
```

---

## 커스터마이징

### 게임 오버 메시지 변경

```csharp
gameOverUIController.SetGameOverTitle("당신은 패배했습니다");
gameOverUIController.SetGameOverMessage("더 강해져서 돌아오세요!");
```

### 페이드 인 시간 조정

인스펙터에서 `Fade In Duration` 값을 조정하세요.
- 빠른 페이드: 0.5초
- 느린 페이드: 2초

### 게임 오버 지연 시간 조정

인스펙터에서 `Show Delay` 값을 조정하세요.
- 즉시 표시: 0초
- 사망 애니메이션 후: 1~2초

### 버튼 커스터마이징

#### 버튼 비활성화
특정 버튼을 숨기고 싶다면:

```csharp
public class GameOverUIController : MonoBehaviour
{
    private void Start()
    {
        // 계속하기 버튼 비활성화 (타이틀만 표시)
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }
}
```

#### 추가 버튼 추가
예: "다시 시작" 버튼

1. UI에 버튼 추가
2. GameOverUIController에 필드 추가:
```csharp
[SerializeField] private Button restartButton;
```
3. 버튼 이벤트 연결:
```csharp
private void InitializeButtons()
{
    // ...
    if (restartButton != null)
    {
        restartButton.onClick.AddListener(OnRestartClicked);
    }
}

private void OnRestartClicked()
{
    Time.timeScale = 1f;
    UIManager.Instance.LoadScene(GameScene.Level_01_Gameplay);
}
```

---

## 예제 코드

### 부활 위치 설정 예제

PlayerController는 이미 사망/부활 로직이 구현되어 있으므로, 부활 위치만 설정하면 됩니다.

```csharp
using UnityEngine;

/// <summary>
/// 플레이어 부활 위치 관리
/// 체크포인트 시스템 등에 활용
/// </summary>
public class RespawnManager : MonoBehaviour
{
    [Header("부활 설정")]
    [Tooltip("부활 위치")]
    [SerializeField] private Transform respawnPoint;
    
    [Tooltip("부활 시 무적 시간")]
    [SerializeField] private float invincibilityDuration = 2f;
    
    private void OnEnable()
    {
        // 부활 이벤트 구독
        PlayerController.OnPlayerRevived += OnPlayerRevived;
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerController.OnPlayerRevived -= OnPlayerRevived;
    }
    
    /// <summary>
    /// 플레이어 부활 시 처리
    /// </summary>
    private void OnPlayerRevived()
    {
        if (PlayerController.Instance == null) return;
        
        // 부활 위치로 이동
        if (respawnPoint != null)
        {
            PlayerController.Instance.transform.position = respawnPoint.position;
            Debug.Log($"플레이어 부활 위치: {respawnPoint.position}");
        }
        
        // 부활 무적 시간 적용 (옵션)
        // PlayerController.Instance.SetInvincible(invincibilityDuration);
        
        // 부활 이펙트 재생 (옵션)
        // PlayRespawnEffect();
    }
    
    /// <summary>
    /// 체크포인트 설정
    /// </summary>
    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
        Debug.Log($"체크포인트 설정: {newRespawnPoint.position}");
    }
}
```

### 테스트: 플레이어 강제 사망

테스트용으로 플레이어를 강제로 사망시키려면:

```csharp
public class DebugTools : MonoBehaviour
{
    private void Update()
    {
        // K 키를 누르면 플레이어 강제 사망
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (PlayerController.Instance != null)
            {
                // 체력을 0으로 만들어 사망 처리
                PlayerController.Instance.TakeDamage(PlayerController.Instance.MaxHealth);
                Debug.Log("테스트: 플레이어 강제 사망");
            }
        }
    }
}
```

---

## 트러블슈팅

### Q: 게임 오버 UI가 표시되지 않아요
**A:** 다음을 확인하세요:
1. PlayerController가 씬에 존재하는지 확인
2. GameOverUIController의 GameOverPanel이 할당되었는지 확인
3. Canvas의 Render Mode가 "Screen Space - Overlay"인지 확인
4. GameOverPanel의 CanvasGroup이 있는지 확인 (자동 생성됨)
5. 콘솔 로그를 확인하여 이벤트가 발생하는지 확인

### Q: 버튼을 눌러도 아무 반응이 없어요
**A:** 다음을 확인하세요:
1. 버튼이 GameOverUIController에 올바르게 할당되었는지 확인
2. EventSystem이 씬에 존재하는지 확인 (`GameObject` → `UI` → `Event System`)
3. 버튼의 Interactable이 체크되어 있는지 확인

### Q: 타이틀 씬으로 돌아가지 않아요
**A:** 다음을 확인하세요:
1. Build Settings (`Ctrl+Shift+B`)에 Title 씬이 추가되었는지 확인
2. SceneLoader.cs의 GameScene enum에 Title이 있는지 확인
3. UIManager 프리팹이나 스크립트가 존재하는지 확인

### Q: 게임 오버 후 시간이 멈춰서 아무것도 할 수 없어요
**A:** GameOverUIController는 게임 오버 시 `Time.timeScale = 0`으로 설정합니다.
   - 애니메이션이 계속 재생되어야 한다면 Animator의 Update Mode를 "Unscaled Time"으로 변경하세요.

### Q: 게임 재시작 후에도 게임 오버 UI가 남아있어요
**A:** 씬을 다시 로드하면 자동으로 해결됩니다. 
   - 씬 재로드 없이 게임을 리셋하는 경우, `gameOverUI.HideGameOver()`를 호출하세요.

### Q: 여러 씬에서 게임 오버 UI를 사용하고 싶어요
**A:** 
   1. GameOverUIController를 프리팹으로 만드세요
   2. 각 게임 씬에 프리팹을 배치하세요
   3. PlayerController의 이벤트는 static이므로 씬 전환 시에도 자동으로 연결됩니다

### Q: 부활 시 특정 위치로 이동하고 싶어요
**A:** PlayerController의 OnPlayerRevived 이벤트를 구독하세요:

```csharp
public class RespawnHandler : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    
    private void OnEnable()
    {
        PlayerController.OnPlayerRevived += MoveToRespawnPoint;
    }
    
    private void OnDisable()
    {
        PlayerController.OnPlayerRevived -= MoveToRespawnPoint;
    }
    
    private void MoveToRespawnPoint()
    {
        if (PlayerController.Instance != null && respawnPoint != null)
        {
            PlayerController.Instance.transform.position = respawnPoint.position;
        }
    }
}
```

---

## 관련 문서

- [UIManager 가이드](README.md)
- [옵션 패널 가이드](OptionPanelController.cs)
- [씬 로드 방법 비교](씬로드_방법_비교_가이드.md)

---

## 버전 정보

- **작성일**: 2025-11-02
- **Unity 버전**: Unity 6
- **스크립트 버전**: 1.0.0

---

## 라이센스

이 프로젝트의 일부입니다. 자유롭게 수정하여 사용하세요.

