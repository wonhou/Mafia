import httpx

async def query_ollama(prompt: str, model: str = "exaone3.5:7.8b") -> str:
    system_prompt = (
        "너는 마피아 게임 AI야. 주어진 역할에 따라 자연스럽고 논리적으로 행동을 판단해야 해.\n"
        "가능한 행동은 kill, investigate, save, none 이고, 반드시 아래 JSON 형태로만 응답해야 해:\n"
        "{\"action\": \"kill\", \"target\": \"user1\"}"
    )
    full_prompt = system_prompt + "\n\n" + prompt

    payload = {
        "model": model,
        "prompt": full_prompt,
        "stream": False
    }

    try:
        async with httpx.AsyncClient(timeout=20.0) as client:
            response = await client.post(
                "https://7fc4-211-49-34-11.ngrok-free.app/api/generate",
                json=payload,
                headers={"Content-Type": "application/json"}
            )
            response.raise_for_status()
            return response.json()["response"].strip()
    except httpx.HTTPStatusError as e:
        return f"Ollama HTTP 오류: {e.response.status_code} - {e.response.text}"
    except httpx.RequestError as e:
        return f"Ollama 요청 실패: {str(e)}"
    except Exception as e:
        return f"Ollama 알 수 없는 오류: {str(e)}"
