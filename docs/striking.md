# 벌목 타격 확인

이번 단계는 서버가 판정하는 도끼 타격·체력·스태미나다. 목재 지급과 인벤토리는 아직 없다. [서버 규칙과 시제품 값](../../OctOpus-backend/docs/striking.md)을 따른다. 서버와 클라이언트는 같은 공용 패키지 SHA의 빌드를 함께 사용한다.

## 수동 확인
1. 이전 서버·게임 창을 닫고 갱신된 서버와 클라이언트 두 개를 실행한다. 같은 서버 주소로 Connect한다.
2. 나무를 클릭해 Working이 되면 마우스를 월드에 둔 채 Space를 한 번 누른다. 임시 도끼가 움직이고 HP가 감소하며 스태미나가 차감되는지 두 창에서 확인한다.
3. 키를 길게 눌러도 한 번만 타격해야 한다. 빠르게 연타하면 Cooldown이 표시되고 허용 간격보다 많은 피해를 주지 않아야 한다.
4. 스태미나가 부족하면 InsufficientStamina가 표시되고 체력은 그대로여야 한다. 잠시 기다린 뒤 다시 누르면 타격할 수 있어야 한다.
5. 손상된 나무 작업 중 지면을 클릭하면 나무 체력은 회복되고 소모한 스태미나는 즉시 환급되지 않아야 한다. 평상시에는 작업 중보다 빠르게 회복한다.
6. 체력을 0으로 만들면 나무가 사라지고 10초 뒤 같은 자리에 돌아와야 한다. 이번 버전은 목재를 지급하지 않는다.
7. Strike key 버튼을 누르고 R 등 키보드 키를 눌러 바꾼다. 설정하는 키 입력 자체는 타격하지 않아야 한다. 이후 바꾼 키로 타격하고 재실행해 설정 유지도 확인한다. Escape는 키 설정 취소용이다.
8. 패널 클릭·스크롤·키 설정·주소 입력·창 포커스 전환이 타격이나 이동을 일으키지 않아야 한다. 패널이 길면 안에서 스크롤한다.

## 자동 검증
백엔드 Windows 서버를 먼저 빌드하고 클라이언트 저장소 루트에서 실행한다.

```powershell
pwsh -NoProfile -File scripts/unity.ps1 -Task Test
pwsh -NoProfile -File scripts/unity.ps1 -Task BuildWindows
pwsh -NoProfile -File scripts/test-strike-session.ps1
pwsh -NoProfile -File scripts/test-tree-session.ps1
```

WSL 서버를 별도로 켰으면 `-ExternalServer -ServerAddress WSL_IPV4`를 실제 주소로 바꿔 추가한다. 테스트 전용 세션에서 실행하며 스크립트는 자신이 실행한 프로세스만 종료한다. 자동 RPC 검증은 실제 키 조작·애니메이션 품질·화면 배치를 대신하지 않는다.
