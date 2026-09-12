# 시작 마을 3D 모델

승인한 [컨셉 시안](art/starting-village-approved.png)을 바탕으로 제작한 Unity 메시다. [실제 모델 모음](art/starting-village-models.png)과 [마을 배치 렌더](art/starting-village-render.png)는 Unity 카메라가 프리팹을 직접 렌더한 이미지이며, 로그인한 게임 화면 캡처는 아니다.

## 구성

플레이어, 도끼 없는 주민, 도끼, 가방, 나무, 그루터기, 목재, 주택, 공방, 수정 가로등, 이정표, 돌·풀·꽃 12종을 `Unity/Assets/Resources/Village`에 저장한다. 작은 시골 마을이며 마법학교는 포함하지 않는다. 집과 공방은 장식이며 입장·제작 기능이 없다.

`CharacterModelView`는 공유 플레이어의 기존 CapsuleCollider를 유지하며 기본 Renderer를 숨긴다. 서버에서 복제된 위치로 걷기를 표현하고 승인된 StrikeSequence가 변할 때 도끼를 휘두른다. 주민은 도끼를 소지하지 않는다.

`TreeModelView`는 기존 나무 Collider를 사용한다. 서버 체력 감소 시 흔들리고 고갈되면 그루터기로 바뀌며 재생 시 나무와 클릭 Collider가 돌아온다. 아이템 지급, 독점, 무게, 재생 시간, 이동 판정은 서버 소유 그대로다.

집과 공방은 이동 경계 밖에 배치한다. 장식에는 Collider나 NetworkObject가 없다. 맵 카메라는 전체 viewport를 사용하고 장식 경계까지 화면에 포함한다.

## 모델 수정과 실행

저장소 루트의 PowerShell에서 실행한다.

```powershell
./scripts/unity.ps1 -Task BuildModels
./scripts/unity.ps1 -Task RenderModels
./scripts/unity.ps1 -Task Test
./scripts/unity.ps1 -Task BuildWindows
```

생성 코드는 `Unity/Assets/Editor/VillageArt`에 있다. BuildModels는 기존 에셋을 갱신하며 `.meta`를 삭제하지 않는다. 생성된 메시·머티리얼·프리팹과 `.meta`를 함께 커밋한다. 생성 순서를 바꾸면 자산 매핑도 검토한다.

RenderModels는 그래픽 드라이버를 사용하는 Unity 카메라/RenderTexture로 `TestResults/village-models`에 PNG를 만든다. 열린 에디터에서 메뉴를 실행하면 새 빈 장면으로 전환하므로 작업 장면을 먼저 저장한다. 빌드 결과는 `Builds/WindowsClient/OctOpus.exe`다.

## 검증과 한계

PlayMode 35개 통과: 기존 입력·맵 검증 및 12종 자산의 메시·머티리얼 존재, 물리·네트워크 컴포넌트 부재, 주민 도끼 부재, 캐릭터 관절 계층을 확인했다. 실제 메시 렌더를 확인하고 건물 앞면 및 카메라 비율 문제를 수정했다.

10명 그래픽 성능 측정, 사용자의 새 모델 조작감 확인은 남아 있다. 컨셉 이미지와 픽셀 단위 일치를 보장하는 모델이 아니며 사용자 피드백에 맞춰 모델 생성 코드를 조정할 수 있다. 공유 패키지 0.7.0과 서버 변경은 필요 없다. 선행 계정 PR #21, UI PR #23을 포함한 작업 브랜치이며 PR 대상은 dev다.

Windows 빌드 및 두 클라이언트 맵 회귀(이동 경계, 독립 벌목·취소·고갈·재생), 과적 인벤토리 회귀(목재 지급·중복 거절·소유자 정보·이동속도)도 통과했다. 이 회귀는 그래픽 없는 실제 실행 파일 검증이다. 별도 코드 리뷰의 건물 촬영 방향·카메라 출력 비율 지적을 수정하고 재렌더했다.

## 곡면 보완
사용자 피드백에 따라 머리카락과 가방끈을 연속된 곡면 메시로 다시 만들고 의상·부츠·가방의 곡률을 조정했다. 건물은 평평한 면을 유지한 채 모서리를 둥글게 처리한다. 수관과 돌은 위도선 격자 대신 삼각형 면을 사용하며 나무·그루터기 뿌리는 곡선으로 이어진다. 나이테의 색 대비도 낮췄다. 미리보기는 바닥 그림자를 포함하며 프로젝트의 그래픽 설정을 변경하지 않는다.

이번 보완 후 모델 생성·실제 렌더 확인·PlayMode 35개·Windows 빌드를 다시 통과했다. 런타임 입력·네트워크 코드는 이번 보완에서 변경하지 않았으며 두 클라이언트 회귀는 최초 적용 때의 검증이다. 새 외형의 사용자 플레이 확인과 10명 성능 검증은 남아 있다.
