# 나무 상호작용 확인

이 문서는 해당 개발 단계의 검증 기록이다. 현재 맵 크기·나무 수·재접속 확인은 [생활 테스트 맵](test-map.md), 공용 버전은 [실행 안내](unity-setup.md)를 따른다.

2026-09-11 사용자가 아래 수동 확인을 모두 통과했다고 확인했다. 이후 체력·스태미나·타격 변경의 확인 절차는 [벌목 타격](striking.md)을 따른다.

이 단계는 나무 접근과 30초 작업 점유까지다. 도끼 타격·체력·스태미나·목재 지급은 다음 단계다. 서버 판정은 [서버 구현 안내](../../OctOpus-backend/docs/tree-interaction.md)를 따른다. 양쪽 manifest의 공용 패키지 SHA가 같은 서버와 클라이언트를 함께 사용한다.

## 직접 확인
1. 갱신된 전용 서버와 Windows 클라이언트 두 개를 실행하고 같은 서버에 Connect한다. WSL은 서버 실행 도우미가 출력한 주소를 입력한다.
2. 첫 창에서 나무를 클릭한다. 초록 플레이어가 접근한 뒤 Your activity가 Working, Worker가 자신의 ID로 표시되고 30초가 줄어들어야 한다. 패널이 길면 안에서 스크롤한다.
3. 다른 창에서 같은 나무를 클릭한다. 접근 후 Busy가 표시되고 기존 작업자의 시간은 유지되어야 한다.
4. 작업 창에서 나무를 여러 번 눌러도 시간이 30초로 돌아가지 않아야 한다. 지면 클릭은 이동하면서 작업을 취소해야 한다.
5. 다시 시작해 기다리면 30초 뒤 TimedOut이 표시되고 Worker가 Free로 바뀌어야 한다.
6. 작업 중 Disconnect하면 다른 창에서 바로 나무 작업을 시작할 수 있어야 한다. 다시 Connect해도 나무가 중복 생성되면 안 된다.
7. 패널 클릭·스크롤은 이동/작업을 취소하지 않아야 한다. Right 설정 후 오른쪽 클릭으로 이동과 나무 선택이 모두 가능하고 재실행해도 설정이 유지되어야 한다.

Selected tree는 로컬 선택 표시다. Your activity, Last server result, Worker, Server time remaining은 서버에서 받은 결과다. 서버와 통신하는 사이에 선택 표시와 작업 확정 시점은 다를 수 있다.

## 자동 검증
백엔드 Windows 서버와 클라이언트를 [빌드 안내](unity-setup.md)에 따라 먼저 빌드한다. 저장소 루트에서 실행한다.

```powershell
pwsh -NoProfile -File scripts/unity.ps1 -Task Test
pwsh -NoProfile -File scripts/test-tree-session.ps1
pwsh -NoProfile -File scripts/test-local-session.ps1 -Movement
```

WSL 서버를 실행한 상태에서는 나무 검증에 `-ExternalServer -ServerAddress WSL_IPV4`를 추가한다. WSL_IPV4를 실제 출력 주소로 바꾼다. 서버에 다른 사용자가 없는 테스트 세션에서 실행한다. 스크립트는 자신이 실행한 프로세스만 종료하며 로그는 TestResults에 남긴다. 자동 조작은 별도 테스트 실행 인자에서만 활성화된다.

자동 검증은 실제 마우스 조작감이나 화면 배치를 대신하지 않는다. 위 수동 확인은 사용자 확인 전까지 미검증으로 남긴다.

나무 세션 스크립트는 다른 플레이어의 점유 실패, 반복 요청 시간 유지, 만료, 이동 취소, 정상 Disconnect 후 25초 이내 재점유, 늦게 접속한 관찰자의 상태를 확인한다. 같은 클라이언트 인스턴스의 재접속은 PlayMode 테스트에서 나무 1개 → 종료 후 0개 → 재접속 후 새 인스턴스 1개와 동일 서버 나무 ID로 검증한다. 강제 프로세스 종료의 즉시 감지는 이 검사 범위가 아니며 서버가 종료를 감지하기 전에는 30초 작업 만료가 먼저 발생할 수 있다.
