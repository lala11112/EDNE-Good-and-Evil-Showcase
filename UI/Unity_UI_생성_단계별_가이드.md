# Unity UI 생성 - 완전 단계별 가이드

## 📌 이 가이드는?
Unity 에디터에서 클릭 한 번 한 번, 어떤 GameObject를 만들고 어떤 설정을 하는지 **모든 과정**을 설명합니다.

---

## 🎯 1단계: Canvas 생성 (5분)

### 1-1. Title 씬 열기
1. Project 창 → `Assets/_Project/1. Scenes/Demo/Title.unity` 더블클릭
2. 씬이 열리면 Hierarchy 창 확인

### 1-2. Canvas 만들기
1. **Hierarchy 창**에서 우클릭
2. **UI** → **Canvas** 클릭
3. 자동으로 2개가 생성됨:
   - Canvas
   - EventSystem

### 1-3. Canvas 설정하기
1. **Hierarchy**에서 **Canvas** 선택
2. **Inspector** 창에서 다음 설정:

#### Canvas 컴포넌트
```
Render Mode: Screen Space - Overlay
```

#### Canvas Scaler 컴포넌트
```
UI Scale Mode: Scale With Screen Size
Reference Resolution: X=1920, Y=1080
Screen Match Mode: Match Width Or Height
Match: 0.5 (슬라이더를 중간으로)
```

---

## 🎨 2단계: MainPanel 만들기 (15분)

### 2-1. 빈 GameObject 생성
1. **Canvas** 우클릭 → **Create Empty**
2. 이름을 **"MainPanel"**로 변경

### 2-2. MainPanel 기본 설정
1. **MainPanel** 선택
2. **Inspector**에서 **Add Component** 클릭
3. **Image** 검색 후 추가

#### Rect Transform 설정
```
Anchors: Stretch (Alt 누른 채로 우하단 프리셋 클릭)
Left: 0
Right: 0
Top: 0
Bottom: 0
```

#### Image 컴포넌트 설정
```
Color: 원하는 배경색 (예: 검정 또는 이미지의 주요 색상)
```

---

## 📝 3단계: 타이틀 텍스트 만들기 (10분)

### 3-1. TextMeshPro 생성
1. **MainPanel** 우클릭
2. **UI** → **Text - TextMeshPro** 클릭
3. ⚠️ 처음 사용 시:
   - "Import TMP Essentials" 창이 뜸
   - **"Import TMP Essentials"** 버튼 클릭
   - 완료될 때까지 대기

### 3-2. 타이틀 텍스트 이름 변경
1. 생성된 Text (TMP) 선택
2. 이름을 **"TitleText"**로 변경

### 3-3. TitleText 설정
1. **TitleText** 선택
2. **Inspector**에서 설정:

#### Rect Transform
```
Anchors: Top Center (상단 중앙 프리셋)
Pos X: 0
Pos Y: -200 (위에서 200픽셀 아래)
Width: 800
Height: 200
```

#### TextMeshPro 컴포넌트
```
Text: 
선악과
편식하면 안되나요?

Font Size: 72
Alignment: Center (가운데 정렬)
Color: 흰색 또는 원하는 색
```

---

## 🔘 4단계: 버튼 만들기 - Game Start Button (20분)

### 4-1. Button 생성
1. **MainPanel** 우클릭
2. **UI** → **Button - TextMeshPro** 클릭
3. 자동으로 생성됨:
   ```
   Button (TMP)
   └── Text (TMP)
   ```

### 4-2. 버튼 이름 변경
1. 생성된 Button (TMP) 선택
2. 이름을 **"GameStartButton"**로 변경

### 4-3. GameStartButton 위치 설정
1. **GameStartButton** 선택
2. **Rect Transform** 설정:

```
Anchors: Middle Center (중앙 프리셋 클릭)
Pos X: 0
Pos Y: 50
Width: 300
Height: 60
```

### 4-4. 버튼 스타일 설정
**Button 컴포넌트** (GameStartButton에 있음):
```
Interactable: ✓ (체크됨)
Transition: Color Tint

Colors:
  Normal Color: RGB(255, 255, 255) - 흰색
  Highlighted Color: RGB(220, 220, 220) - 밝은 회색
  Pressed Color: RGB(180, 180, 180) - 회색
  Selected Color: RGB(200, 200, 255) - 연한 파랑
  Disabled Color: RGB(120, 120, 120) - 어두운 회색
  
  Color Multiplier: 1
  Fade Duration: 0.1
```

### 4-5. 버튼 텍스트 설정
1. **Hierarchy**에서 **GameStartButton** 펼치기 (▶ 클릭)
2. 하위의 **Text (TMP)** 선택
3. **TextMeshPro** 컴포넌트 설정:

```
Text: Game Start
Font Size: 24
Alignment: Center (가운데)
Color: RGB(50, 50, 50) - 어두운 회색 또는 검정
```

---

## 🔄 5단계: 나머지 버튼 복제 (10분)

### 5-1. OptionButton 만들기
1. **Hierarchy**에서 **GameStartButton** 선택
2. **Ctrl+D** (복제)
3. 이름을 **"OptionButton"**으로 변경
4. **Rect Transform** 위치만 변경:
   ```
   Pos Y: -20
   ```
5. 하위 **Text (TMP)** 선택하여 텍스트 변경:
   ```
   Text: Option
   ```

### 5-2. QuitGameButton 만들기
1. **OptionButton** 선택 → **Ctrl+D**
2. 이름을 **"QuitGameButton"**으로 변경
3. 위치:
   ```
   Pos Y: -90
   ```
4. 텍스트:
   ```
   Text: Quit Game
   ```

### 5-3. CreditButton 만들기 (작게)
1. **QuitGameButton** 선택 → **Ctrl+D**
2. 이름을 **"CreditButton"**으로 변경
3. **Rect Transform**:
   ```
   Pos Y: -300
   Width: 200 (작게)
   Height: 40 (작게)
   ```
4. 텍스트:
   ```
   Text: 돌고고
   Font Size: 18 (작게)
   ```

---

## ⚙️ 6단계: OptionPanel 만들기 (30분)

### 6-1. 패널 기본 구조
1. **Canvas** 우클릭 → **Create Empty**
2. 이름: **"OptionPanel"**

### 6-2. OptionPanel 설정
1. **OptionPanel** 선택
2. **Add Component** → **Image**
3. **Add Component** → **Canvas Group**

#### Rect Transform
```
Anchors: Stretch All (전체 화면)
Left: 0, Right: 0, Top: 0, Bottom: 0
```

#### Image 컴포넌트
```
Color: RGB(0, 0, 0, 180) - 반투명 검정 (Alpha=180)
```

#### Canvas Group 컴포넌트
```
Alpha: 1
Interactable: ✓
Block Raycasts: ✓
Ignore Parent Groups: ✗
```

### 6-3. ContentPanel 만들기 (실제 내용)
1. **OptionPanel** 우클릭 → **UI** → **Image**
2. 이름: **"ContentPanel"**

#### Rect Transform
```
Anchors: Middle Center
Width: 600
Height: 400
```

#### Image 컴포넌트
```
Color: RGB(40, 40, 40) - 어두운 회색
```

---

### 6-4. 옵션 타이틀 추가
1. **ContentPanel** 우클릭 → **UI** → **Text - TextMeshPro**
2. 이름: **"OptionTitleText"**

#### Rect Transform
```
Anchors: Top Center
Pos X: 0
Pos Y: -40
Width: 500
Height: 60
```

#### TextMeshPro
```
Text: 설정
Font Size: 48
Alignment: Center
Color: 흰색
```

---

### 6-5. 볼륨 슬라이더 추가

#### 6-5-1. 레이블 텍스트
1. **ContentPanel** 우클릭 → **UI** → **Text - TextMeshPro**
2. 이름: **"VolumeLabel"**

```
Rect Transform:
  Anchors: Middle Left
  Pos X: 80
  Pos Y: 50
  Width: 150
  Height: 30

TextMeshPro:
  Text: 볼륨
  Font Size: 24
  Alignment: Left
```

#### 6-5-2. 슬라이더
1. **ContentPanel** 우클릭 → **UI** → **Slider**
2. 이름: **"VolumeSlider"**

```
Rect Transform:
  Anchors: Middle Center
  Pos X: 100
  Pos Y: 50
  Width: 300
  Height: 30

Slider 컴포넌트:
  Min Value: 0
  Max Value: 1
  Whole Numbers: ✗ (체크 해제)
  Value: 1
```

---

### 6-6. 풀스크린 토글 추가

#### 6-6-1. 레이블
1. **ContentPanel** 우클릭 → **UI** → **Text - TextMeshPro**
2. 이름: **"FullScreenLabel"**

```
Rect Transform:
  Pos X: 80
  Pos Y: -30
  Width: 150
  Height: 30

TextMeshPro:
  Text: 전체화면
  Font Size: 24
```

#### 6-6-2. 토글
1. **ContentPanel** 우클릭 → **UI** → **Toggle**
2. 이름: **"FullScreenToggle"**

```
Rect Transform:
  Pos X: 250
  Pos Y: -30
  Width: 60
  Height: 30

Toggle 컴포넌트:
  Is On: ✓ (기본 체크)
```

---

### 6-7. 닫기 버튼
1. **ContentPanel** 우클릭 → **UI** → **Button - TextMeshPro**
2. 이름: **"CloseButton"**

```
Rect Transform:
  Anchors: Bottom Center
  Pos X: 0
  Pos Y: 40
  Width: 200
  Height: 50

Text (TMP):
  Text: 닫기
  Font Size: 24
```

---

### 6-8. OptionPanel 초기 비활성화 ⭐ 중요!
1. **Hierarchy**에서 **OptionPanel** 선택
2. **Inspector** 최상단의 **체크박스 해제** (비활성화)
   ```
   ✗ OptionPanel (체크 해제!)
   ```

---

## 🎖️ 7단계: CreditPanel 만들기 (15분)

### 7-1. 기본 구조
1. **OptionPanel** 선택 → **Ctrl+D** (복제)
2. 이름: **"CreditPanel"**
3. **체크박스 해제** (비활성화)

### 7-2. 내용 수정
1. **CreditPanel > ContentPanel > OptionTitleText** 선택
   - Text: **"크레딧"**으로 변경

2. VolumeSlider, FullScreenToggle 등 **삭제**

3. **ContentPanel** 우클릭 → **UI** → **Text - TextMeshPro**
   - 이름: **"CreditText"**

```
Rect Transform:
  Anchors: Stretch (전체)
  Pos Y: -100
  Width: 500
  Height: 250

TextMeshPro:
  Text:
  프로그래밍: OOO
  기획: OOO
  아트: OOO
  
  Font Size: 20
  Alignment: Center
  Color: 흰색
```

---

## 🔗 8단계: TitleUIController 연결 (20분)

### 8-1. Canvas에 스크립트 추가
1. **Hierarchy**에서 **Canvas** 선택
2. **Inspector** → **Add Component**
3. 검색: **"Title UI Controller"**
4. 클릭하여 추가

### 8-2. 참조 연결하기 ⭐ 매우 중요!

이제 **Inspector**에서 **Title UI Controller** 컴포넌트를 봅니다:

#### Prefab Buttons 섹션
```
Game Start Button: 
  - Hierarchy에서 GameStartButton을 드래그
  
Option Button:
  - Hierarchy에서 OptionButton을 드래그
  
Quit Game Button:
  - Hierarchy에서 QuitGameButton을 드래그
  
Credit Button:
  - Hierarchy에서 CreditButton을 드래그
```

#### Prefab Panels 섹션
```
Main Panel:
  - Hierarchy에서 MainPanel을 드래그
  
Option Panel:
  - Hierarchy에서 OptionPanel을 드래그
  
Credit Panel:
  - Hierarchy에서 CreditPanel을 드래그
```

#### Option Controls 섹션
```
Volume Slider:
  - OptionPanel > ContentPanel > VolumeSlider 드래그
  
Full Screen Toggle:
  - OptionPanel > ContentPanel > FullScreenToggle 드래그
```

---

## 🎮 9단계: UIManager 프리팹 만들기 (10분)

### 9-1. UIManager GameObject 생성
1. **Hierarchy** 빈 곳 우클릭 → **Create Empty**
2. 이름: **"UIManager"**

### 9-2. UIManager 스크립트 추가
1. **UIManager** 선택
2. **Add Component** → 검색: **"UI Manager"**
3. 추가

### 9-3. Scene Names 설정
**Inspector**에서 **UI Manager** 컴포넌트:
```
Title Scene Name: Title
Gameplay Scene Name: Level_01_Gameplay
```

### 9-4. 프리팹으로 저장 (중요!)
1. **Hierarchy**에서 **UIManager** 선택
2. **Project** 창의 `Assets/_Project/3. Prefabs/` 폴더로 드래그
3. 프리팹이 생성됨
4. ⚠️ 이제 다른 씬에서도 이 프리팹을 사용!

---

## 🎯 10단계: 테스트 (5분)

### 10-1. Build Settings 확인
1. **File** → **Build Settings**
2. **Scenes In Build**에 추가:
   - Title (index 0)
   - Level_01_Gameplay (index 1)

### 10-2. 플레이 테스트
1. **Play 버튼** 클릭 (▶)
2. 테스트할 것:
   - [ ] Game Start 버튼 클릭 → 씬 전환
   - [ ] Option 버튼 → 옵션 패널 열림
   - [ ] 볼륨 슬라이더 작동
   - [ ] 닫기 버튼 → 패널 닫힘
   - [ ] ESC 키 → 패널 닫힘
   - [ ] Credit 버튼 → 크레딧 표시
   - [ ] Quit Game → 게임 종료 (에디터에서는 멈춤)

---

## ✅ 완성된 Hierarchy 구조

```
Title 씬
├── Canvas
│   ├── MainPanel
│   │   ├── TitleText (TMP)
│   │   ├── GameStartButton
│   │   │   └── Text (TMP)
│   │   ├── OptionButton
│   │   │   └── Text (TMP)
│   │   ├── QuitGameButton
│   │   │   └── Text (TMP)
│   │   └── CreditButton
│   │       └── Text (TMP)
│   │
│   ├── OptionPanel (비활성화 ✗)
│   │   └── ContentPanel
│   │       ├── OptionTitleText (TMP)
│   │       ├── VolumeLabel (TMP)
│   │       ├── VolumeSlider
│   │       ├── FullScreenLabel (TMP)
│   │       ├── FullScreenToggle
│   │       └── CloseButton
│   │           └── Text (TMP)
│   │
│   └── CreditPanel (비활성화 ✗)
│       └── ContentPanel
│           ├── TitleText (TMP)
│           ├── CreditText (TMP)
│           └── CloseButton
│               └── Text (TMP)
│
├── EventSystem
└── UIManager (프리팹)
```

---

## 🎨 11단계: 게임 씬 UI (Level_01_Gameplay)

### 11-1. 씬 열기
1. **Level_01_Gameplay.unity** 열기

### 11-2. Canvas 생성
- 위의 1단계와 동일하게 Canvas 생성

### 11-3. PlayerStatsPanel 만들기

#### 기본 Panel
1. **Canvas** 우클릭 → **Create Empty**
2. 이름: **"PlayerStatsPanel"**

```
Rect Transform:
  Anchors: Top Left
  Pos X: 50
  Pos Y: -50
  Width: 300
  Height: 100
```

---

#### HP 바 만들기

1. **PlayerStatsPanel** 우클릭 → **UI** → **Slider**
2. 이름: **"HPBar"**

```
Rect Transform:
  Anchors: Top Left
  Pos X: 0
  Pos Y: 0
  Width: 250
  Height: 20

Slider:
  Fill Rect: Fill (하위의 Fill GameObject)
  Min Value: 0
  Max Value: 1
  Value: 1
```

3. **HPBar > Fill Area > Fill** 선택
   - Image Color: **빨간색 RGB(255, 0, 0)**

4. **HPBar** 우클릭 → **UI** → **Text - TextMeshPro**
   - 이름: **"HPText"**

```
Rect Transform:
  Anchors: Stretch
  Left: 0, Right: 0, Top: 0, Bottom: 0

TextMeshPro:
  Text: 100 / 100
  Font Size: 16
  Alignment: Center
  Color: 흰색
```

---

#### MP 바 만들기 (HP바 복제)

1. **HPBar** 선택 → **Ctrl+D**
2. 이름: **"MPBar"**

```
Rect Transform:
  Pos Y: -30
```

3. **MPBar > Fill Area > Fill** 선택
   - Image Color: **파란색 RGB(0, 100, 255)**

4. **MPBar > HPText** 이름 변경
   - 이름: **"MPText"**
   - Text: **"50 / 50"**

---

### 11-4. PauseMenuPanel 만들기

1. **Canvas** 우클릭 → **Create Empty**
2. 이름: **"PauseMenuPanel"**
3. **Add Component** → **Image**
4. **Add Component** → **Canvas Group**

```
Rect Transform:
  Anchors: Stretch
  Left: 0, Right: 0, Top: 0, Bottom: 0

Image:
  Color: RGB(0, 0, 0, 200) - 반투명 검정

Canvas Group:
  Alpha: 1
  Interactable: ✓
  Block Raycasts: ✓
```

#### ContentPanel
1. **PauseMenuPanel** 우클릭 → **UI** → **Image**
2. 이름: **"ContentPanel"**

```
Rect Transform:
  Anchors: Middle Center
  Width: 400
  Height: 300

Image:
  Color: RGB(50, 50, 50)
```

#### 타이틀
1. **ContentPanel** 우클릭 → **UI** → **Text - TextMeshPro**
2. 이름: **"PauseTitleText"**

```
Text: 일시정지
Font Size: 48
Alignment: Center
Pos Y: 80
```

#### 버튼들
1. **ContentPanel** 우클릭 → **UI** → **Button - TextMeshPro**
2. 3개 만들기:
   - **ResumeButton** (Y: 20) - "계속하기"
   - **OptionButton** (Y: -40) - "옵션"
   - **MainMenuButton** (Y: -100) - "메인 메뉴"

각 버튼:
```
Width: 250
Height: 50
```

---

### 11-5. PauseMenuPanel 비활성화
1. **PauseMenuPanel** 선택
2. **Inspector** 최상단 **체크박스 해제** ✗

---

### 11-6. GameUIController 연결

1. **Canvas** 선택
2. **Add Component** → **"Game UI Controller"**

#### 참조 연결
```
Player Stats UI:
  - HP Bar: HPBar 드래그
  - MP Bar: MPBar 드래그
  - HP Text: HPText 드래그
  - MP Text: MPText 드래그

Pause Menu:
  - Pause Menu Panel: PauseMenuPanel 드래그
  - Resume Button: ResumeButton 드래그
  - Option Button: OptionButton 드래그
  - Main Menu Button: MainMenuButton 드래그
```

---

### 11-7. UIManager 추가
1. **Project** 창에서 **UIManager 프리팹** 찾기
2. **Hierarchy**로 드래그
3. 완료!

---

## 🎊 완성!

이제 두 씬이 모두 완성되었습니다!

### Title 씬
- ✅ MainPanel (메인 메뉴)
- ✅ OptionPanel (옵션)
- ✅ CreditPanel (크레딧)
- ✅ TitleUIController
- ✅ UIManager

### Level_01_Gameplay 씬
- ✅ PlayerStatsPanel (HP/MP)
- ✅ PauseMenuPanel (일시정지)
- ✅ GameUIController
- ✅ UIManager

---

## 📸 체크포인트

각 단계마다 확인할 것:
- [ ] GameObject 이름이 정확한가?
- [ ] 컴포넌트가 추가되었는가?
- [ ] Inspector 설정이 맞는가?
- [ ] 참조가 연결되었는가? (None이 아님)
- [ ] 비활성화해야 할 것은 비활성화했는가?

---

## 🆘 문제 해결

### 버튼이 안 눌림
- EventSystem이 있는지 확인
- Button 컴포넌트의 Interactable 체크
- Canvas Group의 Block Raycasts 체크

### 참조가 None으로 표시됨
- 올바른 GameObject를 드래그했는지 확인
- 씬 저장 (**Ctrl+S**)

### TMP Essentials 에러
- Edit → Project Settings → TextMeshPro
- Import TMP Essentials 클릭

---

**이제 구현을 시작하세요!** 🚀

