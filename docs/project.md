# OctOpus 클라이언트 안내

## 기준 문서
전체 요구사항을 이 저장소에 복사하지 않는다. 원본은 `OctOpus-backend` 저장소에서 관리한다.

| 내용 | 나란히 클론한 로컬 환경의 링크 | 백엔드 저장소 내 경로 |
|---|---|---|
| 제품 요구사항 | [요구사항](../../OctOpus-backend/docs/product/requirements.md) | `docs/product/requirements.md` |
| 단계별 개발 | [개발 단계](../../OctOpus-backend/docs/roadmap.md) | `docs/roadmap.md` |
| 구조·공용 코드 | [아키텍처](../../OctOpus-backend/docs/architecture.md) | `docs/architecture.md` |
| 고정 버전·설치 모듈 | [기술 선정](../../OctOpus-backend/docs/technology-selection.md) | `docs/technology-selection.md` |

이 상대 링크는 두 저장소가 같은 부모 디렉터리에 있을 때 유효하다. GitHub에서는 백엔드 저장소의 명시된 경로를 직접 연다. 다른 위치에 체크아웃했으면 실제 백엔드 위치를 확인하고 읽는다.

## 현재 방향
PC 설치형 3D 쿼터뷰, 최대 10명, 생활직업부터 개발한다. 첫 목표는 접속·클릭 이동·벌목·목재 저장·나무꾼 전직까지다. Unity 6.3 LTS + C#, FishNet, Windows 클라이언트·Linux 전용 서버, 백엔드 소유 공용 Unity 패키지를 확정했다. 정확한 Editor·FishNet 버전과 설치 모듈은 위 기술 선정 문서를 따른다. Windows 로컬 초기 구성과 접속을 검증했으며 자세한 결과는 [초기 구성 안내](unity-setup.md)를 따른다.

클라이언트는 입력과 표현을 맡는다. 서버는 게임 규칙과 아이템 변경을 최종 판정한다. 프런트엔드·백엔드라는 저장소 이름이 웹 앱 구조를 의미하지 않는다.

## 현재 파일 범위
나무 클릭·접근과 서버 작업 점유를 추가했다. 타격·보상은 후속 단계이며 [확인 절차](tree-interaction.md)를 따른다.

`AGENTS.md`는 짧은 공통 작업 규칙, `docs/agent-routing.md`는 작업별 문서 진입점, `docs/tasks/`는 유형별 구현·검증 안내다. `Unity`에는 최소 접속 장면·코드가 있고 `scripts/unity.ps1`로 빌드·테스트한다.

## 첫 설정 작업에서 추가할 사항
- 선정한 버전·대상 OS를 프로젝트 설정에 반영하고 실제 프로젝트 경로와 공용 패키지 SHA를 기록.
- 실행이 확인된 로컬 실행·빌드·테스트 명령.
- 최소 장면, 자산 관리·Git 제외 규칙, 키 설정 및 네트워크 기반.
- 로컬 전용 서버와 클라이언트 2개를 연결하는 검증 절차.
