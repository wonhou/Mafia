from typing import Callable, Dict, Any, List
import inspect

# 툴 등록용 데코레이터
def tool(func: Callable) -> Callable:
    func._is_tool = True
    func._tool_name = func.__name__
    return func

class OpenAIToolFunction:
    def __init__(self, tools: List[Callable]):
        self.tool_map = {}
        for tool in tools:
            if hasattr(tool, "_is_tool"):
                name = getattr(tool, "_tool_name", tool.__name__)
                self.tool_map[name] = tool

    async def run_tool(self, tool_name: str, input_data: Dict[str, Any]) -> Any:
        if tool_name not in self.tool_map:
            raise ValueError(f"Tool '{tool_name}' not found.")
        
        tool_func = self.tool_map[tool_name]

        if inspect.iscoroutinefunction(tool_func):
            return await tool_func(input_data)
        else:
            return tool_func(input_data)
