# Unity 클라이언트 초기 구성

`Unity`가 클라이언트 프로젝트다. 엔진·FishNet·공용 패키지 버전은 [서버 실행 안내](../../OctOpus-backend/docs/unity-setup.md)와 양쪽 manifest를 따른다. 서버와 클라이언트는 같은 공용 SHA를 사용한다. 두 저장소를 같은 부모 디렉터리에 클론하면 아래 통합 테스트를 그대로 실행할 수 있다.

## 빌드와 테스트
Unity 6000.3.23f1 라이선스 활성화, Git, PowerShell 7, 비공개 백엔드 패키지 읽기 권한이 필요하다. 실행 중인 Unity Editor와 포트 7770의 이전 테스트 서버를 종료한 뒤 저장소 루트에서 실행한다.

1. 먼저 백엔드에서 Windows 전용 서버를 빌드한다.
2. 프런트엔드에서 아래 명령을 순서대로 실행한다.

```powershell
pwsh -NoProfile -File scripts/unity.ps1 -Task BuildWindows
pwsh -NoProfile -File scripts/unity.ps1 -Task Test
pwsh -NoProfile -File scripts/test-local-session.ps1
```

다른 Editor 경로는 `-EditorPath`로 전달한다. 다른 위치의 빌드를 사용할 때 세션 스크립트의 `-ServerPath`, `-ClientPath`로 각각 실행 파일을 지정한다. PlayMode 재접속 테스트는 형제 디렉터리의 백엔드 빌드를 사용한다.

플레이어 출력은 `Builds/WindowsClient/OctOpus.exe`이며 Data 폴더와 DLL을 함께 유지한다. 테스트 로그·XML은 `TestResults`에 저장되고 Git에서 제외된다. 포트 충돌 검사, 공용 패키지 테스트, 클라이언트 테스트는 같은 포트를 사용하므로 병렬 실행하지 않는다.

## 접속 화면
Windows 전용 서버를 실행한 뒤 클라이언트를 실행한다. 기본 주소는 127.0.0.1, 포트는 UDP 7770이다. **Connect**로 접속하고 **Disconnect**로 종료한다. 같은 창에서 다시 Connect를 누르면 기존 네트워크 매니저를 재사용한다. `OCTOPUS_SERVER_ADDRESS` 환경변수가 있으면 주소 입력의 초기값으로 사용하며 화면에서 수정할 수 있다.

WSL 서버는 [서버 안내](../../OctOpus-backend/docs/unity-setup.md)의 `scripts/run-wsl-server.ps1`로 실행하고 출력된 WSL IPv4를 두 게임 창에 입력한다. 서버를 켜 둔 상태에서 `scripts/test-local-session.ps1 -ExternalServer -ServerAddress WSL_IPV4`로 자동 검증할 수도 있다. 실제 WSL 주소로 바꿔 실행한다. 외부 서버에는 다른 사용자가 없어야 하며 스크립트는 테스트 클라이언트만 종료한다.

화면에는 연결 상태, 플레이어 수·ID와 서버가 확정한 X/Z 좌표가 나온다. 서버에서 생성한 실제 플레이어 캡슐을 표시하며 자신은 초록색과 **You**, 다른 플레이어는 주황색으로 구분한다. 지면을 클릭하면 자신의 캐릭터가 이동하며 이동 중 새 지점을 클릭하면 목적지가 바뀐다. 쿼터뷰 카메라는 고정이다.

이동 버튼은 기본 **Left**이며 화면의 **Left / Right / Middle**에서 변경한다. 설정은 PlayerPrefs에 저장되어 재실행 후에도 유지된다. 접속 패널 전체 영역 위 클릭과 게임 창에 포커스를 주는 클릭은 이동 요청을 보내지 않는다. 현재 평지는 10×10이며 중심 좌표 범위 ±4.5, Y=1, 속도 초당 4는 테스트용 값이다. 장애물 길찾기·벌목·카메라 조작은 이번 범위에 포함하지 않는다.

세션 테스트에서는 `-octopus-connect`로 접속을 자동 시작한다. 로그의 `[OctOpus] Roster=` 뒤에 정렬된 연결 ID가 기록된다. 테스트는 서버 1개·클라이언트 2개를 시작하고 양쪽 ID 목록 일치, 강제 종료 전파, 새 프로세스 재접속을 확인한다. 테스트 자신이 시작한 프로세스만 정리한다. 별도 PlayMode 테스트는 같은 클라이언트 인스턴스의 연결 종료·재접속과 실제 네트워크 플레이어의 이동·제거·재생성을 검증한다. 입력 테스트는 버튼 저장·잘못된 설정 복구·패널 클릭 차단·포커스 복귀 시 입력 차단을 확인한다.

`scripts/test-local-session.ps1 -Movement`로 두 클라이언트 이동 통합 검증을 실행한다. WSL에서는 `-ExternalServer -ServerAddress WSL_IPV4`를 함께 사용한다. 이동 통합 검증용으로 한 클라이언트에만 `-octopus-connect -octopus-movement-test`를 전달하고 다른 클라이언트는 `-octopus-connect`로 실행한다. 두 플레이어 생성 후 첫 목적지 방향의 실제 이동·도착 전 목적지 교체·서버 및 렌더링 위치 도착·잘못된 좌표 거부·타인 소유권 요청 거부·정상 이동 복구를 검사하고 성공 시 `[OctOpus] MovementTest=Passed`를 기록한다. `[OctOpus] Position=ID:X,Y,Z`는 0.5초마다 수신한 서버 좌표를 기록하고 `[OctOpus] Render=ID:X,Y,Z`는 실제 렌더링 좌표를 기록한다. 타인 소유권 요청 검사는 FishNet 클라이언트 RPC 스텁의 사전 차단을 확인하며, 조작된 패킷을 전송해 서버의 소유권 검사를 시험한 것은 아니다. 자동 검증 인자 없이 실행한 클라이언트는 자동 이동하지 않는다.

## 검증 결과와 남은 범위
나무 접근·독점 작업 시작과 확인 절차는 [나무 상호작용](tree-interaction.md)을 따른다. 이동 검증 기록은 아래에 보존한다.

2026-09-10 클릭 이동 변경으로 Windows 클라이언트 빌드와 PlayMode 10개 테스트가 통과했다. 입력 정책 9개와 실제 서버 접속·이동 도착·종료 후 제거·같은 클라이언트 인스턴스 재접속 검증을 포함한다. 같은 날 사용자가 지면 클릭 이동·다른 창의 이동 표시·목적지 변경·UI 클릭 차단·Right 버튼 변경·재실행 후 설정 유지까지 모두 정상 동작한다고 확인했다. 실행 파일 두 개의 통합 검증 결과는 서버의 [클릭 이동 기록](../../OctOpus-backend/docs/movement.md)을 따른다.

기존 Windows 접속 화면은 2026-09-10 사용자가 두 창의 상태·ID·캡슐 표시와 접속 종료·재접속이 기대대로 동작한다고 확인했다. WSL Ubuntu 26.04 Linux 서버와 Windows 두 클라이언트의 접속·종료·재접속도 자동 검증을 통과했으며, 사용자가 실행 도우미와 실제 게임 창으로 동일한 기대 결과를 확인했다. Unity 공식 지원 Ubuntu 22.04·24.04 배포 환경과 10명 부하·원격 지연·AWS 검증은 남아 있다. GitHub 문서 CI와 Unity 실행 검증은 구분한다.
