from openai_tool_function import tool
from ollama_client import query_ollama

@tool
async def night_action_tool(input: dict) -> dict:
    """
    밤에 AI가 어떤 행동을 취할지 결정함
    """
    player_id = input.get("playerId", "ai_x")
    role = input.get("role", "villager")
    alive_players = input.get("alivePlayers", [])

    prompt = (
        f"당신은 마피아 게임에서 '{role}' 역할입니다.\n"
        f"당신의 ID는 {player_id}이고, 현재 살아 있는 플레이어 목록은 다음과 같습니다:\n"
        f"{', '.join(alive_players)}\n"
        f"밤에 어떤 행동을 할지 결정하고, 한 명을 타겟으로 선택하세요.\n"
        f"예: {{\"action\": \"kill\", \"target\": \"user1\"}}"
    )

    try:
        ai_response = await query_ollama(prompt, model="gemma3:12b")
        # 간단한 파싱 예시 (실제로는 JSON 형식 응답으로 만드는 게 더 안전함)
        return {
            "action": "응답 내용 기반 판단",
            "target": "응답 내용 기반 판단",
            "message": ai_response
        }
    except Exception as e:
        return {
            "action": "none",
            "target": "none",
            "message": f"Ollama 오류 발생: {e}"
        }
