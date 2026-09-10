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
Windows 전용 서버를 실행한 뒤 클라이언트를 실행한다. 기본 주소는 127.0.0.1, 포트는 UDP 7770이다. **Connect**로 접속하고 **Disconnect**로 종료한다. 같은 창에서 다시 Connect를 누르면 기존 네트워크 매니저를 재사용한다.

화면에는 연결 상태, 플레이어 수·ID, 자신의 ID 표시와 플레이어별 임시 캡슐이 나온다. 이것은 접속 확인용 화면이다. 캡슐 위치는 목록 순서에 따른 표시 위치이며 서버의 월드 좌표나 이동 동기화가 아니다. 이동·키 재설정·벌목은 다음 구현 범위다.

세션 테스트에서는 `-octopus-connect`로 접속을 자동 시작한다. 로그의 `[OctOpus] Roster=` 뒤에 정렬된 연결 ID가 기록된다. 테스트는 서버 1개·클라이언트 2개를 시작하고 양쪽 ID 목록 일치, 강제 종료 전파, 새 프로세스 재접속을 확인한다. 테스트 자신이 시작한 프로세스만 정리한다. 별도 PlayMode 테스트는 같은 클라이언트 인스턴스의 연결 종료·재접속과 마커 정리를 검증한다.

## 아직 확인하지 않은 범위
Linux 서버의 실제 Linux 기동·접속, GUI 배치·조작감의 사용자 확인, 10명 부하·원격 지연 환경은 남아 있다. GitHub 문서 CI와 Unity 실행 검증은 구분한다.
