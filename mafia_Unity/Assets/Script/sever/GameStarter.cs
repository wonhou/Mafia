using UnityEngine;
using NativeWebSocket;
using System.Text;

public class GameStarter : MonoBehaviour
{
    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:3000");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ 서버 연결됨!");

            string nickname = Login.nickname;
            string registerMsg = $"{{\"type\":\"register\", \"playerId\":\"{nickname}\"}}";
            websocket.SendText(registerMsg);
            Debug.Log($"📤 register 메시지 보냄: {nickname}");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("📨 받은 메시지: " + message);

            // 나중에 여기서 분기 처리 가능!
            // if (msg.type == "your_role") { ... }
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("❌ 에러 발생: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("🔌 연결 종료됨");
        };

        await websocket.Connect();
    }

    public void OnStartGameButtonClicked()
    {
        if (websocket != null)
        {
            websocket.SendText("{\"type\": \"start_game\"}");
            Debug.Log("📤 start_game 메시지 보냄");
        }
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
                websocket?.DispatchMessageQueue();
        #endif
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}