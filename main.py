from fastapi import FastAPI
from pydantic import BaseModel
from typing import List, Dict, Optional
from openai import OpenAI
from dotenv import load_dotenv
import os

load_dotenv()
app = FastAPI()
client = OpenAI()

# 내부 상태 저장소
memory: Dict[str, Dict] = {}

# ==== 데이터 모델 ====
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
    investigation: Optional[Dict] = None  # { "target": str, "isMafia": bool }
    savedInfo: Optional[Dict] = None      # { "saved": str }

class VotePayload(BaseModel):
    playerId: str
    history: List[Dict]
    alivePlayers: List[str]

class InvestigationPayload(BaseModel):
    playerId: str
    target: str
    isMafia: bool

# ==== GPT 호출 함수 ====
def ask_gpt(prompt: str) -> str:
    print("🔁 GPT 호출")
    try:
        response = client.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[{"role": "user", "content": prompt}],
            temperature=0.8,
        )
        return response.choices[0].message.content.strip()
    except Exception as e:
        print("❌ GPT 오류:", e)
        return "에러"

# ==== 엔드포인트 ====

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
밤 {payload.day}입니다.

- 마피아: 제거할 대상을 고르세요.
- 의사: 보호할 대상을 고르세요.
- 경찰: 조사할 대상을 고르세요.

선택한 닉네임만 단독으로 출력하세요. 예: ai_3
"""
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

    # 조사 정보가 있을 경우
    investigation_text = ""
    if payload.investigation:
        target = payload.investigation["target"]
        result = "마피아입니다" if payload.investigation["isMafia"] else "마피아가 아닙니다"
        investigation_text = f"""
당신은 경찰이며, 밤에 '{target}'을 조사했습니다. 그 결과는: {target}은 {result}.
이 정보를 낮에 공개하거나 숨기거나, 거짓말할 수 있습니다.
80% 확률로 말하고, 20% 확률로 시민처럼 행동하세요.
"""

    # 의사 구조
    saved_text = ""
    if payload.savedInfo:
        saved_target = payload.savedInfo["saved"]
        saved_text = f"""
당신은 의사이며, 밤에 '{saved_target}'을 보호했습니다.
이 정보를 낮에 공개하거나 숨기거나, 거짓말할 수 있습니다.
80% 확률로 말하고, 20% 확률로 시민처럼 행동하세요.
"""

    prompt = f"""당신은 마피아 게임의 플레이어이며, 역할은 '{role}'입니다.
지금은 낮 {payload.day}이며 자유롭게 대화하고 있습니다.

최근 대화 기록:
{history_text}
{investigation_text}
{saved_text}

당신의 역할에 따라 자연스럽게 행동하세요:

- 경찰: 조사 정보를 말하거나 숨기거나 거짓말할 수 있습니다.
- 의사: 살린 대상을 근거로 행동하거나, 침묵하거나 시민처럼 위장할 수 있습니다.
- 시민: 직감과 대화만으로 추리해야 합니다.
- 마피아: 시민인 척 하며 의심, 방어, 혼란을 유도하세요.

당신의 말은 대화처럼 자연스럽고 약간 길게 구성하세요.
아무 말도 하지 않기로 했다면, 시민처럼 자연스럽게 행동하는 말을 하세요.
"""
    message = ask_gpt(prompt)
    return { "message": message.strip() }

@app.post("/vote-suggestion")
def vote_suggestion(payload: VotePayload):
    history_text = "\n".join(
        f"{item['sender']}: {item['message']}" for item in payload.history[-10:]
    )
    prompt = f"""당신은 마피아 게임 참가자입니다.
지금은 낮이며, 투표를 통해 마피아를 찾아야 합니다.
현재 살아있는 플레이어: {', '.join(payload.alivePlayers)}
최근 대화 기록:
{history_text}

이 중 당신이 투표할 플레이어의 닉네임 하나만 출력하세요."""
    target = ask_gpt(prompt)
    return { "target": target.strip() }

@app.post("/investigation-result")
def investigation_result(payload: InvestigationPayload):
    result_text = "마피아입니다" if payload.isMafia else "마피아가 아닙니다"
    prompt = f"""당신은 마피아 게임에서 경찰 역할입니다.
밤에 '{payload.target}'을 조사한 결과, 그 사람은 {result_text}.
낮에 이 사실을 공개하거나 숨기거나, 거짓말할 수 있습니다.

당신이 말할 한 문장을 생성하세요."""
    message = ask_gpt(prompt)
    return { "message": message.strip() }
