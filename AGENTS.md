# SubmersedVR BZ — 모드 개발 문서

> AI 작업 매뉴얼 + 프로젝트 현황 정리

---

## 핵심 규칙

1. 플랜 먼저 — 바로 만들지 말고 계획서부터
2. 수정은 바로 — "이거 바꿔줘" 하면 즉시 반영
3. 코딩 계획서는 바로여기 AGENTS.md에 작성한다.
4. 다음 계획서를 작성하기 전에 이미 완료해서 필요없다고 판단되는 이전 계획서내용은 삭제하고 갱신한다.
5. 코드를 분석, 수정 과정에서 알아낸 기억해야할 중요한 정보, 로직은 AGENTS.md에 기록하여 기억한다. (시도했었지만 실패했던 내역도 정리해서 반드시 기록한다. 같은 실수를 반복하지 않기 위해)
6. 디버깅 워크플로 따르기 — 아래 "디버깅 워크플로" 섹션 참조.

---

## 디버깅 워크플로 (검증된 방식)

AI는 빌드·게임 실행·VR 테스트를 할 수 없음. 사용자와 협업 방식으로 진행:

### 역할 분담

| 주체 | 역할 |
|------|------|
| **AI (Codex)** | 코드 분석·작성, 원인 가설, 테스트 시나리오 설계, 결과 해석 |
| **사용자** | 빌드, 게임 실행, VR에서 관찰, 로그 수집 |

### 순서

1. **계획서 먼저** — AGENTS.md에 원인 분석 + 접근 + 기대결과 + 테스트 시나리오 작성 → 사용자 승인 후 구현
2. **진단 코드 우선** — 곧바로 fix 작성 금지. 먼저 데이터 수집 도구 (핫키 덤프, 강화 로그 등)로 현상 포착. 예: `DialogueHierarchyDumper` F8 핫키로 전체 Canvas 계층 덤프 → `TalkingHead` 오브젝트 식별
3. **한 번에 한 가지** — 여러 버그 동시 수정 금지. 하나씩 완료 → 검증 → 기록 → 다음
4. **사용자에게 구체 지시** — 빌드 전 리스크, 빌드 후 1차(초기화 로그 확인), 2차(런타임 재현 절차), 관찰할 체크포인트(A/B/C 분기)를 명확히. "뭘 보고해야 하는지" 모호하면 실패.
5. **로그는 스크립트로 선별** — 사용자는 로그 파일을 `test_log/`에 복사한다. AI는 전체 로그를 직접 읽기 전에 `tools/vrhud_log_extract.py`로 갱신 여부와 필요한 줄만 확인한다.
6. **실패도 기록** — "해결된 버그 이력" 표에 원인+수정법. 같은 실수 반복 방지

### 진단 도구 패턴 (재사용 가능)

- **핫키 기반 런타임 덤프**: 동적 생성되는 오브젝트는 씬 초기화 시점에 안 잡힘. MonoBehaviour + Input.GetKeyDown + `FindObjectsOfType<Canvas>()` + 재귀 출력이 기본 패턴.
- **고유 로그 접두사**: `[VRHud]`, `[XXX]` 등으로 grep 용이하게.
- **TMP/Image 메타 표시**: 덤프 시 TMP 텍스트 내용 + Image/RawImage 유무도 함께 출력하면 UI 오브젝트 용도 식별 쉬움.

### 로그 분석 워크플로

앞으로 테스트 로그는 사용자가 `SubmersedVR_BZ-main_test/test_log/`에 둔다.

기본 분석 명령:

```powershell
python tools/vrhud_log_extract.py
```

동작:
- `test_log/`에서 최신 `*.log` 파일을 자동 선택
- 이전 실행 상태와 `mtime`, `size`, `sha256`을 비교해 로그가 갱신됐는지 출력
- 기본 명령은 로그 내용까지 읽지 않음. 갱신 확인만 수행
- 상태 파일은 `test_log/.vrhud_log_state.json`

내용 확인은 갱신 확인 후 필요할 때만 `--extract`로 수행한다:

```powershell
python tools/vrhud_log_extract.py --require-new --extract
```

`--require-new --extract` 조합에서는 로그가 갱신되지 않았으면 내용 추출 없이 종료한다. 불필요하게 큰 로그를 다시 읽지 않기 위한 기본 방식이다.

기본 추출 대상은 `[VRHud/TMP]`, `[VRHud/Graphic]`, `[VRHud/CurveNode]`이다.

필요한 로그 요소가 바뀌면 AI가 `tools/vrhud_log_extract.py`의 기본 패턴 또는 옵션 사용법을 갱신한다. 임시로 다른 패턴을 볼 때는:

```powershell
python tools/vrhud_log_extract.py --extract --pattern "\[VRHud" --last 300
```

사용자 보고만 있고 로그가 갱신되지 않았을 가능성이 있으면 `updated: no` 여부를 먼저 확인한다. 새 로그가 반드시 필요할 때는:

```powershell
python tools/vrhud_log_extract.py --require-new
```

로그 갱신 여부는 사용자에게 묻지 않는다. AI가 위 스크립트의 `mtime`, `size`, `sha256`, `updated` 결과로 자동 판단한다. 사용자는 관찰 결과만 보고하고, 로그 파일은 가능한 경우 `test_log/`에 덮어둔다.

### 사용자 관점 요청

- 관찰한 현상을 **구체적으로** (어떤 상황에서, 무엇이, 어떻게)
- 로그 파일은 `test_log/`에 복사
- "안 된다" 보다 **"뭐가, 어떻게 안 된다"**

---

## 파일 구조

```
SubmersedVR_BZ-main/
├── AGENTS.md                       # 프로젝트 문서 (현재 파일)
├── README.md
├── SubmersedVR/
│   ├── SubmersedVR.cs              # 메인 모드 클래스 및 핵심 로직
│   ├── Settings.cs                 # 모든 VR 설정값 정의 (저장/로드 포함)
│   ├── SubmersedVR.csproj          # C# 프로젝트 파일
│   ├── SubmersedVR.sln             # Visual Studio 솔루션
│   │
│   ├── VR/
│   │   ├── VRCameraRig.cs          # VR 카메라 + 컨트롤러 + UI 카메라 관리
│   │   ├── VRHud.cs                # VR HUD 캔버스 설정 (핵심 파일)
│   │   ├── VRMainMenu.cs           # 메인 메뉴 VR 처리
│   │   ├── VRHands.cs              # 손 IK 및 애니메이션
│   │   └── ...
│   │
│   ├── Tweaks/                     # 게임 기능별 개선사항
│   └── Utils/                      # 유틸리티 클래스
```

---

## VR UI 아키텍처 — 검증된 사실

### 카메라 계층 구조

```
SNCameraRoot.main.mainCamera          ← HMD 위치/회전 전체 추적 (헤드 트레킹)
VRCameraRig (transform)               ← rigParentTarget(몸 방향, yaw only) 기준 (LateUpdate)
  └─ uiRig (GameObject)               ← uiRig.transform.rotation = transform.rotation (몸 방향)
       └─ uiCamera (Camera)           ← localPosition=zero, localRotation=identity (= uiRig와 동일한 위치/회전)
            ├─ screenCanvas           ← 메인 UI (PDA, 설정 등) — uiCamera 자식 = 몸 방향 고정
            └─ overlayCanvas          ← 오버레이 UI — uiCamera 자식 = 몸 방향 고정
```

**핵심 발견:**
- `uiCamera.transform`은 `uiRig.transform`과 동일한 위치/회전 → 둘 다 **몸 방향(body-locked)**
- 진짜 **헤드 트레킹(head-locked)**을 원하면 반드시 `SNCameraRoot.main.mainCamera.transform`을 부모로 설정해야 함
- `ManagedUpdate`에서 world position을 직접 설정하는 방식은 UI가 카메라 frustum 밖으로 나가 사라지는 버그 발생

### 커스텀 캔버스 구조 (현재 모드)

```
uiRig (body-locked)
  ├─ StaticHUDCanvas     ← 도보 HUD (기본: uiRig 자식, HeadLocked시: mainCamera 자식)
  ├─ VehicleHUDCanvas    ← 탑승물 HUD (항상 uiRig 자식)
  └─ SubtitleCanvas      ← 자막 캔버스 (uiRig 자식) [자막 오브젝트 발견시 이동]

SNCameraRoot.main.mainCamera (head-locked)
  └─ StaticHUDCanvas     ← HudFollowHead=true 일 때 여기로 이동
```

### Settings.cs 직렬화 방식

`public static` 필드를 reflection으로 자동 저장/로드 (`Settings.Serialize`).  
이벤트(`event FloatChanged`, `event BooleanChanged`)는 직렬화 대상에서 자동 제외됨.

---

## 현재 구현된 기능 (Settings.cs + VRHud.cs)

### 도보 HUD (Submersed VR 탭 → Immersion 섹션)

| 설정 | 타입 | 기본값 | 범위 | 설명 |
|------|------|--------|------|------|
| `HudVerticalOffset` | float | 0.0 | -0.3 ~ +0.3 | HUD 상하 위치 |
| `HudScale` | float | 1.0 | 0.5 ~ 2.0 | HUD 크기 |
| `HudDistance` | float | 0.0 | -0.5 ~ +1.0 | HUD 거리 |
| `HudFollowHead` | bool | false | - | 헤드 트레킹 ON/OFF |
| `HudCurved` | bool | false | - | 커브드 모니터 효과 |
| `HudCurveRadius` | float | 2.0 | 0.5 ~ 5.0 | 곡률 반경(m), 작을수록 더 구부러짐 |

### 자막 (Submersed VR 탭 → Subtitle 섹션)

| 설정 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `SubtitleSyncWithHud` | bool | true | 도보 HUD 위치와 동기화 |
| `SubtitleVerticalOffset` | float | -0.15 | 자막 상하 위치 (sync OFF시) |
| `SubtitleScale` | float | 1.0 | 자막 크기 (sync OFF시) |
| `SubtitleDistance` | float | 0.0 | 자막 거리 (sync OFF시) |

### 탑승물 HUD (Vehicles VR 탭 → Vehicle HUD 섹션)

| 설정 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `VehicleHudVerticalOffset` | float | 0.0 | 탑승물 HUD 상하 위치 |
| `VehicleHudScale` | float | 1.0 | 탑승물 HUD 크기 |
| `VehicleHudDistance` | float | 0.0 | 탑승물 HUD 거리 |
| `VehicleHudCurved` | bool | false | 탑승물 HUD 커브드 효과 |
| `VehicleHudCurveRadius` | float | 2.0 | 탑승물 곡률 반경 |

---

## VRHud.cs 핵심 로직

### 캔버스 위치 계산

```csharp
FootHudPosition()     = (0, 0.1 + HudVerticalOffset, 1.0 + HudDistance)
VehicleHudPosition()  = (0, 0.1 + VehicleHudVerticalOffset, 1.0 + VehicleHudDistance)
SubtitlePosition()    = SubtitleSyncWithHud ? FootHudPosition() : (0, 0.1 + SubtitleVerticalOffset, ...)
```

### 헤드 트레킹 구현 (parent-switching 방식)

```csharp
// GetFootHudParent() - 헤드/몸 방향 선택
Settings.HudFollowHead = true  → SNCameraRoot.main.mainCamera.transform  (HMD 추적)
Settings.HudFollowHead = false → VRCameraRig.instance.uiRig.transform    (몸 방향 고정)

// OnHudFollowHeadChanged() 호출 시
staticHudCanvas.transform.SetParent(newParent, false);
staticHudCanvas.transform.localPosition = FootHudPosition();
```

⚠️ **주의:** `ManagedUpdate`에서 world position 직접 설정 방식은 사용하지 않음. UI가 frustum 밖으로 나가 사라지는 버그 발생.

### 커브드 UI 구현

#### 일반 Graphic (Image, RawImage 등)
`HudCurveEffect : BaseMeshEffect` → `ModifyMesh(VertexHelper)` 에서 원통 투영

#### TextMeshProUGUI (TMP)
`TmpHudCurveEffect : MonoBehaviour` → `TMPro_EventManager.TEXT_CHANGED_EVENT` 구독

**중요 — 이중 적용 방지:**
```csharp
// ForceMeshUpdate()가 TEXT_CHANGED_EVENT를 발생시키므로 guard 필요
private bool applying = false;
public void ForceApply() {
    applying = true;
    tmp.ForceMeshUpdate();  // ← 이 안에서 TEXT_CHANGED_EVENT 발생
    applying = false;
    ApplyToMesh();          // ← 한 번만 적용
}
private void OnTextChanged(Object obj) {
    if (obj == tmp && !applying) ApplyToMesh();  // guard 체크
}
```

**원통 투영 공식:**
```
radiusPixels = worldRadius / (0.00085f * HudScale)  // world단위 → canvas pixel 단위
angle = x / radiusPixels
new_x = radiusPixels * sin(angle)
new_z -= radiusPixels * (1 - cos(angle))
```

---

## uGUI_CanvasScaler 버그 수정

게임 기본 "HUD Size" 슬라이더 조작 시 UI가 사라지는 버그:

```csharp
// uGUI_CanvasScalerFrustum_Fixer.Postfix
public static void Postfix(uGUI_CanvasScaler __instance)
{
    XRSettingsEnabled.isEnabled = true;
    // UpdateFrustum이 localScale을 리셋하면 재적용
    if (VRHud.screenCanvas != null && __instance.transform == VRHud.screenCanvas)
        VRHud.screenCanvas.localScale = new Vector3(0.00072f, 0.00072f, 0.00072f);
}
```

---

## 자막 시스템 현황

**구현된 것:**
- `SubtitleCanvas` 생성 (uiRig 자식, 위치 독립적으로 조절 가능)
- `MoveDialogueElementsToSubtitleCanvas()`: `screenCanvas` 직계 자식 중 키워드 매칭 오브젝트를 `SubtitleCanvas`로 이동
  - 현재 키워드: `subtitle`, `caption`, `dialogue`, `speaker`, `portrait`, `story`, `talking`
- `TalkingHead` 오브젝트 (캐릭터 이름 + 초상화 루트) 이동 확인됨 — 자막 위치 설정에 따라 함께 움직임

**TalkingHead 내부 구조:**
```
TalkingHead
  ├─ Default              ← 일반 캐릭터 대화용
  │    ├─ Background, Waves, Frame [Image]
  │    ├─ Portrait
  │    │    ├─ Mask/Image [Image]   ← 초상화 이미지
  │    │    └─ Frame [Image]
  │    └─ Text [TMP]                ← 캐릭터 이름 (예: "샘")
  └─ AlAn                 ← AI(AlAn) 전용 UI
       ├─ Refraction, Frame, Glyphs, Rays, Face
       └─ FloatingParticles, BulletParticles
```

**아직 확인 안 된 것:**
- 실제 대화 자막 텍스트(말풍선) 오브젝트 위치 — `TalkingHead` 내부에는 캐릭터 이름만 존재
- 대화 시작 시 F8 핫키로 다시 덤프해서 확인 필요

**진단 도구:** [Utils/DialogueHierarchyDumper.cs](SubmersedVR/Utils/DialogueHierarchyDumper.cs) — F8 핫키로 모든 Canvas 계층 재귀 덤프. 디버깅 끝나면 제거 또는 다른 키로 변경.

---

## 조정 가능한 UI 종류 (현재 파악된 것)

| UI | 위치 | 조정 가능 여부 | 방법 |
|----|------|--------------|------|
| 도보 HUD (체력/산소/배고픔/깊이 등) | `StaticHUDCanvas` | ✅ | 설정 슬라이더 |
| 탑승물 HUD (Exosuit, SeaTruck) | `VehicleHUDCanvas` | ✅ | 설정 슬라이더 |
| 손목 HUD (바이탈 바) | `WristCanvas` (왼손) | 제한적 | `WristHud.AdjustHUD()` |
| 자막 텍스트 (대화 내용) | 미확인 | 조사 필요 | F8 덤프로 확인 필요 |
| 캐릭터 이름/초상화 (`TalkingHead`) | `SubtitleCanvas` | ✅ | Subtitle 섹션 슬라이더 (자막과 동일 설정) |
| PDA/설정 메뉴 | `screenCanvas` (헤드 고정) | ❌ (의도적) | 변경 불필요 |
| 인벤토리/제작 | `screenCanvas` (헤드 고정) | ❌ (의도적) | 변경 불필요 |

---

## 현재 미해결 문제 / 다음 테스트

1. **일부 UI 곡률 과적용** — 일부 UI가 곡률을 두 번 받은 것처럼 과도하게 꺾임. `HudCurveTransformEffect` 부모 이동 + 자식 full 곡률 중복 또는 커브 제외 대상 누락 가능성 확인 중.
2. **실제 대화 문장 위치 미확인 가능성** — `TalkingHead`와 확인된 자막/초상화는 `SubtitleCanvas`에서 커브 적용 성공. 만약 특정 대화 문장만 평면이면 그 오브젝트 경로를 덤프로 추가 확인한다.
3. **F8 핫키 충돌** — 게임의 Feedback 신고 기능도 F8 사용. 디버그 종료 후 `DialogueHierarchyDumper` 제거 또는 다른 키로 변경 필요.

## 해결된 버그 이력

| 버그 | 원인 | 수정 방법 |
|------|------|---------|
| 헤드 고정 모드에서 UI 사라짐 (초기) | world position 직접 설정 → uiCamera frustum 밖으로 나감 | parent-switching 방식으로 교체 (`SetParent` 사용) |
| 헤드 고정 모드가 실제로는 몸 방향 고정 | `uiCamera.transform` = `uiRig.transform`과 동일 (body-locked) | `SNCameraRoot.main.mainCamera.transform` 으로 부모 변경 |
| 헤드 고정 모드에서 UI 사라짐 (재발생) | `worldCamera=uiCamera` 고정 → 머리 돌리면 캔버스가 uiCamera frustum 이탈 | `GetFootHudCamera()` 추가, `HudFollowHead` 상태에 따라 `worldCamera`도 함께 전환 |
| TMP 텍스트에 커브드 미적용 | TMP가 `BaseMeshEffect` 우회 | `TmpHudCurveEffect` 클래스 별도 구현 (`TEXT_CHANGED_EVENT` 구독) |
| TMP 커브 이중 적용 | `ForceMeshUpdate()`가 `TEXT_CHANGED_EVENT` 발생시킴 | `applying` 플래그와 `sourceVertices` 캐시로 원본 기준 유지 |
| 플레이어 상태 HUD 플립/스왑/전환 오류 | TMP와 Image가 같은 플립 카드 안에서 서로 다른 곡률 기준 사용 | `BarsPanel/*Bar/Icon` 부모에 `HudCurveTransformEffect` 적용, 자식 TMP/Image는 relative 곡률 |
| 즐겨찾기 조합식 신규 등록 시 아이콘 평면 유지 | 런타임 생성 Graphic에 `HudCurveEffect` 미부착 | `Canvas.willRenderCanvases` 저빈도 재스캔으로 새 Graphic/TMP에 effect 자동 부착 |
| 자막/캐릭터 초상화 평면 유지 | `SubtitleCanvas`가 `ApplyCurve()` 대상에서 빠짐 | `RefreshSubtitleCurve()` 추가, HUD 곡률 설정 공유 및 동적 재스캔 포함 |
| UI 배경/프레임이 평면판처럼 보임 | Image/RawImage 메시 버텍스 밀도 부족 | `HudCurveEffect`가 Graphic triangle stream을 X축 세로 스트립으로 세분화 후 곡률 적용 |
| 캐릭터 이름/초상화 위치 조절 불가 | `TalkingHead` 오브젝트가 `ScreenCanvas` 직계 자식으로 고정. 키워드 검색에 "talking" 미포함으로 이동 안 됨 | `MoveDialogueElementsToSubtitleCanvas()` 키워드에 "talking" 추가 → `SubtitleCanvas`로 이동, 자막 위치 설정 공유 |
| 게임 HUD 크기 슬라이더 조작 시 UI 사라짐 | `uGUI_CanvasScaler.UpdateFrustum`이 scale 리셋 | Postfix에서 `screenCanvas.localScale` 재적용 |
| 기지 밖에서 `전력` TMP 잔상/늘어짐 | `uGUI_PowerIndicator`가 TMP 오브젝트가 아니라 `TextMeshProUGUI.enabled=false`만 하는데, `TmpHudCurveEffect`가 disabled TMP에 `ForceMeshUpdate()`를 호출 | TMP 본체가 disabled면 커브 계산 중단, `ClearMesh(true)` + `canvasRenderer.Clear()`로 렌더 메시 제거 |
| 컷신/탑승물 이후 화면 pitch/roll 고정 | `MainCameraControl.OnUpdate` 대체 패치가 PDA/locked mode의 수평 복구는 유지하지만 컷신 종료 후 일반 조작 복귀 시 수평 복구를 명시적으로 수행하지 않음 | `CameraLevelRestore`가 컷신 종료/스킵/차량 상태 전환 후 조작 가능 상태에서 pitch/roll만 보간 복구. yaw와 VR recenter는 건드리지 않음 |

### 반복 방지용 실패 이력

- `graphicToCanvas` 행렬 방식만 사용하면 TMP 문자별 곡률은 좋지만 플립 회전이 들어간 canvas-x가 반전/수렴해 "멀리서 날아오는" 현상이 발생했다.
- 피벗 균일 오프셋 방식은 플립은 안정적이지만 문자별 곡률을 잃어 텍스트가 평면처럼 보였다.
- PinnedRecipes에 `HudCurveTransformEffect`를 적용하면 레이아웃 재배치와 충돌해 등록/해제 때 transform offset이 누적됐다. PinnedRecipes는 transform 그룹화하지 않는다.
- 6-vertex 단순 사각형만 세분화하는 방식은 실제 UI 배경에 효과가 부족했다. 복잡한 Graphic 메시까지 triangle stream 단위로 X축 세분화해야 한다.

## 자동 리센터 비활성화

**`Settings.AutoRecenterOnVehicleEnter`** (bool, 기본값 `false`)

`false`(기본): 탑승물 입장/컷씬 시작 시 자동 리센터 **없음**  
`true`: 이전 동작 복원 (탑승 시 `VRUtil.Recenter()` 호출)

적용 범위: Exosuit, SeaTruck, Hoverbike, Cyclops 탑승 + `PlayerCinematicController.StartCinematicMode`  
유지: F2 수동 리센터, 게임 초기화 딜레이 리센터, 컷씬 스킵 후 위치 복구 리센터

---

## 현재 수정 계획서 — Graphic 메시 세분화 검증

### 문제

자막/초상화/즐겨찾기/상태 HUD는 커브드 위치와 각도에는 붙었다. 그러나 Image/RawImage 배경과 프레임은 메시 밀도가 낮아 평평한 판처럼 보인다. TMP는 문자별 버텍스가 많아 이미 자연스럽게 휘어진다.

### 2026-04-25 적용

- `HudCurveEffect`에서 커브 적용 대상 Graphic의 triangle stream 전체를 X축 기준 세로 스트립으로 세분화한다.
- 이전 6-vertex 사각형 전용 `SubdivideSimpleQuad()` 방식은 효과가 부족해 폐기했다.
- 새 방식은 각 삼각형을 X축 strip 범위로 클리핑하고 다시 triangulate한다.
- 세분화 밀도는 canvas 폭 기준 16px마다 1개, 최대 48개 segment다.
- full vertex 곡률과 `HudCurveTransformEffect` 하위 relative 곡률 모두 같은 세분화 경로를 사용한다.
- 로그는 `[VRHud/Graphic]`에 `verts=원본->결과`, `segments=N`, before/after x/z 범위를 남긴다. 상태 HUD, PinnedRecipes, TalkingHead, 세분화된 Graphic은 적극적으로 로그 대상이다.
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `SubmersedVR_BZ_0.8.1.zip`이 있어 post-build zip 단계만 실패.

### 사용자 테스트 요청

1. Curved ON, 곡률 반경 2.0 또는 더 작게 설정
2. 자막/캐릭터 초상화 표시 후 배경 네모와 프레임이 실제로 휘어 보이는지 확인
3. 즐겨찾기 조합식의 아이콘/배경이 깨지거나 위치가 어긋나지 않는지 확인
4. 플레이어 상태 HUD 플립/스왑이 기존 정상 상태를 유지하는지 확인
5. 테스트 후 가능하면 `LogOutput.log`를 `test_log/`에 복사. AI는 `tools/vrhud_log_extract.py`로 갱신 여부와 `[VRHud/Graphic]` 로그를 직접 확인한다.

---

## 현재 수정 계획서 — HandReticle 아이콘 안정화

### 문제

손에 나타나는 손모양 아이콘과 스캔 회전 아이콘이 생성 시 시야 방향에 따라 찌그러진다. 스캔 아이콘은 찌그러질 때 회전축이 멀리 잡혀 제자리 회전이 아니라 큰 원으로 도는 경우가 있다.

### 원인 가설

- `HandReticle.main`을 손/레이저 포인터 하위로 옮긴 뒤에도 원본 `HandReticle.LateUpdate`가 카메라/화면 기준 transform을 계속 갱신한다.
- Reticle root 또는 하위 `RectTransform`/`Graphic`에 비균일 scale, z offset, 카메라 기준 회전이 남으면 원형 스캔 아이콘이 타원처럼 찌그러지고 회전 pivot이 어긋날 수 있다.

### 수정 계획

- `SetupHandReticleOnHand()`/`SetupHandReticleLaserPointer()`에서 Reticle root에 안정화 컴포넌트를 붙인다.
- 안정화 컴포넌트는 매 프레임 root localPosition/localRotation/localScale을 모드별 기준값으로 되돌린다.
- 하위 `Graphic`/`RectTransform`은 localScale을 균일화하고 `anchoredPosition3D.z`를 0으로 보정한다.
- `HandReticle.LateUpdate` Postfix에서도 안정화를 호출해 원본 LateUpdate가 마지막에 값을 바꿔도 다시 잡는다.
- `[VRHud/HandReticle]` 로그로 root/자식 scale, rotation, z offset 보정 여부를 제한 출력한다.

### 2026-04-25 적용

- `HandReticleStabilizer` 컴포넌트 추가
- 손 부착 모드 기준값: localPosition `(0,0,0.05)`, localRotation `(90,0,0)`, uniform scale `0.001`
- 레이저 포인터 부착 모드 기준값: 기존 localPosition, localRotation `(40,0,0)`, 현재 scale의 최대축 기준 uniform scale
- `HandReticle.LateUpdate` Postfix에서 stabilizer를 다시 호출해 원본 `LateUpdate` 이후 변형을 보정
- 하위 Graphic localScale 비균일 값과 RectTransform `anchoredPosition3D.z`를 보정
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `SubmersedVR_BZ_0.8.1.zip`이 있어 post-build zip 단계만 실패.

---

## 현재 수정 계획서 — 곡률 과적용 방지

### 문제

일부 UI가 곡률을 두 번 받은 것처럼 과도하게 꺾인다.

### 원인 가설

- `HudCurveTransformEffect`가 부모 오브젝트를 곡면 위치로 옮긴 뒤, 그 하위 Graphic/TMP가 relative가 아니라 full vertex 곡률을 받으면 부모 곡률 + 자식 곡률이 중복된다.
- 동적 재스캔 중 새로 생성되거나 부모가 바뀐 UI가 기존에 붙은 `HudCurveEffect`를 유지한 채 다른 canvas/그룹 기준으로 다시 평가될 수 있다.
- `ScannerIcon`/`HandReticle`처럼 커브 대상이 아닌 cursor/target feedback UI가 HUD canvas 아래에 있어 커브 대상에 잡히면 위치에 따라 과도하게 휘어 보인다.

### 수정 계획

- `ApplyCurve()`에서 `HudCurveTransformEffect` 그룹 하위 Graphic은 무조건 relative 경로만 쓰도록 강제하고, 대상별 mode 로그를 남긴다.
- `ShouldSkipCurve()` 대상은 기존 effect를 0으로 비활성화해 이전 프레임 곡률이 남지 않게 한다.
- `[VRHud/Graphic]` 로그를 status path뿐 아니라 `relative`, `skip`, 큰 `afterZ` 케이스에서도 찍어 과적용 위치를 식별한다.

### 2026-04-26 적용

- `HudCurveDebug.HasCurveTransformAncestor()` 추가
- 이름 기반 `GetCurveTransformRoot()` 판정이 실패해도 ancestor에 `HudCurveTransformEffect`가 실제로 붙어 있으면 Graphic/TMP를 relative 곡률로 강제
- `HudCurveEffect` 로그에 `mode=vertex|relative`를 정확히 남김
- `relative`, 큰 `afterZ`, 세분화된 Graphic은 더 넓게 `[VRHud/Graphic]` 로그를 남기도록 변경
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `SubmersedVR_BZ_0.8.1.zip`이 있어 post-build zip 단계만 실패.

### 2026-04-26 추가 로그 판독 및 2차 적용

- 로그에서 `uGUI_PDAScreen(Clone)/Content/InventoryTab/QuickSlots/...`가 `HudCurveEffect`를 계속 받아 과도하게 꺾이는 사례 확인
- 이 UI는 현재 `targetCanvas` 하위가 아닌데, 이전에 붙은 curve effect가 남아 이전 canvas 기준으로 계속 메시를 변형한 것으로 판단
- `HudCurveEffect.ModifyMesh()`와 `TmpHudCurveEffect`에 `HudCurveDebug.IsDescendantOf(current, targetCanvas)` 방어 추가
- 현재 오브젝트가 targetCanvas 하위가 아니면 곡률 적용을 즉시 skip하고 `[VRHud/Graphic] detached skip`, `[VRHud/TMP] detached skip` 로그 출력
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `SubmersedVR_BZ_0.8.1.zip`이 있어 post-build zip 단계만 실패.

### 2026-04-26 추가 피드백 및 3차 적용

- 사용자 보고: 스캔 이후 나오는 알림 표시에서 테두리는 정상이나, 테두리 안의 스캔 대상 이미지가 과도하게 꺾임.
- 로그 판독 결과: 문제 경로는 `StaticHUDCanvas/HUD/Content/PopupNotification/Unlock/Image`, `.../Journal/Image` 등 `PopupNotification` 아래 카드형 알림 내부 Graphic이다.
- 원인: 팝업 카드 루트가 아니라 내부 `Image`/`Text`가 각각 full vertex 곡률을 받아, 같은 카드 안에서도 테두리/이미지/텍스트가 서로 다른 곡률 기준으로 변형됨.
- 수정: `PopupNotification`의 직계 자식 카드 루트(`Unlock`, `Journal`, `CallAlAn` 등)를 `HudCurveTransformEffect` 그룹으로 처리한다. 하위 Graphic/TMP는 relative 곡률만 받게 되어 카드 내부 요소가 같은 기준으로 붙는다.
- 주의: `PopupNotification` 전체 루트는 화면 전체 크기 컨테이너라 그룹화하지 않는다. 직계 카드 루트만 잡는 일반 규칙이며, 스캔 알림 이미지 전용 하드코딩이 아니다.

### 2026-04-26 추가 피드백 및 4차 적용

- 사용자 보고: 사진은 정상 위치로 돌아왔지만 글자가 조금 튀어나와 보이고, 스캔 UI 등장/퇴장 때 눈앞으로 빠르게 다가오는 동작이 반복되어 번쩍이는 것처럼 보임.
- 로그 확인: 팝업 텍스트는 `mode=relative`였지만 `afterZ`가 180~200까지 발생했고, 팝업 카드 루트가 `z=-624` 수준으로 이동되어 있었다.
- 원인: 카드 루트에 `HudCurveTransformEffect`를 적용하면 원본 팝업 등장/퇴장 transform 애니메이션과 곡률 offset이 충돌한다.
- 수정 계획/적용: `PopupNotification` 카드 루트는 더 이상 `HudCurveTransformEffect` 대상으로 삼지 않는다. 대신 팝업 내부 Graphic/TMP는 `popupRelative` 메시 곡률로 처리하고 depth scale을 0.35로 낮춰 텍스트 돌출과 내부 이미지 과곡률을 줄인다.
- 기대 결과: 팝업 루트 z 이동이 없어져 등장/퇴장 번쩍임이 사라지고, 내부 이미지/테두리/텍스트는 같은 팝업 기준에서 약한 곡률만 받는다.
- 로그 스크립트 기본 패턴에 `PopupNotification`, `Unlock`, `Journal`, `CallAlAn`, `popupRelative`를 추가해 다음 테스트 로그에서 팝업 곡률 상태를 바로 확인한다.

### 2026-04-26 추가 피드백 및 5차 적용

- 사용자 보고: 팝업 UI의 기울기만 커브드가 적용되고 좌표는 평면 위치에 붙어 있음.
- 로그 확인: `popupRelative` 적용 후 `Unlock` 내부 Graphic/TMP의 `afterZ`가 30~75 정도로만 남고, 카드 자체가 곡면 깊이로 이동하지 않았다.
- 원인: 팝업 루트 transform 이동을 제거하면서 메시 계산에서도 각 요소 pivot offset을 제거해 버려, 내부 굽힘만 남고 카드 공통 곡면 위치 offset이 사라졌다.
- 수정 계획: 팝업 루트 transform은 계속 건드리지 않는다. 대신 메시 버텍스 계산에서 `PopupNotification` 직계 카드 루트의 canvas-x를 기준으로 공통 곡면 offset을 더한다. 내부 요소별 곡률은 depth scale 0.35로 유지한다.
- 기대 결과: 팝업 등장/퇴장 애니메이션은 원본 transform 그대로 유지되고, 실제 렌더링 좌표는 카드 단위로 곡면 위치에 붙는다.

### 2026-04-26 추가 피드백 및 6차 적용

- 사용자 보고: 좌표는 돌아온 것 같지만 UI가 양옆으로 눌린 것처럼 짧아졌고, 스캔 이미지는 다시 과도하게 꺾임. 오른쪽 스캔 가능 알림 마크는 평면 화면에 붙음.
- 로그 확인: `Unlock/Image`가 `popupRelative`에서 `afterX=(799,909)`, `afterZ=(-979,-910)`처럼 카드 공통 위치 offset에 더해 X축 상대 곡률까지 받아 폭이 압축됨.
- 원인:
  - 팝업 메시에서 X축 상대 곡률까지 적용해 카드 내부 폭이 줄었다.
  - `ScannerIcon`을 통째로 `ShouldSkipCurve()`에 넣어, 스캔 가능 알림 마크까지 곡률 대상에서 빠졌다.
- 수정 계획:
  - 팝업은 X축은 카드 공통 offset만 적용하고 내부 X 상대 곡률은 제거해 폭을 보존한다.
  - 팝업 내부 Z 상대 굽힘은 depth scale을 0.35에서 0.15로 낮춘다.
  - `ScannerIcon`은 skip하지 않고 `HudCurveTransformEffect` 루트 그룹으로 처리해 위치는 곡면에 붙이고 내부 아이콘은 relative 곡률만 받게 한다.

### 2026-04-26 추가 피드백 및 7차 적용

- 사용자 보고: 오른쪽 스캔 마크와 손 스캔 마크는 정상. 왼쪽 위 스캔 완료 알림은 평평해짐.
- 로그 확인: `Unlock` 팝업은 `popupRelative`에서 X 폭은 보존됐지만 `afterZ` 변화량이 25~35px 수준으로 작아져 곡률이 거의 보이지 않는 상태.
- 원인: 6차에서 X축 압축 방지를 위해 X 상대 곡률을 제거한 것은 맞지만, Z depth scale 0.15가 너무 낮았다.
- 수정 계획: X 폭 보존은 유지하고, 팝업 내부 Z depth scale을 0.35로 되돌린다. 0.35에서 문제가 됐던 과꺾임은 X 압축과 결합된 현상이었으므로 이번 구조에서는 영향이 줄어야 한다.
- 로그 스크립트 기본 패턴에 `ErrorMessageCanvas`, `MessageInstance`도 추가해 실제 왼쪽 위 텍스트 알림과 `Unlock` 팝업을 구분한다.

### 2026-04-26 추가 피드백 및 8차 적용

- 사용자 강한 피드백: 강도 문제가 아니라 그냥 평평함. 의미 없는 상수 조정 반복 중단.
- 최신 로그 확인:
  - 실제 대상은 `StaticHUDCanvas/HUD/Content/PopupNotification/Unlock/...`
  - `ErrorMessageCanvas/MessageInstance`는 최신 로그에 없음.
  - `Unlock` 자식들은 `mode=popupRelative`로 `afterZ`가 존재하지만, 루트 transform 회전은 계속 평면(`lr=(0,0,0)`)이다.
- 원인 재정의: 자식 메시 z만 바꾸고 팝업 카드 루트의 접선 회전을 바꾸지 않아, 로그상 z 변형은 있어도 VR 시야에서는 평평한 카드처럼 보인다.
- 수정 계획:
  - `PopupNotification` 직계 카드 루트를 다시 transform 그룹으로 처리한다.
  - 기존 실패처럼 현재 애니메이션 x로 z를 계산하지 않고, 부모 `PopupNotification`의 `rect.xMin`을 stable anchor로 사용한다.
  - 팝업 루트에는 곡면 위치 offset + Y축 접선 회전을 적용한다.
  - 자식 Graphic/TMP는 전역 offset 없이 낮은 `popupRelative` 메시 곡률만 받는다.
- 기대 결과: 팝업 카드 자체가 곡면의 왼쪽 접선 각도로 돌아가 평평해 보이지 않고, 등장/퇴장 중 z가 빠르게 출렁이는 문제는 stable anchor로 억제된다.

### 2026-04-25 테스트 실패 및 2차 적용

- 테스트 결과: 커브드 옵션 OFF에서도 손/스캔 아이콘 찌그러짐 유지. 즉 커브드 HUD 메시 문제가 아니다.
- 추가 관찰: 아이템에 커서/레이저를 대면 원래 손모양과 아이템 이름이 떠야 하는데 아이템 이름도 안 뜨는 것 같음.
- 원인 재분석: `uGUI_ScannerIcon.LateUpdate()`가 스캔 아이콘에 `localScale=(1+oscX, 1+oscY, 1)`을 직접 넣는다. X/Y가 달라지는 의도된 화면 흔들림이 VR에서는 타원 찌그러짐과 회전축 이탈로 보일 수 있다.
- 1차 `HandReticleStabilizer`는 실제 지점이 아니고 텍스트 표시에도 부작용 가능성이 있어 제거.
- 2차 패치:
  - `uGUI_ScannerIcon.LateUpdate` Postfix에서 `icon.rectTransform.localScale`을 X/Y 최대값 기준 uniform scale로 보정
  - `HandReticle.LateUpdate` Postfix에서 `iconCanvas.localScale`만 X/Y uniform, Z=1로 보정
  - `[VRHud/ScannerIcon]`, `[VRHud/HandReticle]` 로그로 scale 보정과 reticle text 값을 제한 출력
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `SubmersedVR_BZ_0.8.1.zip`이 있어 post-build zip 단계만 실패.

### 2026-04-26 로그 판독 및 3차 적용

- 사용자 보고: 커브드 OFF에서도 증상 유지. 몸 기준 왼쪽 생성은 왜곡이 덜하고 오른쪽 생성은 왜곡이 심한 느낌. 대상 이름/상호작용 텍스트도 안 뜸.
- 로그 스크립트 기본 패턴을 이번 이슈 전용으로 변경: `[VRHud/HandReticle]`, `[VRHud/ScannerIcon]`, `[VRHud/GUIHand]`, `GUIHand`, `Targeting`, `HandReticle`, `ScannerIcon`.
- 로그 확인 결과:
  - `ScannerIcon`의 Graphic이 `StaticHUDCanvas/HUD/Content/ScannerIcon` 아래에서 `HudCurveEffect`를 받고 있었음
  - `pivot=(860,-440,0)`, `afterZ≈-280`으로 오른쪽 위치에서 큰 곡률 오프셋 확인
  - 즉 ScannerIcon은 커서/타겟 피드백인데 HUD 곡면 정보판처럼 구부러지고 있었음
  - `[VRHud/HandReticle]` 로그에서는 `handText=''`, `useText=''`, `targetDistance=0`만 확인되어 실제 타겟 획득 여부 추가 로그 필요
- 3차 패치:
  - `HudCurveDebug.ShouldSkipCurve()` 추가
  - `ScannerIcon`/`HandReticle` 하위 Graphic/TMP는 커브 적용 대상에서 제외
  - 이미 붙은 `HudCurveEffect`/`TmpHudCurveEffect`는 `radiusPixels=0`으로 비활성화
  - `GUIHand.UpdateActiveTarget` Postfix 로그 추가: `[VRHud/GUIHand] target`, `tech`, `distance`, reticle text 출력
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `SubmersedVR_BZ_0.8.1.zip`이 있어 post-build zip 단계만 실패.

---

## 현재 수정 계획서 — PopupNotification 알림 복구

### 문제

최근 패치 후 스캔/데이터뱅크 알림창 위치가 맞지 않고 TMP 글자가 사라졌다. 최신 로그 기준 실제 대상은 `StaticHUDCanvas/HUD/Content/PopupNotification/Unlock/...`이며, `ErrorMessageCanvas/MessageInstance`가 아니다.

### 원인

- `PopupNotification` 직계 카드 루트를 `HudCurveTransformEffect` 그룹으로 다시 넣고 Y 접선 회전을 적용한 것이 실패 원인이다.
- 카드의 안정 앵커가 화면 왼쪽 큰 음수 X에 걸려 약 90도에 가까운 접선 각도가 계산되며, 알림 전체가 거의 edge-on 방향으로 돌아 글자가 사라진다.
- 상태 HUD에는 transform 그룹 방식이 맞지만, 팝업은 자체 슬라이드 애니메이션이 있어 루트 transform 보정과 충돌한다.

### 이번 수정

- 팝업 루트는 더 이상 `HudCurveTransformEffect` 대상에 포함하지 않는다.
- `HudCurveTransformEffect`에서 접선 회전 옵션을 제거해 같은 실수를 다시 못 하게 한다.
- 팝업 내부 Image/TMP는 `popupRelative` 메시 곡률만 적용한다.
- 팝업 내부 X 폭은 보존하고, 같은 팝업 카드 루트 기준의 공통 X/Z 곡면 offset을 Image/TMP에 동일하게 적용한다.
- 팝업 depth dampening을 제거해 `PopupRelativeDepthScale=1.0`으로 실제 메시 Z 곡률이 로그와 화면에서 보이게 한다.
- `[VRHud/Graphic]`, `[VRHud/TMP]` 로그에 `span=(x,z)`를 추가해 "적용 여부"가 아니라 실제 폭 압축과 깊이 변형량을 확인한다.

### 테스트 요청

1. Curved ON, 반경 2.0에서 스캔 완료/데이터뱅크 알림을 띄운다.
2. 알림 위치가 원래 왼쪽 위 위치에 유지되는지 확인한다.
3. 글자가 사라지지 않는지 확인한다.
4. 알림 테두리, 내부 이미지, 텍스트가 함께 휘어 보이는지 확인한다.
5. UI가 좌우로 눌려 짧아지는지 확인한다.

### 2026-04-26 추가 피드백 및 다음 수정

- 사용자 보고: 알림 위치는 정상에 가까워지고 글자도 보인다. 그러나 UI 전체가 여전히 평평하고, 커브드 각도에 따라 살짝 기울어진 평면판처럼 보이며, 좌우로 길쭉하게 늘어난다.
- 로그 확인: `Unlock` 팝업의 `popupRelative` 로그에서 `spanX`가 원래 폭과 거의 같게 유지되고 `spanZ`만 크게 증가한다. 즉 X를 보존한 채 Z만 밀어 넣어 실제 원통 표면이 아니라 비스듬한 평면에 가까운 메시가 만들어지고 있다.
- 원인: `preserveRelativeX=true`가 팝업 압축을 막기 위해 들어갔지만, 팝업이 화면 왼쪽 극단(`x/r`이 약 -90도 이상)에 있어 Z 기울기만 커지고 X 원통 투영이 사라졌다.
- 수정 계획: 팝업은 루트 transform을 건드리지 않고 메시만 처리한다. 다만 내부 메시의 X도 원통 투영하되, 팝업 접선 각도는 ±60도로 제한해 edge-on 소실과 과도한 좌우 압축을 피한다.
- 기대 결과: 글자/위치는 유지하면서, `spanX`가 기존보다 줄고 `spanZ`와 함께 실제 휘어진 표면으로 보인다.

### 2026-04-26 추가 피드백 및 구조 수정 계획

- 사용자 보고: 알림은 구부러지지만 커브드 화면에 붙은 것이 아니라, 기울어지지 않은 채 정면에서 혼자 구부러진 느낌이다.
- 로그 확인: `Unlock` 알림의 `mode=popupArc` 줄에서 `euler=(0,0,0)`과 `dot=1.000`이 유지된다. 즉 메시 vertex는 변형됐지만 실제 시각 루트는 커브드 접선 방향으로 회전하지 않았다.
- 원인: 팝업 루트 transform을 직접 돌리면 게임의 슬라이드 애니메이션과 충돌했고, 메시만 돌리면 루트 방향이 계속 flat이라 VR에서 커브드 캔버스에 붙어 보이지 않는다.
- 수정 계획:
  - 게임이 애니메이션하는 `Unlock`/`Journal` 같은 팝업 카드 루트는 계속 건드리지 않는다.
  - 팝업 카드 루트 안에 `__VRHudPopupCurveVisual` wrapper를 만들고 기존 시각 자식들을 wrapper 아래로 이동한다.
  - wrapper만 안정된 최종 x 기준으로 커브드 위치 offset과 접선 회전을 받는다. 안정 x는 활성 중 관측된 값 중 화면 중앙에 가장 가까운 값으로 잡아, 등장/퇴장 애니메이션 중 z가 크게 튀지 않게 한다.
  - wrapper 아래 Graphic/TMP는 전역 `popupArc`가 아니라 wrapper 로컬 좌표 기준 `popupLocal` 메시 곡률만 받는다.
  - 로그에 `[VRHud/PopupRoot]`를 추가해 `currentX`, `stableX`, `yaw`, `offset`, wrapper 생성 여부를 직접 확인한다.

---

## 현재 수정 계획서 — 핑/신호기 투영·가장자리·화살표 보정

### 문제

신호기/핑 UI가 대략적인 방향은 맞지만 고개를 돌릴수록 정확한 위치에서 조금씩 어긋난다. 정면을 보고 있을 때는 비교적 맞는다.
1차 진단 패치 제거 후 사용자 테스트 결과, 고개를 돌릴 때 핑이 움직이는 방향은 정상이나 이동량이 너무 적어 실제 사물 위치보다 덜 따라온다.
`uGUI_Ping.SetScale` Postfix 방식에서는 컨트롤러가 정상 작동했고, `핑 위치 보정`을 2.0까지 올리면 좌우 움직임은 거의 정확해졌다. 남은 문제는 상하 움직임 보정이다.
2차 테스트 결과 좌우는 2.0이 적당하고, 상하는 3.0도 부족하다. 또한 핑이 화면 가장자리에 붙는 범위가 좌우는 너무 넓고 상하는 너무 좁아 아직 여유 시야가 있는데도 위/아래에서 막힌다.
가끔 좌우 화면 밖 핑의 화살표가 화면 바깥쪽이 아니라 안쪽을 가리키는 경우가 있다.
3차 테스트 결과 최적값은 좌우 1.85, 상하 3.5이며 가장자리 범위 조절은 정상이다. 남은 문제는 핑 투영 기준점이 HUD 기준보다 위에 잡힌 느낌이고, 좌우 화면 밖 화살표가 여전히 안쪽을 가리키는 점이다. 상하 화살표는 정상이다.
4차 테스트 결과 가장자리 기준 위치는 내려와서 좋아졌지만, `HUD 상하 위치`가 내려간 만큼 핑 내용은 오히려 올라간 것처럼 보인다. 또한 X 부호를 무조건 뒤집은 화살표 보정은 정상/비정상 케이스를 서로 뒤바꿨다.
5차 테스트 결과 핑이 살짝 내려갔다. 검증 완료된 움직임 배율은 건드리지 말고 별도 위치 조정 슬라이더로 미세 조정한다. 화살표는 여전히 불규칙하며, 일부 핑은 왼쪽 화면 밖에서는 정상인데 오른쪽 화면 밖에서도 왼쪽 화살표로 표시된다.

### 원인 분석

- `uGUI_Pings.OnWillRenderCanvases()` IL 확인 결과, 핑 위치 계산은 `MainCamera.camera`의 `worldToLocalMatrix`, `aspect`, `fieldOfView`, `forward`, `position` 기준으로 수행된다.
- 원본 계산은 `local.x / (abs(local.z) * tan(fieldOfView/2) * aspect)`와 `local.y / (abs(local.z) * tan(fieldOfView/2))`로 normalized 화면 좌표를 만든 뒤 ping canvas half-size를 곱한다.
- VR에서 `Camera.fieldOfView`가 실제 HUD 체감 FOV보다 넓게 잡히면 denominator가 커지고, 같은 머리 회전/월드 각도에도 ping 이동량이 부족해진다.
- 사용자 관찰이 "방향은 맞고 이동량만 부족"이므로, 계산 기준 자체를 바꾸기보다 centered ping canvas 좌표에 보정 배율을 곱하는 것이 가장 작은 수정이다.
- 원본 `OnWillRenderCanvases`는 화면 밖일 때 `SetAngle(atan2(y,x))`로 화살표를 먼저 회전시킨 뒤 마지막에 `SetScale()`을 호출한다. 현재 보정은 `SetScale` Postfix에서 위치를 다시 바꾸므로, 화살표 각도가 보정 전 위치 기준으로 남을 수 있다.
- 도보 HUD 실제 기준 Y는 `FootHudPosition() = 0.1 + HudVerticalOffset`이다. 핑 보정은 parent rect 중앙만 기준으로 쓰고 있어, HUD 위치 조정과 기준점이 분리되어 보일 수 있다.
- 4차 수식은 HUD 기준점을 스케일 피벗으로도 사용했다. `adjusted = center + (source - center) * scale` 구조라서 상하 배율이 3.5처럼 1보다 클 때 기준점을 내리면 `(1 - scale) * center` 항 때문에 핑 내용이 반대로 올라간다.
- 좌우 화살표는 X 부호를 무조건 뒤집으면 기존에 맞던 케이스도 틀어진다. 거리/behind 여부를 `SetScale` 후처리에서 직접 알 수 없으므로, 현재 화살표 방향 벡터가 최종 표시 위치 벡터와 반대일 때만 180도 보정하는 방식이 더 안전하다.
- 5차 화살표 피드백상 여전히 추측 보정으로는 부족하다. 거리, behind 판정, 화살표 graphic의 기본 방향, 원본 `SetAngle()` 값 중 어느 값이 실제 화살표 방향과 불일치하는지 로그로 분리해야 한다.

### 수정 계획

- `uGUI_Pings.OnWillRenderCanvases`는 Postfix/Transpiler 모두 금지한다. 이 메서드를 패치하면 게임 시작 직후 컨트롤러 입력이 사라지는 부작용이 재현됐다.
- 대신 `uGUI_Ping.SetScale` Postfix에서 이미 원본이 계산해 둔 `rectTransform.anchoredPosition`만 읽고, ping canvas 중심 기준 상대 좌표를 보정한다.
- 좌우 배율은 기존 저장값 호환을 위해 `Settings.PingProjectionScale`을 유지하고, 상하 배율은 새 `Settings.PingProjectionVerticalScale`로 분리한다.
- 옵션은 `핑 좌우 위치 보정`, `핑 상하 위치 보정` 두 슬라이더로 나눈다. 좌우 기본값은 1.35, 범위는 0.5~3.0이다. 상하는 3.0도 부족했으므로 범위를 0.5~8.0으로 확장한다.
- 가장자리 고정 범위도 `핑 좌우 가장자리 범위`, `핑 상하 가장자리 범위`로 분리한다. 1.0은 현재 parent rect 기준이고, 값이 작으면 중심 쪽에서 더 빨리 막히며, 값이 크면 더 바깥까지 이동한다.
- 방향 계산, 표시 여부, 거리/텍스트/화살표 각도 계산은 원본 코드 그대로 둔다.
- 단, 화살표가 이미 켜진 상태라면 최종 보정/클램프된 위치 기준으로 `SetAngle()`을 다시 호출해 안쪽을 가리키는 케이스를 줄인다.
- 핑 중심점 Y는 `0.1 + HudVerticalOffset`을 `0.00085 * HudScale`로 나눈 canvas pixel 값만큼 이동해 도보 HUD 기준과 동기화한다.
- HUD 기준점 Y는 최종 표시 중심/가장자리 기준에만 더한다. 투영 배율의 피벗은 원본 ping canvas 중심으로 유지해 HUD 오프셋이 반대로 증폭되지 않게 한다.
- 화살표는 새 각도를 직접 계산하지 않는다. 원본 `SetAngle()` 결과를 읽고, 현재 방향 벡터와 최종 표시 위치 벡터의 내적이 음수일 때만 180도 뒤집는다.
- `핑 좌우/상하 위치 보정` 라벨은 실제 의미에 맞춰 `핑 좌우/상하 움직임 배율`로 변경한다.
- `PingVerticalOffset`과 `핑 상하 위치 조정` 슬라이더를 추가한다. 기존 배율/가장자리 값은 유지하고, 최종 display center에만 위치 offset을 더한다.
- `PingDebugLogs` 옵션을 추가한다. 켜져 있을 때 `[VRHud/Ping]` 로그에 원본 anchored position, 상대 벡터, 최종 벡터, limit, label/distance text, arrow enabled, arrow angle before/after, dot, correction 여부를 제한 출력한다.
- 한글 표시가 정상 확인되었으므로 Submersed VR/탑승물 VR/앰비언트 오클루전 옵션 표시 문자열을 한글화한다. 내부 저장값에 의존하는 선택지는 표시값만 한글로 바꾸고 저장값은 기존 영문 값을 유지한다.

### 실패 이력

- 1차 시도: `uGUI_Pings.OnWillRenderCanvases` Postfix에서 projection 값을 읽는 방식.
- 결과: 게임 시작 직후 컨트롤러 입력이 작동하지 않는 부작용 발생. `OnWillRenderCanvases`는 입력/UI 초기화와 같은 프레임에 매우 이른 빈도로 호출되므로 진단 Postfix도 안전하지 않다고 판단하고 즉시 제거한다.
- 2차 시도: `uGUI_Pings.OnWillRenderCanvases` Transpiler로 위치 배율만 삽입. 결과 역시 게임 시작 직후 컨트롤러가 사라짐. 결론: `uGUI_Pings.OnWillRenderCanvases` 자체를 패치 대상에서 제외한다.
- 로그 추출 스크립트의 `[VRHud/Pings]` 패턴은 다음 안전한 진단용으로 유지한다.

### 테스트 요청

1. 게임 시작 직후 컨트롤러가 정상 작동하는지 먼저 확인한다.
2. 신호기/핑 하나를 보이게 두고 `핑 좌우 위치 보정`은 이전 테스트 기준 2.0 근처에서 시작한다.
3. 상하가 부족하면 `핑 상하 위치 보정`을 3.0 이상으로 올리고, 과하면 내린다.
4. 좌우 가장자리에서 너무 늦게 막히면 `핑 좌우 가장자리 범위`를 내리고, 위/아래가 너무 빨리 막히면 `핑 상하 가장자리 범위`를 올린다.
5. 화면 밖 화살표가 안쪽을 가리키는 경우가 남으면, 좌/우/상/하 중 어느 가장자리에서 발생했는지 보고한다.
6. `HUD 상하 위치`를 움직였을 때 핑 투영 기준점도 같은 방향으로 따라오는지 확인한다.
7. 화살표 문제 재현 시 `핑 디버그 로그`를 켜고 좌/우 화면 밖 케이스를 짧게 재현한 뒤 로그를 `test_log/`에 복사한다.

### 2026-05-18 적용

- 실패한 `uGUI_Pings.OnWillRenderCanvases` Transpiler 제거.
- `uGUI_Ping.SetScale` Postfix 기반 `PingProjectionScaleFixer`로 변경.
- `Settings.PingProjectionScale` 추가 및 옵션 슬라이더 `핑 위치 보정` 추가.
- 새 옵션은 한글 표시 테스트를 겸해 한글 라벨/설명으로 넣었다.

### 2026-05-18 2차 적용

- 사용자 확인: 컨트롤러 정상. 좌우는 슬라이더 2.0에서 거의 정확. 상하는 여전히 이상함.
- `Settings.PingProjectionVerticalScale` 추가.
- `PingProjectionScaleFixer`에서 X/Y 상대 좌표를 각각 `PingProjectionScale`, `PingProjectionVerticalScale`로 별도 보정.
- 기존 `핑 위치 보정`은 `핑 좌우 위치 보정`으로 변경하고, `핑 상하 위치 보정`을 추가.
- 두 핑 보정 슬라이더 범위를 0.5~3.0으로 확장.
- 한글 표시가 정상 확인되어 모드 옵션 라벨/설명/선택지를 한글화. `ShowLaserPointer`, AO 방식/샘플 같은 내부 문자열 설정은 기존 영문 저장값으로 변환해 호환성을 유지한다.

### 2026-05-18 3차 적용

- `Settings.PingProjectionVerticalScale` 범위를 0.5~8.0으로 확장.
- `Settings.PingHorizontalEdgeScale`, `Settings.PingVerticalEdgeScale` 추가.
- `PingProjectionScaleFixer`에서 최종 위치 클램프를 parent rect 고정값이 아니라 axis별 edge scale로 계산.
- `uGUI_Ping.arrow.enabled`가 true인 경우 최종 위치 기준으로 `SetAngle()`을 재호출.
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `InputTracking.Recenter()` obsolete 경고 1개만 남음.

### 2026-05-18 4차 적용

- 핑 보정 기본값을 사용자 검증값에 맞춰 좌우 1.85, 상하 3.5로 변경.
- `PingProjectionScaleFixer` 기준점을 parent rect 중앙에서 `HUD 상하 위치`가 반영된 기준점으로 변경.
- 좌우 지배 벡터의 화살표 각도 재계산 시 X 부호를 뒤집어 좌/우 화면 밖 화살표가 바깥쪽을 가리키도록 보정.
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `InputTracking.Recenter()` obsolete 경고 1개만 남음.

### 2026-05-18 5차 적용

- 투영 배율 피벗은 parent rect 중앙으로 되돌리고, `HUD 상하 위치` 기준은 최종 display center에만 더한다.
- 가장자리 클램프도 최종 display center 기준으로 유지한다.
- 화살표 보정은 무조건 X 반전이 아니라, 원본 화살표 방향이 최종 표시 벡터와 반대일 때만 180도 뒤집는 방식으로 변경한다.
- helper 메서드는 Harmony patch class 밖의 `PingProjectionHelpers`로 분리해 Harmony analyzer 오인 경고를 피한다.
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `InputTracking.Recenter()` obsolete 경고 1개만 남음.

### 2026-05-18 6차 적용

- `Settings.PingVerticalOffset` 추가 및 `핑 상하 위치 조정` 슬라이더 추가.
- 검증 완료된 움직임 배율 기본값 좌우 1.85, 상하 3.5는 유지.
- 기존 `핑 좌우/상하 위치 보정` 라벨을 `핑 좌우/상하 움직임 배율`로 변경.
- `Settings.PingDebugLogs` 추가 및 디버그 옵션에 `핑 디버그 로그` 토글 추가.
- `[VRHud/Ping]` 상세 로그 추가. 로그는 arrow가 켜진 ping 중심으로 제한 출력하고, `tools/vrhud_log_extract.py` 기본 패턴에도 포함한다.
- 화살표는 이번 단계에서 더 이상 강제 보정하지 않고, 원본 각도 기준 `wouldFlip` 진단값만 남긴다.
- 빌드 확인: C# 컴파일과 DLL 생성 성공. 기존 `InputTracking.Recenter()` obsolete 경고 1개만 남음.

### 2026-05-18 7차 적용

- 사용자 확인: `핑 상하 위치 조정`은 정상 작동한다. 좌우/상하 움직임 배율은 최적값을 찾았으므로 옵션에서 제거한다.
- 최신 `[VRHud/Ping]` 로그 분석 결과, 좌우 부호 자체는 맞다. `side=right`는 대체로 0도 근처, `side=left`는 180도 근처로 기록되어 수학적 좌우 판정은 뒤집혀 있지 않다.
- 그러나 현재 `PingProjectionScaleFixer`는 원본 `uGUI_Pings.OnWillRenderCanvases()`가 계산한 위치/각도 이후에 `anchoredPosition`만 다시 보정하고, 화살표 각도를 최종 보정 좌표 기준으로 다시 계산하지 않는다.
- 로그상 보정 후 `adjRel` 기준 목표 각도와 실제 `arrow.rectTransform.rotation.z` 사이에 최대 약 27도 차이가 있다. 특히 상하 배율과 edge clamp가 적용된 대각선 가장자리에서 차이가 커진다.
- 수정:
  - `uGUI_Pings.OnWillRenderCanvases`는 계속 패치하지 않는다. 이 메서드 패치는 컨트롤러 입력이 사라지는 부작용이 있었다.
  - `uGUI_Ping.SetScale` Postfix에서 최종 `adjustedRelative` 기준 `atan2(y,x)` 각도를 계산하고 `uGUI_Ping.SetAngle()`을 다시 호출한다.
  - IL 확인 결과 `SetAngle()`은 `SetScale()`을 다시 호출하지 않으므로 재귀 위험은 없다. 단, 음수 각도를 받으면 화살표를 disable하므로 `atan2` 결과는 0~360도로 정규화해서 넘긴다.
  - 검증된 움직임 배율은 옵션에서 제거하고, 런타임 계산은 고정값 좌우 1.85 / 상하 3.5를 사용한다. 기존 public 설정 필드는 저장 호환용으로만 남긴다.
  - `[VRHud/Ping]` 로그에는 `angleBefore`, `targetAngle`, 최종 `angle`, arrow transform scale/euler를 남겨, 그래도 시각 방향이 틀리면 이미지/부모 transform 미러링 여부를 다음 로그에서 바로 판독할 수 있게 한다.
  - `tools/vrhud_log_extract.py`는 상태 파일 쓰기가 잠겨도 로그 추출 결과 자체는 실패 처리하지 않도록 수정한다.
- 빌드 확인: C# 컴파일과 DLL/zip 생성 성공. 기존 `InputTracking.Recenter()` obsolete 경고 1개만 남음.

### 2026-05-18 8차 적용

- 사용자 확인: 화살표 방향은 여전히 이상하다. 완전 빌드는 꼭 필요한 시점 또는 최종 적용 시점에만 수행한다.
- 최신 로그는 전체 로그 앞부분이 아니라 마지막 `[VRHud/Ping]` 구간만 `--pattern "\[VRHud/Ping\]" --last 800`으로 확인한다.
- 로그 판독:
  - `targetAngle`과 최종 world `angle`은 일치한다.
  - 하지만 `arrowLE=(x,y,z)`에서 local X/Y가 10~18도, 120~160도처럼 크게 섞인다.
  - 원인: 게임 원본 `uGUI_Ping.SetAngle()`은 `arrow.rectTransform.rotation = Quaternion.Euler(0,0,angle)`로 world rotation을 설정한다. 2D overlay에서는 문제가 없지만, VR HUD 캔버스처럼 부모가 3D로 기울어진 상태에서는 화살표가 UI 로컬 평면 기준으로 회전하지 않는다.
- 수정 계획:
  - `SetScale` Postfix에서는 기존처럼 최종 `adjustedRelative` 기준 `targetAngle`을 계산한다.
  - 원본 `SetAngle(targetAngle)`은 화살표 enable 상태 갱신용으로 호출하되, 직후 `arrow.rectTransform.localRotation = Quaternion.Euler(0,0,targetAngle)`로 덮어써 UI 평면 기준 회전을 보장한다.
  - 로그의 `angle`은 local z 기준으로 바꾸고, `worldAngle`도 함께 남겨 world/local 차이를 다음 로그에서 확인한다.
- 빌드 정책: 완전 빌드는 최종 적용 시점에만 1회 수행한다. 중간 로그 분석/코드 검토에는 완전 빌드를 반복하지 않는다.
- 빌드 확인: C# 컴파일과 DLL/zip 생성 성공. 기존 `InputTracking.Recenter()` obsolete 경고 1개만 남음.
