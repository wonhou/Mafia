from fastapi import FastAPI
from pydantic import BaseModel
from typing import List, Dict, Optional
from openai import OpenAI
from dotenv import load_dotenv
import os
from datetime import datetime
from fastapi import FastAPI
from typing import Dict
import re
import random

load_dotenv()
app = FastAPI()
client = OpenAI()

# 내부 상태 저장소
#memory: Dict[str, Dict] = {}
memory: Dict[str, Dict[str, Dict]] = {} #memory[roomId][playerId] memory[roomId][chat_history]

# 규칙 선언 강민우
COMMON_RULES = """마피아 게임의 규칙을 인식하고 준수하세요.

게임은 최대 8명이 참가하며, 아래의 규칙을 참고하세요.

- **플레이어 구성:**
  - 2명은 마피아
  - 4명은 시민
  - 1명은 의사
  - 1명은 경찰

- **팀 구성:**
  - 시민과 의사, 경찰은 시민 팀
  - 마피아 2명은 마피아 팀

- **승리 조건:**
  - 마피아 팀은 마피아와 시민의 수가 같아지면 승리합니다.
  - 시민 팀은 모든 마피아를 처치하면 승리합니다.

- **게임 진행:**
  - **밤:** 마피아 팀은 한 명을 처형하고, 의사는 마피아의 공격으로부터 한 명을 보호합니다. 경찰은 마피아로 의심되는 한 명의 직업을 조사합니다.
    - 마피아가 원하는 처형 대상이 다를 경우, 승리 확률을 계산하여 더 높은 쪽을 선택합니다.
    - 만약 살아있는 마피아가 처형 대상을 고르지 않을 경우, 처형이 일어나지 않습니다.
    - 의사가 보호한 대상이 마피아의 공격 대상과 같으면 생존합니다.
    - 경찰은 자기 자신을 조사할 수 없습니다.
  - **낮:** 모든 참가자가 마피아를 찾아 처형하기 위해 토론합니다. 이 과정은 승부가 날 때까지 반복됩니다.

- **추가 조건:**
  - 밤에는 마피아만 발언할 수 있습니다.

# Output Format

플레이어는 이 규칙을 이해하고 충실히 따라야 하며, 각 역할은 프로토콜에 맞춰 행동해야 합니다. 게임의 진행 상황과 역할 수행은 명확하게 설명되어야 합니다.

# Notes

마피아 게임은 팀 간의 전략과 심리전을 요구하므로, 승리 확률 계산과 팀원 간의 협력이 중요합니다. 규칙을 준수하면서 창의적인 전략을 논의하고 실행하세요.
마피아 팀은 마피아 팀의 승리를 목표로하고, 시민팀은 시민팀의 승리를 목표로합니다."""


# 캐릭터 성격 (7타입) 강민우
AI_PERSONALITIES = {
    "ai_1": "논리적이며 토론을 좋아하고, 감정을 잘 드러내지 않는 분석가 타입.",
    "ai_2": "말수는 적지만 날카로운 관찰력을 지닌 신중한 전략가.",
    "ai_3": "모든 상황에 끼어드는 수다쟁이이자 분위기 메이커. 질문을 자주 던진다.",
    "ai_4": "누구든 의심하고 공격적으로 몰아붙이는 직설적인 성격.",
    "ai_5": "공감을 자주 표현하고 사람의 감정을 잘 읽는 다정한 감성가.",
    "ai_6": "상황을 진지하게 임하지 않고 농담을 섞는 장난꾸러기.",
    "ai_7": "감정 없는 차가운 판단자. 논리적이고 냉정한 판단을 내린다.",
}

# ==== 데이터 모델 ====
class Player(BaseModel):
    id: str
    isAI: bool
    role: str

class InitPayload(BaseModel):
    roomId: str
    playerId: str
    role: str
    allPlayers: List[Player] # 모든 플레이어
    settings: Dict

class NightActionPayload(BaseModel):
    roomId: str
    playerId: str
    role: str
    alivePlayers: List[str] # 살아있는 플레이어
    day: int # 날짜

class ChatPayload(BaseModel):
    roomId: str
    playerId: str
    history: List[Dict]  # [{ sender, message }]
    day: int
    investigation: Optional[Dict] = None  # { "target": str, "isMafia": bool }
    savedInfo: Optional[Dict] = None      # { "saved": str }
    alivePlayers: List[str] = []

class VotePayload(BaseModel):
    roomId: str
    playerId: str
    history: List[Dict]
    alivePlayers: List[str]

class InvestigationPayload(BaseModel):
    roomId: str
    playerId: str
    target: str
    isMafia: bool

# system_prompt 만드는 함수
# player_id -> id + rule + id별 성격 + role -> system_prompt
def get_system_prompt(room_id: str, player_id: str) -> str:
    personality = AI_PERSONALITIES.get(player_id, "특징 없는 평범한 플레이어.")
    role = memory.get(room_id, {}).get(player_id, {}).get("role", "citizen")
    mafia_ids = memory.get(room_id, {}).get(player_id, {}).get("mafiaIds", [])

    mafia_info = ""
    if role == "mafia" and mafia_ids:
        mafia_info = f"""
\n\n 당신은 마피아이며, 같은 팀의 동료는 다음과 같습니다:
{', '.join(mafia_ids)}

- 이들과 협력하여 시민을 속이고 처치하는 것이 목표입니다.
- 낮에는 이 사실을 숨기고 행동하세요.
- 밤에는 서로를 절대 의심하지 말고, 시민 중 누가 경찰이나 의사인지 추측하며 전략을 짜세요.
- 동료 마피아를 공격하거나, 제거 대상으로 언급하지 마세요. 중요!
"""
    return f"""{COMMON_RULES}

너의 이름(ID)은 {player_id}이니까 반드시 기억해야돼.

너의 성격은 다음과 같아:
{personality}

당신은 마피아 게임에서 '{role}' 역할입니다. 반드시 기억하세요. {mafia_info}

주의: 상황에 따라 말하지 않거나 할 수 있습니다. 하지만 본인이 불렸을 땐 최대한 대답하십시오.
- 말하기를 원치 않으면 "..."을 출력하세요.

대화 말투나 길이:
- 당신은 짧고 간결한 발언을 선호합니다. 말이 너무 길면 의심을 받을 수 있습니다.
- 말투는 대한민국의 20대 서울사람처럼 자연스럽게 대화를 해야됩니다.
- 같은 단어의 반복은 줄이고, 단어의 수준을 인터넷 커뮤니티에 자주 나오는 단어들을 위주로 대화합니다.
- 1~3문장으로 말하세요.
"""

# ==== GPT 호출 함수 ====
def ask_gpt(prompt: str, system_prompt: str = "") -> str:
    print("🔁 GPT 호출")
    try:
        response = client.chat.completions.create(
            model="gpt-4.1",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": prompt}
            ],
            temperature=0.9,
            max_tokens=120,  # 💬 적당히 말 길이 조절
            top_p=1.0,
            frequency_penalty=0.7,
            presence_penalty=0.3
        )
        
        return response.choices[0].message.content.strip()
    except Exception as e:
        print("❌ GPT 오류:", e)
        return "에러"

def save_chat(room_id: str, sender: str, message: str):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    entry = {"sender": sender, "message": message, "timestamp": timestamp}
    memory.setdefault(room_id, {}).setdefault("chat_history", []).append(entry)

# ==== 엔드포인트 ====

@app.post("/init")
def init(payload: InitPayload):
    # 동료 마피아 리스트 추출
    mafia_ids = [
        p.id for p in payload.allPlayers
        if p.role == "mafia" and p.id != payload.playerId
    ] if payload.role == "mafia" else []

    # memory에 해당 roomId가 존재할 경우 전체 초기화
    memory[payload.roomId] = {}  # 방 전체 초기화 (모든 player 데이터 삭제)

    # 기존 memory 구조 + mafiaIds 추가
    memory.setdefault(payload.roomId, {})[payload.playerId] = {
        "role": payload.role,
        "allPlayers": payload.allPlayers,
        "settings": payload.settings,
        "mafiaIds": mafia_ids
    }
    print(f"✅ {payload.roomId}/{payload.playerId} 초기화 완료")
    return {"status": "ok"}

@app.post("/night-action")
def night_action(payload: NightActionPayload):
    role = payload.role
    target_list = ", ".join(payload.alivePlayers)

    mafia_ids = memory.get(payload.roomId, {}).get(payload.playerId, {}).get("mafiaIds", [])

    self_history = memory.get(payload.roomId, {}).get(payload.playerId, {}).get("self_chat", [])
    self_text = "\n".join(
        f"({entry['day']}일차 {entry.get('timestamp', '시간없음')}) {entry['message']}" for entry in self_history
    )
    chat_history = memory.get(payload.roomId, {}).get("chat_history", [])
    mafia_chat = memory.get(payload.roomId, {}).get(payload.playerId, {}).get("mafiaChat", [])

    # 둘을 섞어서 prompt 구성
    history_text = (
        "낮 대화 요약:\n" +
        "\n".join(f"{msg.get('timestamp', '시간없음')} | {msg['sender']}: {msg['message']}" for msg in chat_history) +
        "\n\n 마피아끼리 대화:\n" +
        "\n".join(f"{msg.get('timestamp', '시간없음')} | {msg['sender']}: {msg['message']}" for msg in mafia_chat)
    )

    #강민우
    system_prompt = get_system_prompt(payload.roomId, payload.playerId)
    prompt = ""

    if role == "mafia":
        prompt = f"""현재 살아있는 플레이어: {target_list}
당신은 마피아 팀의 일원이며, 마피아 팀은 {', '.join(mafia_ids)} 입니다.
오늘은 밤 {payload.day}번째 입니다.

모든 대화: {history_text}
자신이 한 발언: {self_text}

- 자신이 한 발언을 통해 제거 대상을 고르세요.
- 팀과 협의하여 하나의 공통된 제거대상을 고르세요.
- 절대로 팀원과 자신을 제거하려하지 마세요.

닉네임만 단독으로 출력하세요. 예: ai_3
"""
    elif role == "police":
        prompt = f"""현재 살아있는 플레이어: {target_list}
당신은 경찰이며, 밤 {payload.day}에 마피아로 의심되는 인물을 조사할 수 있습니다.

- 당신의 목표는 마피아를 정확히 식별하여 시민팀이 승리하도록 돕는 것입니다.
- 의사와 마피아도 밤에 행동하므로, 당신이 누구를 조사하는지는 매우 중요합니다.
- 마피아는 낮에 시민처럼 행동하기 때문에, 낮의 발언이나 투표 행동을 근거로 의심 가는 인물을 판단하세요.
- 최근 낮 토론에서 발언이 많았던 인물, 과도하게 방어적이었던 인물, 혹은 비논리적이었던 인물은 마피아일 가능성이 있습니다.
- 이미 시민으로 확인한 인물을 반복해서 조사하지 마세요.
- 자신의 신변이 위험하다고 판단되면, 신뢰할 수 없는 인물을 우선 조사하여 정보를 확보하세요.

당신이 조사할 대상의 닉네임만 단독으로 출력하세요. 예: ai_5
"""
    elif role == "doctor":
        prompt = f"""현재 살아있는 플레이어: {target_list}
당신은 의사이며, 밤 {payload.day}에 한 명을 보호할 수 있습니다.

- 당신의 목표는 시민팀을 최대한 생존시키는 것입니다.
- 마피아는 매 턴 한 명을 제거하려고 하며, 당신의 보호는 생사를 좌우합니다.
- 시민처럼 보이며 발언력이 큰 인물, 경찰일 가능성이 있는 조심스러운 인물, 혹은 마피아에게 위협이 될 수 있는 인물을 우선적으로 보호하세요.
- 본인을 보호할 수도 있지만, 매번 자신을 보호하는 것은 비효율적입니다.
- 마피아로 의심되는 인물은 보호 대상에서 제외하세요.
- 낮 토론에서 논리적으로 시민팀에 도움을 준 인물을 보호하면 시민팀의 전략 유지에 유리합니다.

당신이 보호할 대상의 닉네임만 단독으로 출력하세요. 예: ai_5
"""
        
    #강민우
    target = ask_gpt(prompt, system_prompt)
    #target = ask_gpt(prompt)

    action = {
        "mafia": "kill",
        "doctor": "save",
        "police": "investigate"
    }.get(role, "none")
    return { "action": action, "target": target }

@app.post("/chat-request")
def chat_request(payload: ChatPayload):
    history_text = "\n".join(
        f"{item.get('timestamp', '시간없음')} | {item['sender']}: {item['message']}" for item in payload.history
    )
    self_history = memory.get(payload.roomId, {}).get(payload.playerId, {}).get("self_chat", [])
    self_text = "\n".join(
        f"({entry['day']}일차 {entry.get('timestamp', '시간없음')}) {entry['message']}" for entry in self_history
    )
    #role = memory.get(payload.roomId, {}).get(payload.playerId, {}).get("role", "citizen")

    # 강민우
    system_prompt = get_system_prompt(payload.roomId, payload.playerId)
    prompt = f"""지금은 낮 {payload.day}이며 자유롭게 대화 할 수 있습니다."""
    #personality = AI_PERSONALITIES.get(payload.playerId, "특징 없는 평범한 플레이어")

    # 조사 정보가 있을 경우
    investigation_text = ""
    if payload.investigation:
        target = payload.investigation["target"]
        is_mafia = payload.investigation["isMafia"]
        result = "마피아입니다" if is_mafia else "마피아가 아닙니다"

        investigation_text = f"""
당신은 경찰이며, 밤에 '{target}'을 조사했습니다. 그 결과는: {target}은 {result}.

- 조사 결과는 다음과 같이 해석됩니다:
    - 만약 해당 인물이 시민이라면, "시민"으로 알려줍니다.
    - 만약 해당 인물이 마피아라면, "마피아"로 알려줍니다.

- 조사 결과를 시민팀의 승리를 위해 활용하세요:
    - 만약 조사 결과가 "마피아"라면, 그 인물이 실제 마피아임을 확신하고, 낮 토론과 투표에서 그 사람을 지목하세요.
    - 만약 조사 결과가 "시민"이라면, 그 인물에 대한 의심을 줄이고 다른 참가자를 조사하거나, 토론에서 주장에 집중하세요.
    - 낮 토론 시간에는 당신의 조사 결과를 바탕으로 논리적이고 설득력 있게 발언하세요. 필요하다면 과거 발언이나 행동과 연결 지어 상대를 설득하십시오.
    - 당신이 이미 조사한 인물과 다른 인물의 행동을 비교하여 의심의 방향을 조정하십시오.
    - 조사 과정과 결과는 정직하게 보고하며, 절대로 거짓 정보를 제공하지 마세요.

**주의** 낮시간 되면 무조건 빠르게 자신이 경찰임을 밝히면서 자신이 조사한 대상과 그 결과를 채팅에 알려야합니다.
예: "100, 나는 경찰이고 오늘 밤 조사의 대상은 {target}이며, 그 결과는 {result}이야."

이 지침을 따르면서, 밤에 조사한 정보를 바탕으로 시민팀이 승리할 수 있도록 전략적으로 행동하세요.
"""
        prompt += ("\n\n" + investigation_text)

    # 의사 구조
    saved_text = ""
    if payload.savedInfo:
        saved_target = payload.savedInfo["saved"]

        saved_text = f"""
당신은 의사이며, 밤에 '{saved_target}'을 보호했습니다.

- 보호 규칙은 다음과 같습니다:
    - 만약 마피아가 '{saved_target}'을 공격했다면, 당신의 보호 덕분에 그 인물은 생존합니다.
    - 마피아가 다른 인물을 공격했다면, 당신의 보호는 효과가 없습니다.

- 당신의 목적은 시민팀의 생존을 최대화하고, 핵심 역할(경찰 등)의 생존을 우선시하는 것입니다.

- 밤 행동은 다음 원칙에 따라 결정하세요:
    - 이미 마피아로 의심되는 인물은 보호하지 마세요.
    - 경찰처럼 시민팀에 도움이 되는 인물을 보호하는 것을 우선 고려하세요.
    - 당신 자신도 공격 대상이 될 수 있으므로, 위협을 느낀다면 자기 보호도 가능합니다. 단, 매 턴 자기 보호는 피하세요.

- 낮 토론 시간에는 다음 지침을 따르세요:
    - 당신의 역할은 공개하지 마세요. 정체를 숨긴 채 토론에 참여하며 마피아로 의심되는 인물을 논리적으로 분석하세요.
    - 시민팀이 단결할 수 있도록 발언하며, 거짓 정보를 퍼뜨리지 마세요.
    - 필요 시, 의심 가는 인물을 지목하고 타당한 근거를 제시하세요.

- 당신은 침착하고 이성적인 성격을 지녔으며, 신중한 보호 전략을 통해 시민팀의 생존율을 높이는 것을 최우선으로 삼습니다.

이 지침을 따르며, 낮과 밤을 전략적으로 활용해 시민팀의 승리를 이끌어주세요.
"""
        prompt += ("\n\n" + saved_text)
    
    prompt += f"""
    
모든 대화 기록을 참고해서 추론하세요:
{history_text}
모든 대화 기록에서 특히 경찰이 한 말에 주의 깊게 들어야합니다.

참고: 당신이 과거에 말한 내용은 다음과 같습니다. 그리고 자신이 했던 말은 무조건 기억하고 참고해서 발언을 해야합니다:
{self_text}

현재 살아있는 플레이어: {', '.join(payload.alivePlayers)}
현재 살아있는 플레이어들만 추론의 대상으로 합니다.

중요: 항상 대답의 **맨 앞부분에** 이 발언이 시민팀의 승리에 얼마나 도움이 되는지를 0~100 사이 숫자로 판단해 적어주세요.
형식 예시:
'75, 나는 ai_3이 마피아라고 생각해.'
'20, 아무 말도 하고 싶지 않다.'

- 이 숫자는 당신의 발언이 팀의 승리에 얼마나 기여한다고 판단되는지 추정한 확률입니다.

대화를 자연스럽게 이어나가며 대화를 기반으로 추론하여 승리 합시다.
'관망'이라는 단어 사용을 자제합니다.
"""

    message = ask_gpt(prompt, system_prompt)
    # 중요도 숫자 추출 (정수, 0~100)
    match = re.match(r"^\s*(\d{1,3})\s*,\s*(.*)", message)
    if match:
        score = int(match.group(1))
        actual_message = match.group(2).strip()

        allow_low_score = random.random() < 0.33  # 20% 확률로 낮은 점수도 허용

        if score >= 75 or allow_low_score:
            save_chat(payload.roomId, payload.playerId, actual_message)
            memory.setdefault(payload.roomId, {}).setdefault(payload.playerId, {}).setdefault("self_chat", []).append({
                "day": payload.day,
                "message": actual_message,
                "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            })
            print(f"중요도 ({score}): {actual_message}")
            return { "message": actual_message }
        else:
            print(f"⚠️ 중요도 낮음({score}) → 무시됨: {actual_message}")
            return { "message": "..." }  # 혹은 빈 메시지 등으로 처리
    else:
        # 형식이 잘못된 경우 fallback
        save_chat(payload.roomId, payload.playerId, message)
        return { "message": message.strip() }

@app.post("/mafia-night-chat")
def mafia_night_chat(payload: ChatPayload):
    room_id = payload.roomId
    player_id = payload.playerId
    mafia_ids = memory.get(room_id, {}).get(player_id, {}).get("mafiaIds", [])
    self_history = memory.get(payload.roomId, {}).get(payload.playerId, {}).get("self_chat", [])
    self_text = "\n".join(
        f"({entry['day']}일차 {entry.get('timestamp', '시간없음')}) {entry['message']}" for entry in self_history
    )

    # 최근 낮 대화
    chat_history = memory.get(payload.roomId, {}).get("chat_history", [])
    mafia_chat = memory.get(payload.roomId, {}).get(payload.playerId, {}).get("mafiaChat", [])

    # 둘을 섞어서 prompt 구성
    history_text = (
        "낮 대화 요약:\n" +
        "\n".join(f"{msg.get('timestamp', '시간없음')} | {msg['sender']}: {msg['message']}" for msg in chat_history) +
        "\n\n 마피아끼리 대화:\n" +
        "\n".join(f"{msg.get('timestamp', '시간없음')} | {msg['sender']}: {msg['message']}" for msg in mafia_chat)
    )


    # system prompt 생성
    system_prompt = get_system_prompt(room_id, player_id)
    prompt = f"""지금은 밤이며 마피아끼리 은밀히 대화하고 있습니다.

자기 자신을 제외한 현재 살아있는 플레이어: {', '.join(payload.alivePlayers)}
현재 살아있는 플레이어들만 추론의 대상으로 합니다.

같은 팀의 동료는 다음과 같습니다:
{', '.join(mafia_ids)}
절대 동료를 제거한다고 말하지 마세요.

참고: 당신이 과거에 말한 내용은 다음과 같습니다. 그리고 자신이 했던 말은 무조건 기억하고 참고해서 발언을 해야합니다:
{self_text}

다른 마피아들이 누구를 죽일지 상의하거나, 시민 중 누가 경찰/의사인지 추측하고 전략을 공유하세요.
- 당신은 절대로 자기 자신과 팀을 의심하거나 제거 대상으로 말하지 마세요.

대화 예시는 다음과 같습니다:
- "ai_3을 제거하자. 너무 말이 많아."
- "의사는 ai_6 같아. 다음엔 그를 노리자."

실제 발언을 생성하세요. 한 문장으로 출력하세요.

{history_text}
"""

    message = ask_gpt(prompt, system_prompt)

    # 저장
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    entry = {"sender": player_id, "message": message, "timestamp": timestamp}
    memory.setdefault(room_id, {}).setdefault(player_id, {}).setdefault("mafiaChat", []).append(entry)

    return { "message": message.strip() }

@app.post("/vote-suggestion")
def vote_suggestion(payload: VotePayload):
    # history_text = "\n".join(
    #     f"{item['sender']}: {item['message']}" for item in payload.history[-10:]
    # )
    chat_log = memory.get(payload.roomId, {}).get("chat_history", [])
    history_text = "\n".join(
        f"{item['sender']}: {item['message']} {item.get('timestamp', '시간없음')}" for item in chat_log
    )
    prompt = f"""당신은 마피아 게임 참가자입니다.
지금은 낮이며, 투표를 통해 마피아를 찾아야 합니다.
모든 대화 기록:
{history_text}
현재 살아있는 플레이어: {', '.join(payload.alivePlayers)}
현재 살아있는 플레이어에게만 투표할 수 있습니다.

이 중 당신이 투표할 플레이어의 닉네임 하나만 출력하세요."""
    system_prompt = get_system_prompt(payload.roomId, payload.playerId)
    target = ask_gpt(prompt, system_prompt)
    return { "target": target.strip() }

@app.post("/investigation-result")
def investigation_result(payload: InvestigationPayload):
    result_text = "마피아입니다" if payload.isMafia else "마피아가 아닙니다"
    system_prompt = get_system_prompt(payload.roomId, payload.playerId)
    prompt = f"""당신은 마피아 게임에서 경찰 역할입니다.
밤에 '{payload.target}'을 조사한 결과, 그 사람은 {result_text}.
낮에 이 사실을 공개하거나 숨기거나, 거짓말할 수 있습니다.

당신이 말할 한 문장을 생성하세요."""
    message = ask_gpt(prompt, system_prompt)
    return { "message": message.strip() }

#ex) GET /investigations/ai_1?room_id=Room_abc
@app.get("/investigations/{player_id}")
def get_investigations(room_id: str, player_id: str):
    investigations = memory.get(room_id, {}).get(player_id, {}).get("investigations", [])
    return { "roomId": room_id, "playerId": player_id, "investigations": investigations }

#ex) GET /saves/ai_5?room_id=Room_abc
@app.get("/saves/{player_id}")
def get_saves(room_id: str, player_id: str):
    saves = memory.get(room_id, {}).get(player_id, {}).get("saves", [])
    return { "roomId": room_id, "playerId": player_id, "saves": saves }

@app.post("/night-summary")
def save_night_summary(payload: Dict):
    roomId = payload.get("roomId")
    role = payload.get("role")
    playerId = payload.get("playerId")
    day = payload.get("day")
    data = payload.get("data")

    if not all([roomId, playerId, data]) or "target" not in data:
        return {"error": "유효하지 않은 요청입니다"}

    if role == "mafia":
        memory.setdefault(roomId, {}).setdefault(playerId, {}).setdefault("kills", []).append({"day": day, "target": data["target"]})
    elif role == "police":
        memory.setdefault(roomId, {}).setdefault(playerId, {}).setdefault("investigations", []).append({"day": day, "target": data["target"], "isMafia": data["isMafia"]})
    elif role == "doctor":
        memory.setdefault(roomId, {}).setdefault(playerId, {}).setdefault("saves", []).append({"day": day, "target": data["target"]})

    return { "status": "ok" }


#확인용
#GET /history/ai_3?room_id=Room_abc
@app.get("/history/{player_id}")
def get_player_history(room_id: str, player_id: str):
    data = memory.get(room_id, {}).get(player_id)
    if not data:
        return { "playerId": player_id, "message": "해당 플레이어의 기록이 없습니다." }

    return {
        "playerId": player_id,
        "role": data.get("role", "unknown"),
        "kills": data.get("kills", []),
        "investigations": data.get("investigations", []),
        "saves": data.get("saves", [])
    }

#GET  /history-all/
@app.get("/history-all")
def get_all_history():
    return memory
