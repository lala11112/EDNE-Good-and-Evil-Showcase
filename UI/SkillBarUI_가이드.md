# SkillBarUI 사용 가이드 (롤 스타일)

## 📋 개요

`SkillBarUI`는 롤(League of Legends) 스타일의 스킬 쿨타임 UI 시스템입니다.
4개의 스킬(Z, X, C, V)을 하단 중앙에 표시하며, 쿨타임 애니메이션과 사용 가능 상태를 시각적으로 표현합니다.

---

## 🎯 주요 특징

### 1. **롤 스타일 UI**
- 스킬 아이콘 4개 (Z, X, C, V)
- Radial Fill 쿨타임 오버레이 (시계 반대 방향)
- 쿨타임 숫자 표시
- 키 표시 (Z, X, C, V)
- 사용 가능/불가능 상태 시각화

### 2. **옵저버 패턴**
- `SkillUIEvents`를 통한 실시간 업데이트
- 정신력 부족 시 자동으로 사용 불가 표시
- 쿨타임 자동 추적

### 3. **시각 효과**
- 스킬 사용 시 플래시 효과
- 쿨타임 진행 애니메이션
- 사용 불가 시 어두운 오버레이

---

## 🛠️ Unity 에디터에서 UI 제작하기

### **1단계: Canvas 설정**

기존 `PlayerStatusCanvas`를 사용하거나 새로 생성합니다.

1. Canvas 설정 확인:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler**:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`

---

### **2단계: SkillBar 컨테이너 생성**

1. Canvas 하위에 우클릭 → `Create Empty`
2. 이름을 `SkillBar`로 변경
3. Rect Transform 설정:
   - **Anchors**: 하단 중앙 (Bottom-Center)
   - **Anchor Presets**: 하단 중앙 클릭 (Shift+Alt 클릭하여 Position도 함께 설정)
   - **Pos X**: `0`
   - **Pos Y**: `100` (하단에서 100픽셀 위)
   - **Width**: `300`
   - **Height**: `70`
4. **Horizontal Layout Group** 컴포넌트 추가:
   - Spacing: `10`
   - Child Alignment: `Middle Center`
   - Child Controls Size: Width와 Height 모두 체크
   - Child Force Expand: 둘 다 체크 해제

---

### **3단계: 개별 스킬 슬롯 UI 생성**

각 스킬 슬롯(Z, X, C, V)을 만듭니다. 하나를 만든 후 복제하면 편합니다.

#### **3-1. Z 슬롯 생성**

**A. 컨테이너 생성**
1. SkillBar 하위에 우클릭 → `UI` → `Image`
2. 이름을 `ZSlot`으로 변경
3. Rect Transform:
   - Width: `64`, Height: `64`
4. Image 컴포넌트:
   - **Source Image**: UI Sprite (테두리용)
   - **Color**: 흰색 (사용 가능 시)
   - **Image Type**: Sliced (9-Slice 사용 권장)

**B. 스킬 아이콘**
1. ZSlot 하위에 우클릭 → `UI` → `Image`
2. 이름을 `Icon`으로 변경
3. Rect Transform:
   - **Anchors**: Stretch (모든 방향으로 늘림)
   - **Left/Right/Top/Bottom**: 모두 `8` (여백)
4. Image 컴포넌트:
   - **Source Image**: 기본 아이콘 스프라이트
   - **Color**: 회색 (스킬 없음 표시)
   - **Preserve Aspect**: 체크

**C. 쿨타임 오버레이**
1. ZSlot 하위에 우클릭 → `UI` → `Image`
2. 이름을 `CooldownOverlay`로 변경
3. Rect Transform:
   - **Anchors**: Stretch (전체)
   - **Left/Right/Top/Bottom**: 모두 `0`
4. Image 컴포넌트:
   - **Source Image**: 원형 스프라이트 또는 빈 이미지
   - **Color**: 반투명 검정 (R:0, G:0, B:0, A:180)
   - **Image Type**: `Filled`
   - **Fill Method**: `Radial 360`
   - **Fill Origin**: `Top`
   - **Fill Clockwise**: 체크 해제 (반시계 방향)
   - **Fill Amount**: `1.0`
5. 초기 상태: 비활성화 (체크 해제)

**D. 쿨타임 텍스트**
1. ZSlot 하위에 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름을 `CooldownText`로 변경
3. Rect Transform:
   - **Anchors**: Center
   - **Pos X/Y**: `0`
   - **Width**: `60`, Height: `60`
4. TextMeshProUGUI 설정:
   - **Text**: 빈 문자열
   - **Font Size**: `24`
   - **Color**: 흰색
   - **Alignment**: 중앙
   - **Font Style**: Bold
   - **Outline**: 활성화, 검정 (두께: 0.2)
5. 초기 상태: 비활성화

**E. 키 텍스트**
1. ZSlot 하위에 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름을 `KeyText`로 변경
3. Rect Transform:
   - **Anchors**: 좌하단 (Bottom-Left)
   - **Pos X**: `5`, **Pos Y**: `5`
   - **Width**: `20`, Height: `20`
4. TextMeshProUGUI 설정:
   - **Text**: `Z`
   - **Font Size**: `14`
   - **Color**: 흰색
   - **Alignment**: 중앙
   - **Font Style**: Bold
   - **Outline**: 활성화, 검정

**F. 키 배경 (선택 사항)**
1. KeyText 하위에 우클릭 → `UI` → `Image`
2. 이름을 `KeyBackground`로 변경
3. Rect Transform:
   - **Anchors**: Stretch
   - **Left/Right/Top/Bottom**: 모두 `-2`
4. Image 컴포넌트:
   - **Color**: 반투명 검정 (A: 150)
5. Hierarchy에서 KeyText 위로 드래그 (배경이 되도록)

**G. 사용 불가 오버레이**
1. ZSlot 하위에 우클릭 → `UI` → `Image`
2. 이름을 `UnavailableOverlay`로 변경
3. Rect Transform:
   - **Anchors**: Stretch
   - **Left/Right/Top/Bottom**: 모두 `0`
4. Image 컴포넌트:
   - **Color**: 반투명 빨강 (R:255, G:0, B:0, A:100)
5. 초기 상태: 비활성화

---

#### **3-2. Z 슬롯에 스크립트 추가**

1. **ZSlot** 선택
2. Inspector에서 `Add Component` 클릭
3. `SkillSlotUI` 검색 후 추가
4. 스크립트 필드 할당:

| 필드 | 할당할 오브젝트 |
|------|--------------|
| **Slot Type** | `Z` |
| **Skill Icon Image** | Icon |
| **Default Icon Sprite** | 기본 아이콘 이미지 |
| **Cooldown Overlay Image** | CooldownOverlay |
| **Cooldown Text** | CooldownText |
| **Key Text** | KeyText |
| **Key Background Image** | KeyBackground (선택) |
| **Unavailable Overlay Image** | UnavailableOverlay |

5. 색상 설정 (선택):
   - **Cooldown Overlay Color**: (R:0, G:0, B:0, A:0.7)
   - **Available Border Color**: 흰색
   - **Unavailable Border Color**: 회색

6. 애니메이션 설정:
   - **Use Flash Effect**: 체크
   - **Flash Color**: 흰색
   - **Flash Duration**: `0.2`

---

#### **3-3. X, C, V 슬롯 복제**

1. Hierarchy에서 **ZSlot** 선택
2. `Ctrl+D` 3번 눌러 복제
3. 이름을 각각 `XSlot`, `CSlot`, `VSlot`으로 변경
4. 각 슬롯의 **SkillSlotUI** 컴포넌트에서:
   - **XSlot**: Slot Type을 `X`로 변경, KeyText를 `X`로 변경
   - **CSlot**: Slot Type을 `C`로 변경, KeyText를 `C`로 변경
   - **VSlot**: Slot Type을 `V`로 변경, KeyText를 `V`로 변경

---

### **4단계: SkillBarUI 스크립트 추가**

1. **SkillBar** (최상위 컨테이너) 선택
2. Inspector에서 `Add Component` 클릭
3. `SkillBarUI` 검색 후 추가
4. 스크립트 필드 자동 할당 확인:
   - Z Slot UI → ZSlot
   - X Slot UI → XSlot
   - C Slot UI → CSlot
   - V Slot UI → VSlot
   
   (자동 할당되지 않으면 수동으로 할당)

---

### **5단계: 프리팹 저장**

1. Hierarchy에서 `SkillBar` 선택
2. `4. Prefabs/UI/` 폴더로 드래그하여 프리팹 생성
3. 프리팹 이름: `SkillBarUI`

---

## 🎮 사용 방법

### **씬에 배치**

1. 생성한 `SkillBarUI` 프리팹을 Hierarchy로 드래그
2. Canvas 하위에 배치됨
3. 자동으로 PlayerController의 SkillController와 연동되어 작동

### **SkillController 설정**

SkillController가 각 슬롯의 타입을 자동으로 설정합니다!

1. Player 오브젝트에 SkillController가 있는지 확인
2. 플레이 모드 실행 시 각 슬롯이 자동으로 생성되고 타입이 할당됨:
   - **Z Slot** → 자동으로 Type: `Z` 설정
   - **X Slot** → 자동으로 Type: `X` 설정
   - **C Slot** → 자동으로 Type: `C` 설정
   - **V Slot** → 자동으로 Type: `V` 설정

**추가 설정 불필요!** 스크립트가 자동으로 처리합니다.

---

## 📊 이벤트 시스템 동작 원리

```
플레이어 스킬 사용
    ↓
SkillSlot.ExecuteSkill()
    ↓
SkillUIEvents 이벤트 발생
    ↓
SkillSlotUI가 이벤트 구독 중
    ↓
UI 자동 업데이트
- 쿨타임 오버레이 애니메이션
- 쿨타임 숫자 표시
- 사용 불가 오버레이
```

### **이벤트 목록**
- `OnSkillSlotChanged` - 스킬 변경 시 (아이콘 업데이트)
- `OnCooldownStarted` - 쿨타임 시작
- `OnCooldownUpdated` - 쿨타임 진행 (매 프레임)
- `OnCooldownEnded` - 쿨타임 종료
- `OnSkillUsed` - 스킬 사용 (플래시 효과)

---

## ⚙️ 커스터마이징

### **1. 슬롯 크기 변경**
SkillBarUI 컴포넌트에서:
- **Slot Size**: `(64, 64)` → 원하는 크기

### **2. 슬롯 간격 변경**
SkillBar의 Horizontal Layout Group에서:
- **Spacing**: `10` → 원하는 간격

### **3. 색상 변경**
각 SkillSlotUI 컴포넌트에서:
- **Cooldown Overlay Color**: 쿨타임 오버레이 색상
- **Available Border Color**: 사용 가능 시 테두리 색상
- **Unavailable Border Color**: 사용 불가 시 테두리 색상
- **Flash Color**: 스킬 사용 시 플래시 색상

### **4. 쿨타임 텍스트 포맷 변경**
`SkillSlotUI.cs` 파일에서:

```csharp
// 389~398번 줄
if (remainingTime >= 1f)
{
    cooldownText.text = Mathf.Ceil(remainingTime).ToString();
}
else if (remainingTime > 0f)
{
    cooldownText.text = remainingTime.ToString("F1");
}
```

---

## 🎨 아트 에셋 준비

### **필요한 스프라이트**
1. **스킬 아이콘**: 각 스킬마다 64x64 PNG
2. **테두리 이미지**: 9-Slice 가능한 UI 스프라이트
3. **기본 아이콘**: 스킬 없을 때 표시할 아이콘

### **스프라이트 임포트 설정**
1. Texture Type: `Sprite (2D and UI)`
2. Sprite Mode: `Single`
3. Pixels Per Unit: `100`
4. Filter Mode: `Bilinear`
5. Compression: `None` (고품질) 또는 `Normal Quality`

---

## 🐛 문제 해결

### **UI가 업데이트되지 않음**
1. PlayerController가 씬에 존재하는지 확인
2. SkillController가 PlayerController에 있는지 확인
3. 각 SkillSlot의 **Slot Type**이 올바르게 설정되었는지 확인
4. Console에서 `[SkillSlotUI]` 관련 에러 확인

### **쿨타임 오버레이가 보이지 않음**
1. CooldownOverlay의 Image 컴포넌트 확인:
   - **Image Type**: `Filled`
   - **Fill Method**: `Radial 360`
   - **Fill Origin**: `Top`
2. 초기 상태에서는 비활성화되어 있어야 함

### **스킬 아이콘이 표시되지 않음**
1. PlayerSkillData SO에서 **Skill Icon** 필드가 할당되었는지 확인
2. Default Icon Sprite가 SkillSlotUI에 할당되었는지 확인

### **키 텍스트가 잘못 표시됨**
각 SkillSlotUI의 KeyText를 수동으로 확인:
- Z Slot → "Z"
- X Slot → "X"
- C Slot → "C"
- V Slot → "V"

---

## 🚀 고급 기능

### **1. 동적 슬롯 수 변경**
SkillBarUI를 확장하여 3개 또는 5개 슬롯 지원 가능

### **2. 스킬 툴팁 추가**
SkillSlotUI에 OnPointerEnter/Exit 이벤트를 추가하여 툴팁 표시

### **3. 스킬 레벨 표시**
KeyText 옆에 레벨 표시 추가 (예: "3/5")

### **4. 스킬 업그레이드 UI**
Ctrl+Q/W/E/R 입력 시 스킬 업그레이드 UI 표시

---

## 📚 참고 코드

### **코드에서 스킬 UI 접근**

```csharp
// SkillBarUI 찾기
SkillBarUI skillBarUI = FindObjectOfType<SkillBarUI>();

// 특정 슬롯 UI 가져오기
SkillSlotUI zSlotUI = skillBarUI.GetSlotUI(SkillSlotType.Z);

// 모든 슬롯 새로고침
skillBarUI.RefreshAllSlots();

// 스킬 바 표시/숨김
skillBarUI.SetVisible(false);
```

---

## 📞 문의

문제가 발생하거나 추가 기능이 필요하면 알려주세요!

