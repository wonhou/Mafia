## 🧠 GPT / AI 연동

### 🤖 사용 모델

- **모델명**: GPT-4 (OpenAI)
- **API 타입**: Chat Completion API
- **모델 목적**: 마피아 게임 내 AI 플레이어가 역할에 따라 상황에 맞는 대화 생성

---

### 🔗 연동 방식

- **게임 서버**: Node.js
- **AI 서버**: FastAPI
- **통신 방식**: HTTP POST
- **API Endpoint**: `/chat_request`

```json
#요청
POST /chat_request
{
  "player_id": "ai_01",
  "role": "mafia",
  "history": [
    {"sender": "user", "message": "오늘 누구 죽일까?"},
    {"sender": "ai_02", "message": "나는 시민 같아 보이는데?"}
  ],
  "turn": "night"
}

#응답
{
  "response": "이번에는 player_04를 제거하는 게 좋겠어."
}
```
