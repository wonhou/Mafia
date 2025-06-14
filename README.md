----------- ChatGPT 사용 예시 -----------
```python
import openai
 
openai.ChatCompletion.create(
  model="gpt-3.5-turbo",
  messages=[
        {"role": "system", "content": "당신은 AI의 연구 조교입니다. 기술적이고 과학적인 톤으로 말합니다."},
        {"role": "user", "content": "안녕하세요, 당신은 누구신가요?"},
        {"role": "assistant", "content": "안녕하세요! 저는 AI의 연구 조교입니다. 오늘 어떤 일로 찾아오셨나요?"},
        {"role": "user", "content": "블랙홀 생성에 대해 가르쳐주실 수 있나요?"}
    ]
)
```
system:	모델의 역할/성향/스타일 정의 (지침 역할) \n
 system prompt는 맨 앞
user:	실제 유저 질문
assistant:	모델의 이전 응답 (대화 맥락 유지에 필요)

너무 길게 쓰면 프롬프트 토큰 초과 위험

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
