using UnityEngine;
using NativeWebSocket;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading.Tasks;

[System.Serializable]
public class ServerMessage
{
    public string type;
    public string playerId;
    public string playerName;
    public string message;
    public string role;
    public string roomId;
    public string roomName;
}

[System.Serializable]
public class RegisterMessage
{
    public string type = "register";
    public string playerName;

    public RegisterMessage(string playerName)
    {
        this.playerName = playerName;
    }
}

[System.Serializable]
public class CreateRoomMessage
{
    public string type = "create_room";
    public string roomName;
}

public class MafiaClientUnified : MonoBehaviour
{
    public static MafiaClientUnified Instance { get; private set; }

    public System.Action OnConnected;
    public TMP_InputField chatInput;
    public TMP_Text chatLog;
    public Button startGameButton;

    public string playerId;
    public string playerName;
     public string roomId;

    private WebSocket websocket;
    private WebSocketState lastState = WebSocketState.Closed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 유지용 싱글톤 구조
        }
    }

    void Start()
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }
    }

    public async void ConnectToServer()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.Log("⏩ 이미 연결된 WebSocket 사용");
            OnConnected?.Invoke();
            return;
        }

        websocket = new WebSocket("ws://localhost:3000");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ WebSocket 연결됨");
            Register();
            // ✅ 연결 완료되면 콜백 실행
            OnConnected?.Invoke();  // 연결 후 CreateRoom 호출을 위한 콜백 실행

            if (startGameButton != null)
                startGameButton.interactable = true;
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("📨 수신 메시지: " + message);

            try
            {
                ServerMessage root = JsonUtility.FromJson<ServerMessage>(message);

                if (string.IsNullOrEmpty(root.type))
                {
                    Debug.LogWarning("⚠️ type 필드가 없음");
                    return;
                }

                switch (root.type)
                {
                    case "register_success":
                    case "room_created":
                    case "chat":
                    case "your_role":
                    case "game_over":
                        HandleStandardMessage(JsonUtility.FromJson<ServerMessage>(message));
                        break;

                    case "update_players":
                        Debug.Log("🧑‍🤝‍🧑 플레이어 목록 갱신 메시지 수신 (현재 무시 중)");
                        break;

                    default:
                        Debug.Log("📦 처리되지 않은 메시지: " + message);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ 메시지 처리 중 예외 발생: " + ex.Message);
            }
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("❌ WebSocket 에러: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("🔌 연결 종료됨 (CloseCode: " + e + ")");
        };

        try
        {
            await websocket.Connect();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("⚠️ WebSocket 연결 실패: " + ex.Message);
        }
    }

    private void HandleStandardMessage(ServerMessage msg)
    {
        switch (msg.type)
        {
            case "register_success":
                playerId = msg.playerId;
                playerName = msg.playerName;
                Debug.Log($"🟢 등록 성공! ID: {playerId}, 이름: {playerName}");
                break;

            case "room_created":
                roomId = msg.roomId;
                Debug.Log($"✅ 방 생성 완료! ID: {msg.roomId}, 이름: {msg.roomName}");
                break;

            case "chat":
                chatLog.text += $"{msg.playerName}: {msg.message}\n";
                break;

            case "your_role":
                Debug.Log($"🎭 역할: {msg.role}");
                break;

            case "game_over":
                chatLog.text += $"게임 종료! 승자: {msg.message}\n";
                break;
        }
    }

    private void Register()
    {
        playerName = Login.nickname;

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("❌ playerName이 비어 있음. Register 전송 중단");
            return;
        }

        RegisterMessage msg = new RegisterMessage(playerName);
        string json = JsonUtility.ToJson(msg);
        websocket.SendText(json);
        Debug.Log("📌 Register 전송됨: " + json);
    }

    public async void CreateRoom()
    {
        Debug.Log($"🌐 CreateRoom 시도: websocket = {(websocket == null ? "null" : websocket.State.ToString())}");

        while (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.Log("🕐 WebSocket 연결 대기 중...");
            await Task.Delay(100);
        }

        Debug.Log("📌 방 생성 요청 - roomName: " + Pass_Name.room_name);

        var createRoomMsg = new CreateRoomMessage
        {
            roomName = Pass_Name.room_name
        };

        string json = JsonUtility.ToJson(createRoomMsg);
        websocket.SendText(json);
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("❌ JoinRoom 실패 - roomId 또는 playerId 없음");
            return;
        }

        var joinRoomMsg = new
        {
            type = "join_room",
            roomId = roomId,
            playerId = playerId
        };

        string json = JsonUtility.ToJson(joinRoomMsg);
        websocket.SendText(json);
        Debug.Log("📌 JoinRoom 메시지 전송됨: " + json);
    }

    public void LeaveRoom()
    {
        if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(roomId))
        {
            Debug.LogError("❌ LeaveRoom 실패 - playerId 또는 roomId 없음");
            return;
        }

        var leaveMsg = new
        {
            type = "leave_room",
            roomId = roomId,
            playerId = playerId
        };

        string json = JsonUtility.ToJson(leaveMsg);
        websocket.SendText(json);
        Debug.Log("📤 LeaveRoom 메시지 전송됨: " + json);
    }

    public void SendChat(string msg)
    {
        var payload = new { type = "chat", text = msg };
        string json = JsonUtility.ToJson(payload);
        websocket.SendText(json);
        chatInput.text = "";
    }

    public void OnClickStartGame()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.SendText("{\"type\":\"start_game\"}");
            Debug.Log("게임 시작 메시지 전송됨!");
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
        if (websocket != null && websocket.State != lastState)
        {
            Debug.Log("📡 WebSocket 상태: " + websocket.State);
            lastState = websocket.State;
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
            await websocket.Close();
    }
}
