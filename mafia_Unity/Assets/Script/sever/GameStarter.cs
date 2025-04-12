using UnityEngine;
using NativeWebSocket;
using UnityEngine.UI;
using TMPro;
using System.Text;

[System.Serializable]
public class ServerMessage
{
    public string type;
    public string sender;
    public string message;
    public string role;
}

public class GameStarter : MonoBehaviour
{
    private WebSocket websocket;

    public TMP_Text chatDisplay;           // 채팅 로그 출력용 UI 텍스트
    public TMP_InputField chatInput;       // 채팅 입력창
    public Button startGameButton;         // Start Game 버튼 (옵션)

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:3000");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ 서버 연결됨");
            string nickname = Login.nickname;
            string registerMsg = $"{{\"type\":\"register\", \"playerId\":\"{nickname}\"}}";
            websocket.SendText(registerMsg);
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("📨 받은 메시지: " + message);

            ServerMessage msg = JsonUtility.FromJson<ServerMessage>(message);

            if (msg.type == "chat")
            {
                chatDisplay.text += $"{msg.sender}: {msg.message}\n";
            }
            else if (msg.type == "your_role")
            {
                Debug.Log($"역할: {msg.role}");
                // 역할 패널 띄우는 로직은 여기에 추가
            }
            else if (msg.type == "game_over")
            {
                chatDisplay.text += $"게임 종료! 승자: {msg.message}\n";
            }
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

    public void OnClickStartGame()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.SendText("{\"type\":\"start_game\"}");
            Debug.Log("📤 게임 시작 메시지 전송됨!");
        }
    }

    public void OnSendChat()
    {
        if (websocket == null || websocket.State != WebSocketState.Open) return;

        string message = chatInput.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        var payload = new
        {
            type = "chat",
            sender = Login.nickname,
            message = message
        };

        string json = JsonUtility.ToJson(payload);
        websocket.SendText(json);
        chatInput.text = "";
    }
}
