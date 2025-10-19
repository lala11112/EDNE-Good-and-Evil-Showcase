# UI 시스템 - 빠른 참조 가이드

## 📁 파일 구조

```
Assets/_Project/2. Scripts/UI/
├── UIManager.cs                    # 전역 UI 매니저 (싱글톤)
├── TitleUIController.cs            # 타이틀 화면 컨트롤러
├── GameUIController.cs             # 인게임 UI 컨트롤러
├── Common/
│   ├── UIButton.cs                 # 공통 버튼 컴포넌트
│   ├── UIPanel.cs                  # 공통 패널 컴포넌트
│   └── UIPopup.cs                  # 팝업/다이얼로그 컴포넌트
└── Docs/
    ├── UI_구현_가이드.md            # 전체 구현 가이드
    └── 프리팹_UI_구조_가이드.md     # 프리팹 사용 가이드
```

---

## 🚀 빠른 시작

### 1. UIManager 설정 (1회만)
1. 빈 GameObject 생성 → "UIManager"
2. UIManager 컴포넌트 추가
3. Scene Names 설정:
   - Title Scene Name: "Title"
   - Gameplay Scene Name: "Level_01_Gameplay"

### 2. 타이틀 씬 구성
1. Canvas 생성
2. 버튼/패널 프리팹 배치
3. TitleUIController 추가 및 참조 연결

### 3. 게임 씬 구성
1. Canvas 생성
2. HP/MP 바, 일시정지 메뉴 구성
3. GameUIController 추가 및 참조 연결

---

## 💡 핵심 사용법

### UIManager (전역)
```csharp
// 씬 전환
UIManager.Instance.LoadGameplayScene();
UIManager.Instance.LoadTitleScene();
UIManager.Instance.QuitGame();

// 설정
UIManager.Instance.SetMasterVolume(0.5f);
```

### UIButton (프리팹)
```csharp
[SerializeField] private UIButton myButton;

myButton.OnClick.AddListener(OnButtonClicked);
myButton.SetText("새 텍스트");
myButton.SetInteractable(false);
```

### UIPanel (프리팹)
```csharp
[SerializeField] private UIPanel optionPanel;

optionPanel.Open();        // 페이드 인
optionPanel.Close();       // 페이드 아웃
optionPanel.Toggle();      // 토글
```

### UIPopup (프리팹)
```csharp
[SerializeField] private UIPopup confirmPopup;

// 확인 버튼만
confirmPopup.ShowAlert("제목", "메시지", () => Debug.Log("확인"));

// 확인/취소
confirmPopup.ShowConfirm(
    "제목", 
    "메시지",
    onConfirm: () => Debug.Log("확인"),
    onCancel: () => Debug.Log("취소")
);
```

---

## ✅ 프리팹화 권장사항

### 반드시 프리팹으로 만들 것
- ✅ 버튼 (크기별로 여러 Variant)
- ✅ 패널 (팝업, 전체화면 등)
- ✅ 팝업/다이얼로그
- ✅ HP/MP 바
- ✅ 스킬 슬롯

### 씬에 직접 구성해도 되는 것
- ⚪ 씬 전용 레이아웃
- ⚪ 고유한 특수 UI

---

## 🎨 프리팹 생성 3단계

### 1단계: 기본 프리팹
```
1. UI 요소 생성 (Button, Panel 등)
2. UIButton/UIPanel 컴포넌트 추가
3. 스타일 설정
4. Prefabs 폴더로 드래그
```

### 2단계: Prefab Variant
```
1. 기본 프리팹 우클릭 → Prefab Variant
2. 크기/색상만 변경
3. 기본 프리팹 수정 시 자동 반영 ✓
```

### 3단계: 씬에 배치
```
1. 프리팹을 Hierarchy로 드래그
2. 컨트롤러에서 컴포넌트 참조 연결
3. 이벤트 리스너 등록
```

---

## 📋 체크리스트

### 타이틀 씬
- [ ] UIManager 생성 및 설정
- [ ] 버튼 프리팹 생성 (Standard, Large, Small)
- [ ] 패널 프리팹 생성 (MainPanel, OptionPanel)
- [ ] TitleUIController 연결
- [ ] 모든 버튼 동작 테스트

### 게임 씬
- [ ] GameUIController 설정
- [ ] HP/MP 바 프리팹 생성 및 배치
- [ ] 일시정지 메뉴 구성
- [ ] ESC 키 동작 테스트
- [ ] 타이틀 복귀 테스트

### Build Settings
- [ ] Title 씬 추가 (Index 0)
- [ ] Gameplay 씬 추가 (Index 1)
- [ ] 씬 이름 일치 확인

---

## ⚠️ 주의사항

1. **컴포넌트 참조 필수**
   ```csharp
   [SerializeField] private UIButton button; // ✓ 올바름
   [SerializeField] private GameObject button; // ✗ 비효율
   ```

2. **Time.timeScale 관리**
   - 일시정지: `Time.timeScale = 0f`
   - 재개: `Time.timeScale = 1f`
   - 씬 전환 전 반드시 복구!

3. **TextMeshPro 필수**
   - 첫 사용 시 "Import TMP Essentials" 클릭

4. **프리팹 연결 유지**
   - "Unpack Prefab" 사용 금지
   - Override는 최소화

---

## 📚 상세 문서

- **🌟 Unity UI 생성 단계별 가이드**: `Unity_UI_생성_단계별_가이드.md` ⭐ **초보자용!**
- **🔗 버튼 씬이동 연결 가이드**: `버튼_씬이동_연결_가이드.md` ⭐ **필독!**
- **⚖️ 씬로드 방법 비교 가이드**: `씬로드_방법_비교_가이드.md` ⭐ **Enum vs 문자열**
- **프리팹 시스템**: `프리팹_UI_구조_가이드.md`

---

## 🎯 핵심 요약

**프리팹화 = 필수!**

1. 공통 UI는 프리팹으로
2. Variant로 변형 관리
3. 씬에는 인스턴스만 배치
4. 컴포넌트로 참조 연결

이렇게 하면:
- 유지보수 ✓
- 일관성 ✓
- 효율성 ✓
- 확장성 ✓

---

## 🆘 트러블슈팅

### 문제: 버튼이 안 눌림
- EventSystem 있는지 확인
- Button 컴포넌트의 Interactable 체크
- CanvasGroup의 Block Raycasts 체크

### 문제: 씬 전환 안 됨
- Build Settings에 씬 추가했는지 확인
- UIManager의 씬 이름이 정확한지 확인

### 문제: 프리팹 수정이 반영 안 됨
- Prefab Mode에서 편집했는지 확인
- Override가 있는지 확인 (Inspector → Overrides)

### 문제: 참조가 Missing
- 컴포넌트가 아닌 GameObject를 연결했는지 확인
- 프리팹을 Unpack하지 않았는지 확인

