# PlayerStatusUI 사용 가이드

## 📋 개요

`PlayerStatusUI`는 옵저버 패턴을 사용하여 플레이어의 체력과 정신력을 실시간으로 표시하는 UI 시스템입니다.

---

## 🎯 주요 특징

### 1. **옵저버 패턴 구현**
- `PlayerStatusEvents`를 통해 이벤트를 구독
- 플레이어 상태 변경 시 자동으로 UI 업데이트
- UI와 게임 로직이 완전히 분리됨

### 2. **부드러운 애니메이션**
- 슬라이더가 부드럽게 애니메이션됨
- 체력/정신력 변경 시 텍스트 깜빡임 효과

### 3. **상태별 색상 변화**
- 체력: 70% 이상(초록), 30~70%(노랑), 30% 이하(빨강)
- 정신력: 30% 이상(파랑), 30% 이하(보라)

---

## 🛠️ Unity 에디터에서 UI 제작하기

### **1단계: Canvas 생성**

1. Hierarchy에서 우클릭 → `UI` → `Canvas`
2. Canvas 이름을 `PlayerStatusCanvas`로 변경
3. Canvas 설정:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler** 컴포넌트:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`
     - Screen Match Mode: `Match Width Or Height`
     - Match: `0.5`

---

### **2단계: 컨테이너 패널 생성**

1. Canvas 하위에 우클릭 → `UI` → `Panel`
2. 이름을 `StatusPanel`로 변경
3. Rect Transform 설정:
   - **Anchors**: 좌상단 (Left-Top)
   - **Anchor Presets**: 좌상단 클릭
   - **Pos X**: `20`
   - **Pos Y**: `-20`
   - **Width**: `400`
   - **Height**: `150`
4. Image 컴포넌트:
   - **Color**: 반투명 검정 (R:0, G:0, B:0, A:150)

---

### **3단계: 체력 UI 생성**

#### **3-1. 체력 라벨**
1. StatusPanel 하위에 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름을 `HealthLabel`로 변경
3. Rect Transform:
   - Anchors: 좌상단
   - Pos X: `20`, Pos Y: `-20`
   - Width: `100`, Height: `30`
4. TextMeshProUGUI 설정:
   - Text: `체력`
   - Font Size: `20`
   - Color: 흰색
   - Alignment: 왼쪽 중앙

#### **3-2. 체력 슬라이더**
1. StatusPanel 하위에 우클릭 → `UI` → `Slider`
2. 이름을 `HealthSlider`로 변경
3. Rect Transform:
   - Anchors: 좌상단
   - Pos X: `210`, Pos Y: `-35`
   - Width: `250`, Height: `20`
4. Slider 컴포넌트:
   - Min Value: `0`
   - Max Value: `1`
   - Value: `1`
   - **Interactable**: 체크 해제 (읽기 전용)
5. Fill Area → Fill 이미지:
   - Color: 초록색 (R:51, G:204, B:51)

#### **3-3. 체력 텍스트**
1. StatusPanel 하위에 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름을 `HealthText`로 변경
3. Rect Transform:
   - Anchors: 좌상단
   - Pos X: `210`, Pos Y: `-60`
   - Width: `250`, Height: `25`
4. TextMeshProUGUI 설정:
   - Text: `100 / 100`
   - Font Size: `16`
   - Color: 흰색
   - Alignment: 중앙

---

### **4단계: 정신력 UI 생성**

#### **4-1. 정신력 라벨**
1. StatusPanel 하위에 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름을 `MentalPowerLabel`로 변경
3. Rect Transform:
   - Anchors: 좌상단
   - Pos X: `20`, Pos Y: `-90`
   - Width: `100`, Height: `30`
4. TextMeshProUGUI 설정:
   - Text: `정신력`
   - Font Size: `20`
   - Color: 흰색
   - Alignment: 왼쪽 중앙

#### **4-2. 정신력 슬라이더**
1. StatusPanel 하위에 우클릭 → `UI` → `Slider`
2. 이름을 `MentalPowerSlider`로 변경
3. Rect Transform:
   - Anchors: 좌상단
   - Pos X: `210`, Pos Y: `-105`
   - Width: `250`, Height: `20`
4. Slider 컴포넌트:
   - Min Value: `0`
   - Max Value: `1`
   - Value: `1`
   - **Interactable**: 체크 해제
5. Fill Area → Fill 이미지:
   - Color: 파란색 (R:51, G:128, B:255)

#### **4-3. 정신력 텍스트**
1. StatusPanel 하위에 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름을 `MentalPowerText`로 변경
3. Rect Transform:
   - Anchors: 좌상단
   - Pos X: `210`, Pos Y: `-130`
   - Width: `250`, Height: `25`
4. TextMeshProUGUI 설정:
   - Text: `50 / 50`
   - Font Size: `16`
   - Color: 흰색
   - Alignment: 중앙

---

### **5단계: PlayerStatusUI 스크립트 추가**

1. **StatusPanel** 선택
2. Inspector에서 `Add Component` 클릭
3. `PlayerStatusUI` 검색 후 추가
4. 스크립트 필드 할당:

| 필드 | 할당할 오브젝트 |
|------|--------------|
| **Health Slider** | HealthSlider |
| **Health Text** | HealthText |
| **Health Fill Image** | HealthSlider → Fill Area → Fill |
| **Mental Power Slider** | MentalPowerSlider |
| **Mental Power Text** | MentalPowerText |
| **Mental Power Fill Image** | MentalPowerSlider → Fill Area → Fill |

5. 색상 설정 (선택 사항):
   - **Health High Color**: 초록 (R:0.2, G:0.8, B:0.2)
   - **Health Mid Color**: 노랑 (R:1, G:0.8, B:0)
   - **Health Low Color**: 빨강 (R:1, G:0.2, B:0.2)
   - **Mental Power Color**: 파랑 (R:0.2, G:0.5, B:1)
   - **Mental Power Low Color**: 보라 (R:0.6, G:0.3, B:1)

6. 애니메이션 설정:
   - **Slider Animation Speed**: `5`
   - **Use Text Blink**: 체크
   - **Text Blink Duration**: `0.3`

---

### **6단계: 프리팹 저장**

1. Hierarchy에서 `StatusPanel` 선택
2. `4. Prefabs/UI/` 폴더로 드래그하여 프리팹 생성
3. 프리팹 이름: `PlayerStatusUI`

---

## 🎮 사용 방법

### **씬에 배치**

1. 생성한 `PlayerStatusUI` 프리팹을 Hierarchy로 드래그
2. Canvas 하위에 배치됨
3. 자동으로 PlayerController와 연동되어 작동

### **코드에서 접근 (필요 시)**

```csharp
// UI 강제 갱신
PlayerStatusUI statusUI = FindObjectOfType<PlayerStatusUI>();
if (statusUI != null)
{
    statusUI.ForceRefresh();
}
```

---

## 📊 이벤트 시스템 동작 원리

```
플레이어 상태 변경
    ↓
PlayerController → PlayerStatusEvents 이벤트 발생
    ↓
PlayerStatusUI가 이벤트 구독 중
    ↓
UI 자동 업데이트 (슬라이더, 텍스트, 색상)
```

### **이벤트 목록**
- `OnHealthChanged(int current, int max)` - 체력 변경 시
- `OnMentalPowerChanged(int current, int max)` - 정신력 변경 시
- `OnMentalPowerConsumed(int amount)` - 정신력 소모 시
- `OnMentalPowerRestored(int amount)` - 정신력 회복 시

---

## ⚙️ 커스터마이징

### **1. 색상 변경**
Inspector에서 각 색상 필드를 조정하세요.

### **2. 애니메이션 속도 조정**
- `Slider Animation Speed`: 값이 클수록 빠름 (권장: 3~10)

### **3. 텍스트 포맷 변경**
`PlayerStatusUI.cs` 파일에서 다음 부분 수정:

```csharp
// 체력 텍스트 (237번 줄)
healthText.text = $"{currentHealth} / {maxHealth}";

// 정신력 텍스트 (266번 줄)
mentalPowerText.text = $"{currentMentalPower} / {maxMentalPower}";
```

예시:
```csharp
// 퍼센트로 표시
healthText.text = $"HP: {currentHealth}/{maxHealth} ({currentHealth * 100 / maxHealth}%)";
```

---

## 🐛 문제 해결

### **UI가 업데이트되지 않음**
1. PlayerController가 씬에 존재하는지 확인
2. PlayerController.Initialize()가 호출되었는지 확인
3. Console에서 `[PlayerStatusUI]` 관련 에러 확인

### **슬라이더가 보이지 않음**
1. Fill Area → Fill 이미지가 올바르게 설정되었는지 확인
2. Canvas의 Render Mode 확인
3. Camera가 UI Layer를 렌더링하는지 확인

### **색상이 변하지 않음**
1. `Health Fill Image`와 `Mental Power Fill Image`가 할당되었는지 확인
2. 슬라이더의 Fill 이미지를 직접 할당

---

## 📚 추가 기능 확장

### **용기 시스템 UI 추가**

PlayerStatusEvents에 이미 용기 이벤트가 포함되어 있습니다:
```csharp
PlayerStatusEvents.OnCourageChanged += OnCourageChanged;
PlayerStatusEvents.OnCourageStateChanged += OnCourageStateChanged;
```

동일한 방식으로 용기 바를 추가할 수 있습니다.

---

## 📞 문의

문제가 발생하거나 추가 기능이 필요하면 알려주세요!

