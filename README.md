# 마피아 게임 (Unity + Node.js)

멀티플레이 마피아 게임 프로젝트입니다.  
클라이언트는 Unity로 구성되어 있으며, 서버는 Node.js와 WebSocket을 사용합니다.

---

## 1. 클라이언트: Unity

### ✅ 닉네임 입력

- 스크립트: `Login.cs`
- 랜덤 닉네임 생성 및 static 저장 (`Login.nickname`)
- OK 버튼 클릭 시 로비 화면으로 전환

### ✅ 서버 연결

- 스크립트: `GameStarter.cs`
- 유니티 실행 시 WebSocket을 통해 `ws://localhost:3000`에 연결
- 연결되면 `register` 메시지를 서버로 전송

```json
{ "type": "register", "playerId": "닉네임" }
```

### ✅ 게임 시작

- "start_game" 버튼 클릭 시 서버로 아래 메시지 전송

```json
{ "type": "start_game" }
```

### ✅ 서버 메시지 수신

서버에서 수신하는 주요 메시지:

- `your_role`: 역할 정보 수신
- `night_result`: 밤 턴 결과
- `day_start`: 낮 시작 알림
- `start_vote`: 투표 시작
- `vote_result`: 투표 결과
- `game_over`: 게임 종료 및 승리 정보

### ✅ 역할 UI 출력 (구현 중)

- 각 역할별 행동 패널:
  - `MafiaPanel`
  - `DoctorPanel`
  - `PolicePanel`

---

## 2. 서버: Node.js (`server.js`)

### ✅ WebSocket 서버

- 클라이언트(`Unity`, `vote-client.js`) 연결 수락
- `"register"` 메시지 수신 시 유저 등록
- `"start_game"` 수신 시 게임 시작

### ✅ 게임 진행 흐름

1. 역할 분배 → 각 유저에게 `your_role` 전송
2. 밤 턴 시작 → AI 및 유저 행동 처리
3. 낮 턴 → 투표 시작 → 투표 결과 계산
4. 승리 조건 만족 시 `game_over` 메시지 전송

---

## 3. AI 서버: `ai-server.js`

### ✅ 동작 방식

- 클라이언트(서버)로부터 행동 요청 수신
- 현재는 역할과 관계없이 무조건 랜덤 행동 선택
- 향후 역할별 로직 추가 예정

---

## 4. vote-client.js

### ✅ 기능

- Node.js 기반 콘솔 테스트 클라이언트
- `register`, `vote` 메시지 수동 전송
- `"start_vote"` 수신 시 사용자 입력 유도

---

## 앞으로의 계획

- 유저 역할에 따라 행동 선택 UI 구성
- AI 행동 고도화 (역할 기반 판단)
- 로비/방 시스템 UI 구현
- 게임 재시작 및 나가기 기능 추가
