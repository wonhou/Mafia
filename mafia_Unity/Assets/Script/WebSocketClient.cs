using NativeWebSocket;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class WebSocketClient : MonoBehaviour
{
    public WebSocket websocket;

    public TMP_InputField roomIdInput;
    public TMP_InputField chatInput;
    public TMP_Text chatLog;

    public string playerId;
    public string roomId;

    

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:3000");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ 연결됨");
        };

        websocket.OnMessage += (bytes) =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("📨 " + msg);
            chatLog.text += "\n" + msg;
        };

        await websocket.Connect();
    }

    public void Init()
    {
        playerId = Login.nickname; 
        _ = SendJson(new RegisterMessage { playerId = playerId });
    }

    public void CreateRoom()
    {
        roomId = roomIdInput.text;
        if (string.IsNullOrEmpty(Pass_Name.room_name))
        {
            Debug.LogError("❌ Pass_Name.room_name이 비어 있습니다. pass_name()이 먼저 호출되었는지 확인하세요.");
            return;
        }

        Debug.Log("📌 방 생성 요청 - roomId: " + roomId + ", roomName: " + Pass_Name.room_name);
        _ = SendJson(new CreateRoomMessage { playerId = playerId, roomName = Pass_Name.room_name });
    }

    public void JoinRoom()
    {
        roomId = roomIdInput.text;
        _ = SendJson(new JoinRoomMessage { playerId = playerId, roomId = roomId });
    }

    public void SendChat(string msg)
    {
        _ = SendJson(new ChatMessage { text = msg });
    }

    async Task SendJson(object obj)
    {
        if (websocket.State == WebSocketState.Open)
        {
            string json = JsonUtility.ToJson(obj);
            await websocket.SendText(json);
        }
    }

    void Update()
    {
        if (websocket != null)
            websocket.DispatchMessageQueue();
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    [System.Serializable] class RegisterMessage { public string type = "register"; public string playerId; }
    [System.Serializable] class CreateRoomMessage { public string type = "create_room"; public string playerId; public string roomName; }
    [System.Serializable] class JoinRoomMessage { public string type = "join_room"; public string playerId; public string roomId; }
    [System.Serializable] class ChatMessage { public string type = "chat"; public string text; }
}
