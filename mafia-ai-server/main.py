from fastapi import FastAPI
from openai_tool_function import OpenAIToolFunction
from tools import night_action_tool  # 너의 툴 import

app = FastAPI()

# 도구 엔진 초기화
engine = OpenAIToolFunction(
    tools=[night_action_tool.night_action_tool]
)

@app.post("/tools/night-action")
async def call_night_action(input: dict):
    return await engine.run_tool("night_action_tool", input)

@app.get("/")
def root():
    return {"message": "FastAPI is running"}
