# 생활 테스트 맵 확인

기존 서버와 게임 창을 종료한 뒤 갱신된 서버와 Windows 클라이언트 두 개를 실행한다. WSL 서버 도우미가 출력한 주소를 입력해 Connect한다. 양쪽 공용 패키지 버전이 같아야 한다. [서버 맵 범위](../../OctOpus-backend/docs/test-map.md)를 따른다.

## 직접 확인
1. 출발 공터에서 플레이어가 보이고, 길·안내 자리·나무 5그루를 구분할 수 있어야 한다. 왼쪽 UI는 유지되며 오른쪽에 맵이 보인다.
2. 맵의 여러 끝 지점을 클릭해 이동한다. 지면 가장자리보다 밖으로 나가면 안 된다. 길 바깥 평지 이동도 가능하다.
3. 두 창에서 서로 다른 나무를 선택하면 둘 다 Working이어야 한다. 각각 Space로 타격하면 선택한 나무만 체력이 줄어야 한다.
4. 한쪽이 지면을 클릭해 취소해도 다른 나무 작업은 계속돼야 한다.
5. 한 나무를 파괴하면 그 나무만 사라지고 10초 뒤 같은 위치에 돌아와야 한다.
6. 같은 나무를 선택하면 기존과 같이 한 사람만 작업하며 다른 사람은 Busy여야 한다.
7. Disconnect 후 다시 연결해도 나무가 정확히 5그루이며 중복되지 않아야 한다.

안내 자리는 NPC 위치 표식이며 아직 퀘스트 기능은 없다. 도끼/타격 키와 스태미나의 상세 확인은 [벌목 타격](striking.md)을 따른다. 실제 장애물 우회·목재 지급·생활 레벨 기능은 다음 단계다.

## 자동 검증
서버 Windows 빌드 후 클라이언트 저장소 루트에서 실행한다.

```powershell
pwsh -NoProfile -File scripts/unity.ps1 -Task Test
pwsh -NoProfile -File scripts/unity.ps1 -Task BuildWindows
pwsh -NoProfile -File scripts/test-map-session.ps1
pwsh -NoProfile -File scripts/test-strike-session.ps1
pwsh -NoProfile -File scripts/test-tree-session.ps1
pwsh -NoProfile -File scripts/test-local-session.ps1 -Movement
```

WSL에서 실행 중인 테스트 서버에는 `-ExternalServer -ServerAddress WSL_IPV4`를 실제 주소로 바꿔 추가한다. 테스트 로그는 TestResults에 남는다. 순수 규칙·RPC 자동 검증과 실제 마우스 조작/화면 검증을 구분한다.

## 검증 기록 (2026-09-11)
- 공용 패키지 `0.5.0`, Git SHA `a9dbc6af81b45890a7e01e366b6ac1716e2cee23`를 양쪽 manifest와 lock에 고정했다. 태그는 `shared-v0.5.0-test-map`이다.
- 고정 버전으로 서버 PlayMode 34개·클라이언트 PlayMode 21개, Windows 서버/클라이언트와 Linux 전용 서버 빌드가 통과했다.
- Windows 서버 및 WSL Ubuntu 26.04 Linux 서버에 Windows 클라이언트 두 개를 연결한 맵 테스트가 통과했다. 공용 배치/식별자 일치, 스폰, 이동 경계, 서로 다른 나무 점유·타격·취소·고갈·재생성을 확인했다.
- Unity Editor에서 생성한 배치 미리보기로 지면·길·표식·나무 5그루의 화면 구성을 확인했다. 게임의 실제 마우스 조작감과 시각적 표현에 대한 사용자 확인은 남아 있다.
- 구현과 분리된 코드 리뷰에서 지적 없음. 10명 부하·인터넷 지연·공식 지원 Ubuntu 배포판·AWS는 미검증이다.
- 고정 버전 Windows 회귀 검증에서 타격·스태미나·재생성·시간 초과, 나무 독점/종료/늦은 접속, 이동 목적지 변경·잘못된 요청 거절을 통과했다. WSL에서도 타격·스태미나·재생성·시간 초과를 통과했다.
