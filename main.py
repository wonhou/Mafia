from fastapi import FastAPI
from pydantic import BaseModel
from typing import List, Dict
from openai import OpenAI
from dotenv import load_dotenv
import os

load_dotenv()  # ✅ 환경변수 로딩

app = FastAPI()
client = OpenAI()  # ✅ API 키는 자동으로 .env에서 불러옴

# 내부 메모리 저장소
memory: Dict[str, Dict] = {}

# 데이터 모델
class Player(BaseModel):
    id: str
    isAI: bool

class InitPayload(BaseModel):
    playerId: str
    role: str
    allPlayers: List[Player]
    settings: Dict

class NightActionPayload(BaseModel):
    playerId: str
    role: str
    alivePlayers: List[str]
    day: int

class ChatPayload(BaseModel):
    playerId: str
    history: List[Dict]  # [{ sender, message }]
    day: int

# GPT 호출 함수
def ask_gpt(prompt: str) -> str:
    print("GPT 호출 시작")  # 요청 직전 로그

    try:
        response = client.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[{"role": "user", "content": prompt}],
            temperature=0.8,
        )
        print("GPT 응답 수신")  # 응답 직후 로그
        return response.choices[0].message.content.strip()

    except Exception as e:
        print("GPT 호출 중 오류 발생:", e)
        return "에러"

@app.post("/init")
def init(payload: InitPayload):
    memory[payload.playerId] = {
        "role": payload.role,
        "allPlayers": payload.allPlayers,
        "settings": payload.settings
    }
    print(f"✅ {payload.playerId} 초기화 완료")
    return {"status": "ok"}

@app.post("/night-action")
def night_action(payload: NightActionPayload):
    role = payload.role
    target_list = ", ".join(payload.alivePlayers)
    prompt = f"""당신은 마피아 게임에서 '{role}' 역할입니다.
현재 살아있는 플레이어: {target_list}
밤 {payload.day}입니다. 당신이 이번 턴에 할 행동의 대상을 한 명 선택하세요.
단순히 이름만 응답하세요. 예: ai_3"""

    target = ask_gpt(prompt)
    action = {
        "mafia": "kill",
        "doctor": "save",
        "police": "investigate"
    }.get(role, "none")

    return { "action": action, "target": target }

@app.post("/chat-request")
def chat_request(payload: ChatPayload):
    history_text = "\n".join(
        f"{item['sender']}: {item['message']}" for item in payload.history[-10:]
    )
    role = memory.get(payload.playerId, {}).get("role", "citizen")

    prompt = f"""당신은 마피아 게임에서 '{role}' 역할입니다.
지금은 낮 {payload.day}이며 모두가 자유롭게 대화하고 있습니다.
최근 대화 기록은 다음과 같습니다:

{history_text}

이 상황에서 당신이 말할 수 있는 한 마디를 생성하세요."""

    message = ask_gpt(prompt)
    return { "message": message }
