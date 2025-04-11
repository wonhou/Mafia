## 🧠 Branch 전략 및 협업 가이드

마피아 게임 프로젝트에서는 다음과 같은 브랜치 전략을 따릅니다.

---

###  브랜치 역할 정리

| 브랜치 이름 | 용도 | 설명 |
|-------------|------|------|
| `master`    |  배포용 메인 브랜치 | 항상 안정된 버전 유지. 실제 서비스용 코드 |
| `develop`   |  통합 개발 브랜치 | 모든 기능 브랜치를 통합하고 테스트하는 브랜치 |
| `llm-A`     |  AI 담당 작업 브랜치 | AI/LLM 관련 기능 전용 작업 공간 |
| `server-A`  |  서버 기능 개발 | 서버 전용 기능을 개발하는 브랜치 |
| `unity-A`   |  Unity 클라이언트 개발 | 클라이언트(UI/UX 등) 전용 브랜치 |

각 브랜치는 역할에 맞게 기능별 하위 브랜치를 만들 수 있습니다.
예: `feature/night-vote`, `bugfix/ws-connection`, `hotfix/login-error` 등

---

###  브랜치 흐름도

         feature/xxx
             ↓
          develop ← llm-A
         ↙       ↘
     server-A     unity-A
               ↓
         master (배포)

---

###  작업 흐름 요약

1. `develop` 브랜치에서 `feature/기능명` 브랜치 생성
2. 기능 작업 완료 후 PR(Pull Request) 생성
3. 코드 리뷰 후 `develop` 브랜치로 merge
4. 버전 안정화되면 `master`로 최종 merge 및 배포

---

###  협업 팁

- PR 작성 시 반드시 작업 내용과 테스트 여부를 작성해 주세요.
- 커밋 메시지는 명확하고 간결하게 작성합니다.  
  예: `feat: 밤 투표 기능 추가`, `fix: WebSocket 연결 오류 수정`
- 기능 분배와 진행 상황 관리는 GitHub Issues 또는 Project Board를 활용합니다.

---

