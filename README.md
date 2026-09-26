# 바이러스의 비밀

resources의 종합 기획안 V4, 튜토리얼 V3, 캐릭터 시트를 바탕으로 만든 Unity 3D 어드벤처입니다. 사용자의 3D 전환 지시에 따라 이전 도트 월드를 입체 모델·원근 카메라·실시간 조명으로 교체했습니다.

시작 화면은 원본 캐릭터 시트 세 장을 참조해 생성한 2D 일러스트를 사용합니다. 제목과 메뉴는 Unity UI로 표시합니다.

## 실행

- Unity 버전: `6000.6.3f1`.
- Unity에서 `Assets/Game/Scenes/Campaign.unity`를 열고 Play를 누릅니다.
- Windows 빌드가 있다면 루트의 `start.cmd` 또는 `Builds/Windows/SecretOfVirus.exe`를 실행합니다.
- 프로젝트 최초 구성은 자동으로 실행됩니다. 수동 구성은 `Secret of Virus > Configure Campaign`입니다.

## 조작

| 조작 | 기본 키 |
|---|---|
| 이동 및 메뉴 선택 | 방향키 |
| 확인 및 정지 상태의 조사 | Z / Enter |
| 취소 및 달리기 | X / Shift |
| 동료 순차 교체 | C |
| 다은 / 제임스 / 다니엘 | 1 / 2 / 3 |
| 소지품 / 단서 / 파티 기록 | I / Tab |
| 일시정지 | ESC |

버튼은 마우스로도 누를 수 있습니다. 환경 설정에서 확인·취소·교체·기록 키, 글자 크기·속도, 음량, 흔들림과 깜빡임을 조절할 수 있습니다.

## 게임의 구성

공방 제작 → 동생과의 일상 → 제임스와 다니엘 합류 → 검문과 수용 구역 탈출 → 잠입 또는 전투 → A 협동 퍼즐 → B 약품 소거 → C 대화 추리 → 제한 기록실 → 최종 준비 → 소장의 거래와 3페이즈 전투 → A/B/C/D 엔딩 순서입니다.

일반 전투의 패배는 전투 직전 상태를 복원합니다. 최종 전투의 패배는 D 엔딩이며, 마지막 선택 직전의 저장으로 재도전할 수 있습니다. 튜토리얼 중 저장은 금지되며 수리 키트 완료 대사가 끝나면 처음 자동 저장합니다.

## 데이터와 파일

- `Assets/Game/Scripts/Core`: 아이템·제작·전투·사건·저장 규칙, 스토리 데이터.
- `Assets/Game/Scripts/Runtime`: 게임 진행, 사선 시점 3D 월드, UI, 오리지널 합성 오디오. `Model3D`는 캐릭터·가구 모델과 관절 동작, `Showcase3D`는 타이틀·전투·초상화 렌더링을 담당합니다. `PixelArt`는 UI 아이콘 및 색상 헬퍼에만 남아 있습니다.
- `Assets/Game/Editor`: 씬 구성, 검증, Windows 빌드 메뉴, 로컬 명령 처리.
- `resources`: 원본 자료. 변경하지 않았습니다.
- `tutorial_scripts_fixed`: 기존 초안. 변경하지 않았습니다. 이동 정규화·속도·방향과 카메라 경계 개념을 런타임에 반영했습니다.
- `Documentation/구현과 검증.md`: 기획 대응표와 검증 범위.

저장은 Windows의 `%USERPROFILE%\AppData\LocalLow\Secret Virus Studio\바이러스의 비밀\Campaign`에 생성됩니다. `auto`, `manual`, `before-final` 슬롯과 이전 정상 저장의 `.bak`, 환경 설정 및 엔딩 감상 기록을 구분합니다. 튜토리얼 도중 종료하면 처음부터 재시작합니다.

## 개발 검증

Unity 메뉴의 `Secret of Virus > Run Rules Verification`에서 규칙 검사를 실행합니다. 프로젝트가 열린 상태에서 다음 명령도 사용할 수 있습니다.

```powershell
powershell -NoProfile -File Tools/Invoke-Campaign.ps1 -Action tests
powershell -NoProfile -File Tools/Invoke-Campaign.ps1 -Action play
powershell -NoProfile -File Tools/Invoke-Campaign.ps1 -Action journey
powershell -NoProfile -File Tools/Invoke-Campaign.ps1 -Action traversal
powershell -NoProfile -File Tools/Invoke-Campaign.ps1 -Action branches
```

규칙 검사는 임시 파일만 사용하고, 진행 경로 검사는 사용자 저장을 쓰지 않습니다. `journey` 검사는 이야기 연결을 검증하기 위해 전투마다 HP를 보정하므로 난이도 검증으로 해석하지 않습니다. `traversal`은 맵의 충돌과 조사 가능 위치를 검사합니다. 실제 키보드 플레이는 별도 검증입니다.

Windows 실행 파일의 `--smoke`는 저장을 변경하지 않는 개발 검증 모드입니다. 6개 대표 화면과 폰트·오디오 리스너 로딩 결과를 실행 파일 옆 `Verification` 폴더에 남기고 종료합니다.

## 자산 출처

픽셀 그래픽과 배경음·효과음은 프로젝트 코드에서 생성합니다. 캐릭터 외형은 resources의 사용자 제공 시트를 기준으로 재해석했습니다. 배포용으로 사용자가 제공한 캐릭터 설정의 권리는 사용자 측 자료를 따릅니다.

한글 폰트는 [Noto CJK의 Noto Sans CJK KR Regular](https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Korean)이며 SIL Open Font License 원문을 `Assets/Game/Resources/NotoSansKR-OFL.txt`에 포함했습니다.
