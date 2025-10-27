# 프리팹 기반 UI 구조 가이드

## 🎯 왜 프리팹화가 좋은가?

### ✅ 장점
1. **재사용성** - 같은 스타일의 UI를 여러 곳에서 사용
2. **일관성** - 모든 UI가 동일한 디자인과 동작 유지
3. **유지보수** - 한 곳만 수정하면 모든 인스턴스에 자동 반영
4. **협업 효율** - 디자이너/프로그래머 작업 분리
5. **버그 감소** - 검증된 컴포넌트 재사용

### ⚠️ 단점 (매우 적음)
1. 초기 설정 시간 필요
2. 프리팹 참조 관리 필요 (Unity가 자동 처리)

---

## 🏗️ 권장 프리팹 구조

```
Assets/_Project/3. Prefabs/
├── UI/
│   ├── Common/              # 공통 컴포넌트
│   │   ├── Button_Standard.prefab
│   │   ├── Button_Large.prefab
│   │   ├── Button_Small.prefab
│   │   ├── Panel_Popup.prefab
│   │   ├── Panel_Fullscreen.prefab
│   │   └── Popup_Confirm.prefab
│   │
│   ├── Title/               # 타이틀 씬 전용
│   │   ├── TitleMainPanel.prefab
│   │   ├── TitleOptionPanel.prefab
│   │   └── TitleCreditPanel.prefab
│   │
│   └── Game/                # 게임 씬 전용
│       ├── HPBar.prefab
│       ├── MPBar.prefab
│       ├── SkillSlot.prefab
│       └── PauseMenu.prefab
```

---

## 📝 프리팹 생성 단계별 가이드

### 1단계: 공통 버튼 프리팹 만들기

#### 1-1. 기본 버튼 생성
1. Hierarchy에서 우클릭 → UI → Button - TextMeshPro
2. 이름을 "Button_Standard"로 변경

#### 1-2. UIButton 컴포넌트 추가
1. Button_Standard 선택
2. Inspector → Add Component → UIButton
3. Button 컴포넌트는 자동으로 연결됨

#### 1-3. 디자인 설정
```
Button_Standard
├── Image (Background)
│   - Color: (220, 220, 220, 255)
│   - Width: 300, Height: 60
└── Text (TMP)
    - Font Size: 24
    - Alignment: Center
    - Color: (50, 50, 50, 255)
```

#### 1-4. UIButton 설정
Inspector에서:
- Button Text: Button_Standard의 TextMeshPro 드래그
- Text: "버튼"
- Play Sound: ✓ (체크)

#### 1-5. 프리팹으로 저장
1. Button_Standard를 Project 창의 `Assets/_Project/3. Prefabs/UI/Common/`로 드래그
2. Hierarchy에서 삭제

---

### 2단계: Prefab Variant로 버튼 변형 만들기

#### 버튼 크기별 변형
1. Project 창에서 Button_Standard 우클릭
2. Create → Prefab Variant
3. 이름을 "Button_Large"로 변경
4. 더블클릭하여 Prefab Mode 진입
5. Width: 400, Height: 80, Font Size: 32
6. 저장 후 나가기

**이점**: Button_Standard의 스타일을 수정하면 모든 Variant에 자동 반영!

---

### 3단계: 공통 패널 프리팹 만들기

#### 3-1. 패널 생성
1. Hierarchy 우클릭 → UI → Panel
2. 이름을 "Panel_Popup"으로 변경

#### 3-2. 구조 설정
```
Panel_Popup
├── Background (Image - 반투명)
│   - Color: (0, 0, 0, 180)
│   - Anchor: Stretch All
└── ContentPanel
    ├── TitleBar
    │   └── TitleText (TMP)
    ├── ContentArea
    │   └── (여기에 내용 추가)
    └── ButtonArea
        └── CloseButton (Button_Standard 프리팹)
```

#### 3-3. UIPanel 컴포넌트 추가
1. Panel_Popup 선택
2. Add Component → Canvas Group (자동 추가)
3. Add Component → UIPanel
4. 설정:
   - Close On Escape: ✓
   - Fade Animation: ✓
   - Fade Duration: 0.3

#### 3-4. 프리팹으로 저장
`Assets/_Project/3. Prefabs/UI/Common/Panel_Popup.prefab`

---

### 4단계: 팝업 프리팹 만들기

#### 4-1. Panel_Popup의 Variant 생성
1. Panel_Popup 우클릭 → Create → Prefab Variant
2. 이름: "Popup_Confirm"

#### 4-2. 내용 추가
```
Popup_Confirm
└── ContentPanel
    ├── TitleText: "확인"
    ├── MessageText: "메시지"
    ├── ConfirmButton (Button_Standard)
    └── CancelButton (Button_Standard)
```

#### 4-3. UIPopup 컴포넌트 추가
1. Add Component → UIPopup
2. 모든 참조 연결:
   - Title Text
   - Message Text
   - Confirm Button (UIButton)
   - Cancel Button (UIButton)
   - Panel (UIPanel)

---

## 🎨 타이틀 씬에 프리팹 적용하기

### 5단계: 타이틀 화면 구성

#### 5-1. Canvas 설정
1. Title.unity 씬 열기
2. Canvas 생성 (UI Scale Mode: Scale With Screen Size)

#### 5-2. 메인 패널 만들기
1. Panel_Fullscreen Variant 생성
2. 이름: "TitleMainPanel"
3. 내용 구성:

```
TitleMainPanel (UIPanel)
├── TitleLogo (Image 또는 TextMeshPro)
│   └── "선악과\n편식하면 안되나요?"
├── ButtonGroup
│   ├── GameStartButton (Button_Large 인스턴스)
│   ├── OptionButton (Button_Standard 인스턴스)
│   ├── QuitGameButton (Button_Standard 인스턴스)
│   └── CreditButton (Button_Small 인스턴스)
```

**중요**: 버튼들은 프리팹을 드래그해서 인스턴스로 생성!

#### 5-3. 옵션 패널 만들기
1. Panel_Popup 인스턴스 생성
2. 이름: "TitleOptionPanel"
3. 내용:

```
TitleOptionPanel (UIPanel)
├── ContentPanel
│   ├── TitleText: "설정"
│   ├── VolumeLabel & VolumeSlider
│   ├── FullScreenToggle
│   └── CloseButton (Button_Standard)
```

#### 5-4. TitleUIController 연결
1. Canvas에 TitleUIController 컴포넌트 추가
2. Inspector에서 참조 연결:

**Prefab Buttons:**
- 각 버튼의 **UIButton 컴포넌트** 연결 (GameObject가 아님!)

**Prefab Panels:**
- 각 패널의 **UIPanel 컴포넌트** 연결

**Popup:**
- Popup_Confirm 인스턴스를 씬에 배치하고 UIPopup 연결

---

## 🔧 프리팹 사용 패턴

### 패턴 1: 정적 UI (씬에 배치)
```csharp
// Inspector에서 미리 연결
[SerializeField] private UIButton startButton;

private void Start()
{
    startButton.OnClick.AddListener(OnStartClicked);
}
```

### 패턴 2: 동적 생성 (런타임)
```csharp
[SerializeField] private UIButton buttonPrefab;
[SerializeField] private Transform buttonContainer;

private void CreateButtons()
{
    for (int i = 0; i < 5; i++)
    {
        UIButton button = Instantiate(buttonPrefab, buttonContainer);
        button.SetText($"버튼 {i + 1}");
        
        int index = i; // 클로저 문제 방지
        button.OnClick.AddListener(() => OnButtonClicked(index));
    }
}
```

### 패턴 3: 오브젝트 풀링 (최적화)
```csharp
// 많은 UI를 반복 생성/파괴할 때
private Queue<UIButton> buttonPool = new Queue<UIButton>();

private UIButton GetButton()
{
    if (buttonPool.Count > 0)
    {
        UIButton btn = buttonPool.Dequeue();
        btn.gameObject.SetActive(true);
        return btn;
    }
    return Instantiate(buttonPrefab);
}

private void ReturnButton(UIButton btn)
{
    btn.gameObject.SetActive(false);
    buttonPool.Enqueue(btn);
}
```

---

## 📋 실전 체크리스트

### 프리팹 생성 시
- [ ] UIButton/UIPanel 컴포넌트 추가
- [ ] 모든 참조 연결 완료
- [ ] 프리팹 이름 규칙 준수 (Button_*, Panel_*, Popup_*)
- [ ] 올바른 폴더에 저장

### 씬에서 사용 시
- [ ] 프리팹 인스턴스를 드래그해서 배치
- [ ] 컨트롤러에서 **컴포넌트** 참조 (GameObject X)
- [ ] 초기 상태 설정 (Active/Inactive)

### 수정 시
- [ ] Prefab Mode에서 편집
- [ ] 변경사항이 모든 인스턴스에 반영되는지 확인
- [ ] Overrides가 있다면 의도적인 것인지 확인

---

## 🎓 고급 팁

### Tip 1: Prefab Override 활용
- 대부분은 프리팹 스타일 유지
- 특정 버튼만 색상 변경 → Override 사용
- Inspector에서 "Overrides" 드롭다운으로 관리

### Tip 2: Nested Prefabs
```
Panel_ConfirmDialog
├── Button_Confirm (프리팹)
└── Button_Cancel (프리팹)
```
이렇게 프리팹 안에 프리팹을 넣을 수 있음!

### Tip 3: Addressables (대규모 프로젝트)
- 많은 프리팹을 런타임에 동적 로드
- 메모리 관리 최적화
- 추후 필요 시 적용

### Tip 4: 버전 관리
```
UI/Common/
├── v1/
│   └── Button_Standard.prefab (구버전)
└── v2/
    └── Button_Standard.prefab (신버전)
```

---

## 🚫 피해야 할 실수

### ❌ 실수 1: GameObject 참조
```csharp
[SerializeField] private GameObject button; // X
button.GetComponent<UIButton>().OnClick.AddListener(...); // 비효율
```

### ✅ 올바른 방법
```csharp
[SerializeField] private UIButton button; // O
button.OnClick.AddListener(...); // 효율적
```

### ❌ 실수 2: 프리팹 연결 끊기
- Hierarchy에서 프리팹 인스턴스를 우클릭 → Unpack Prefab → 연결 끊김!
- 특별한 이유가 없으면 사용 금지

### ❌ 실수 3: 프리팹 없이 복붙
- 버튼을 Ctrl+C, Ctrl+V로 복제
- 나중에 스타일 변경 시 모든 버튼을 일일이 수정해야 함

---

## 💡 요약

**프리팹화는 좋은 방법입니다!**

1. **공통 UI 요소는 반드시 프리팹화**
   - 버튼, 패널, 팝업 등

2. **Prefab Variant 활용**
   - 기본 프리팹 하나 + 여러 변형

3. **씬별로 프리팹 인스턴스 배치**
   - 타이틀: TitleMainPanel 인스턴스
   - 게임: GameUIPanel 인스턴스

4. **컴포넌트 참조로 연결**
   - UIButton, UIPanel, UIPopup 컴포넌트

이 구조로 하면:
- 디자인 일관성 ✓
- 유지보수 쉬움 ✓
- 확장성 높음 ✓
- 버그 적음 ✓


