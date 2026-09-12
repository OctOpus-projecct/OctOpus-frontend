# 시작 마을 3D 모델

[승인 시안](art/starting-village-approved.png)을 바탕으로 제작한 실제 Unity 메시다. [얼굴 확대](art/starting-village-faces.png), [캐릭터 확대](art/starting-village-characters.png), [도끼 잡는 자세](art/starting-village-axe-grip.png), [12종 모델](art/starting-village-models.png), [마을 배치](art/starting-village-render.png)는 프리팹의 실제 렌더다. 로그인한 게임창 캡처는 아니며, 도끼 확대는 팔을 0·55·110도로 움직인 자세 검증이다.

## 구성과 표현

플레이어, 도끼 없는 주민, 도끼, 가방, 나무, 그루터기, 목재, 집, 공방, 수정 가로등, 이정표, 돌·풀·꽃을 Assets/Resources/Village에 보관한다. 시작 지역은 작은 시골 마을이다. 마법학교·건물 입장·제작 기능은 포함하지 않는다.

캐릭터 몸통·소매·바지·부츠·앞치마는 체형을 정의한 곡면이다. 얼굴·손은 OrganicSculpt의 암시적 표면을 메시로 만들고 중복 꼭짓점을 합친다. 코와 귀는 얼굴 표면에 연결되며, 볼 색은 별도 타원 부품 대신 피부 정점 색으로 표현한다. 얼굴 깊이와 코·귀 돌출을 줄이고 작은 눈·눈썹·입을 배치했다.

머리카락은 HairSurface가 두피를 감싸는 하나의 닫힌 곡면으로 만든다. 비대칭 앞머리와 흐르는 굴곡을 같은 표면에 정의하며 눈·눈썹을 가리지 않게 한다. 주민 콧수염은 끝이 가늘어지는 곡선이다. 얼굴 확대는 같은 조명·카메라 조건의 플레이어와 주민을 보여준다.

오른손은 도끼 축을 기준으로 손가락을 만들고 손잡이·기울어진 감개가 들어갈 공간을 확보한다. HeldAxe와 GripAxis가 같은 Hand 아래에 있어 팔을 움직여도 잡는 위치가 유지된다. 주민은 도끼를 들지 않는다.

CharacterModelView는 기존 CapsuleCollider를 유지하고 기본 Renderer만 숨긴다. 복제된 위치로 걷기를 표현하며 서버의 StrikeSequence 변경에 맞춰 타격 동작을 표시한다. TreeModelView는 기존 Collider를 사용하고 서버 체력 감소·고갈·재생에 따라 흔들림·그루터기·나무를 표시한다. 보상·독점·무게·재생·이동 판정은 서버 소유다.

집과 공방은 이동 경계 밖의 장식이며 Collider와 NetworkObject가 없다. 카메라는 전체 viewport와 장식 경계를 포함한다.

## 생성·검증 명령

저장소 루트의 PowerShell에서 실행한다.

```powershell
./scripts/unity.ps1 -Task BuildModels
./scripts/unity.ps1 -Task Test
./scripts/unity.ps1 -Task RenderModels
./scripts/unity.ps1 -Task BuildWindows
```

생성 코드는 Unity/Assets/Editor/VillageArt에 있다. BuildModels는 기존 자산을 갱신하고 .meta를 보존한다. 생성 메시·머티리얼·프리팹·피부 셰이더와 .meta를 함께 커밋한다. 생성 순서 변경 시 자산 매핑을 검토한다.

RenderModels는 Unity 카메라/RenderTexture로 TestResults/village-models에 PNG를 만든다. 열린 에디터의 메뉴에서 실행하면 장면 저장 확인 후 새 장면으로 전환한다. 프로젝트 그래픽 설정은 변경하지 않는다. 실행 파일은 Builds/WindowsClient/OctOpus.exe다.

## 검증 범위

PlayMode 39개 통과: 기존 입력·맵, 12종 자산과 물리·네트워크 컴포넌트 부재, 주민 도끼 부재, 관절 경로를 확인한다. 추가 검사는 얼굴·머리카락·손의 모서리 폐쇄와 일관된 면 방향·양의 체적·정점 연결성, 도끼와 감개를 포함한 피부 간격, 세 가지 팔 각도에서 잡는 위치를 확인한다. 엄밀한 manifold 판정이나 모든 애니메이션의 전신 간섭 검사는 아니다.

초기 메시에서 뒤집힌 미세 면과 감개의 손 간섭을 테스트로 재현했다. 사면체의 안팎에 따른 면 방향 결정, 중복 정점 용접, 감개까지 고려한 공간 확보로 수정했다. 얼굴 비례·두피 곡면 머리카락·콧수염 수정 후 재생성·재렌더·PlayMode 39개·Windows 빌드로 확인했다. 독립 코드 리뷰의 감개 간섭 지적도 반영했다.

두 클라이언트 맵·과적 회귀는 최초 마을 적용 시 통과했다. 이번 캐릭터 수정은 런타임 입력·통신을 변경하지 않았으며 해당 회귀를 반복하지 않았다. 새 외형의 사용자 플레이, 전신 충돌 없는 애니메이션 조정, 10명 그래픽 성능 측정과 메시 최적화는 남아 있다. 서버 동반 변경 없음, 공용 패키지 0.7.0 유지. 계정 PR #21·UI PR #23에 의존하며 PR 대상은 dev다.
