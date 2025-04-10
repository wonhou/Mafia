using UnityEngine;
using NativeWebSocket;

public class GameStarter : MonoBehaviour
{
    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:3000");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ 서버 연결됨!");
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