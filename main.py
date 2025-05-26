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
            temperature=0.7,
            max_tokens=100,  # 💬 적당히 말 길이 조절
            top_p=1.0,
            frequency_penalty=0.5,
            presence_penalty=0.6
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

- 마피아: 제거할 대상을 고르세요. 현재 살아있는 플레이어만 죽여야 합니다.
- 의사: 보호할 대상을 고르세요. 현재 살아있는 플레이어만 보호할 수 있습니다
- 경찰: 조사할 대상을 고르세요. 현재 살아있는 플레이어만 조사할 수 있습니다.

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

    personality = {
        "ai_1": "논리적이며 토론을 좋아하고, 감정을 잘 드러내지 않는 분석가 타입",
        "ai_2": "말수는 적지만 날카로운 관찰력을 지닌 신중한 전략가",
        "ai_3": "모든 상황에 끼어드는 수다쟁이이자 분위기 메이커, 질문을 자주 던짐",
        "ai_4": "누구든 의심하고 공격적으로 몰아붙이는 직설적이고 불신 가득한 성격",
        "ai_5": "사람의 감정을 잘 읽고 공감을 자주 표현하는 다정한 감성가",
        "ai_6": "상황에 진지하게 임하지 않고 농담과 장난을 섞어 말하는 장난꾸러기",
        "ai_7": "감정이 없고 차갑게 말하며, 판단을 빠르게 내리는 냉정한 결정자"
    }.get(payload.playerId, "특징 없는 평범한 플레이어")

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

    prompt = f"""
[마피아 게임 설명]
플레이어는 '마피아', '시민', '경찰', '의사' 역할 중 하나입니다.
밤에는 마피아는 제거할 사람을, 경찰은 조사할 사람을, 의사는 보호할 사람을 고릅니다.
낮에는 모두가 토론하며 마피아를 찾아 투표로 처형합니다.
마피아는 정체를 숨기고 시민처럼 행동하거나, 경찰/의사인 척 하며 거짓말을 할 수 있습니다.
    
당신은 마피아 게임의 플레이어이며, 역할은 '{role}'입니다.
당신의 성격은 '{personality}'입니다. 말투와 표현 방식에 반영하세요.
지금은 낮 {payload.day}이며 자유롭게 대화하고 있습니다.

최근 대화 기록:
{history_text}
{investigation_text}
{saved_text}

당신의 역할에 따라 자연스럽게 행동하세요:

- 경찰: 조사 정보를 말하거나 숨기거나 거짓말할 수 있습니다.
- 의사: 살린 대상을 말하거나 숨기거나 시민인 척 할 수 있습니다.
- 시민: 추리와 직감을 바탕으로 대화에 참여하세요.
- 마피아: 정체를 숨기고 시민처럼 행동하거나, 경찰/의사인 척 하며 다른 사람을 의심하게 만들 수 있습니다. 예: "나는 경찰인데 ai_2를 조사했더니 마피아였다" (거짓말 가능)

각 플레이어는 자신의 성격에 따라 발언의 초점이 다릅니다.
논리적인 AI는 증거 중심으로, 감성적인 AI는 분위기와 느낌으로, 공격적인 AI는 특정 인물을 강하게 몰아세웁니다.
성격에 맞게, 같은 내용을 다르게 표현하려고 하세요.

같은 사실이라도 성격에 따라 다르게 표현하려고 하세요.
가능하면 다른 플레이어가 말한 표현을 그대로 반복하지 말고, 당신만의 방식으로 이야기하세요.
중복된 문장은 피하고, 유사한 의미라도 새롭게 표현하세요.

당신의 말은 사람처럼 자연스럽고, 짧고 단순한 문장으로 구성하세요.
보통 사람의 대화처럼 한두 문장 내외로 말하는 게 좋습니다.
말은 너무 장황하지 않도록 주의하세요.
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
현재 살아있는 플레이어에게만 투표할 수 있습니다.
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
