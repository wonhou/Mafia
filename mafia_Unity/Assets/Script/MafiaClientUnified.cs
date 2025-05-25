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

[System.Serializable]
public class RoomPlayer
{
    public string id;
    public string name;
    public int slot;
    public bool isOwner;
}

[System.Serializable]
public class RoomInfoMessage
{
    public string type;
    public string roomId;
    public string roomName;
    public RoomPlayer[] players;
    public bool isOwner;
}

[System.Serializable]
public class JoinRoomMessage
{
    public string type = "join_room";
    public string roomId;
    public string playerId;

    public JoinRoomMessage(string roomId, string playerId)
    {
        this.roomId = roomId;
        this.playerId = playerId;
    }
}

[System.Serializable]
public class LeaveRoomMessage
{
    public string type = "leave_room";
    public string roomId;
    public string playerId;

    public LeaveRoomMessage(string roomId, string playerId)
    {
        this.roomId = roomId;
        this.playerId = playerId;
    }
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
    private bool isRegistered = false;
    private bool roomCreated = false;

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

                    case "room_info":
                        RoomInfoMessage roomInfo = JsonUtility.FromJson<RoomInfoMessage>(message);
                        Debug.Log($"🏠 방 정보 수신됨 - RoomID: {roomInfo.roomId}, RoomName: {roomInfo.roomName}, 방장 여부: {roomInfo.isOwner}");

                        foreach (RoomPlayer p in roomInfo.players)
                        {
                            Debug.Log($"🔹 슬롯 {p.slot} | 닉네임: {p.name} | ID: {p.id} | {(p.isOwner ? "👑 방장" : "유저")}");
                        }
                        break;

                    case "left_room":
                        Debug.Log($"🚪 방 나가기 완료! roomId: {root.roomId}");
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
                isRegistered = true;
                Debug.Log($"🟢 등록 성공! ID: {playerId}, 이름: {playerName}");
                TryJoinRoom();
                break;

            case "room_created":
                roomId = msg.roomId;
                roomCreated = true;
                Debug.Log($"✅ 방 생성 완료! ID: {msg.roomId}, 이름: {msg.roomName}");

                // 여기서 직접 방 ID 텍스트 업데이트

                if (RoomCodeInit.Instance != null)
                {
                    RoomCodeInit.Instance.UpdateRoomUI(roomId, Pass_Name.room_name);
                }

                TryJoinRoom();
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

        JoinRoomMessage joinRoomMsg = new JoinRoomMessage(roomId, playerId);
        string json = JsonUtility.ToJson(joinRoomMsg);
        Debug.Log("📌 JoinRoom 메시지 전송됨: " + json);
        websocket.SendText(json);
    }


    private void TryJoinRoom()
    {
        if (isRegistered && roomCreated)
        {
            Debug.Log("🚪 조건 충족 → JoinRoom 호출");
            JoinRoom();
        }
        else
        {
            Debug.Log($"⏳ 아직 대기 중 - isRegistered: {isRegistered}, roomCreated: {roomCreated}");
        }
    }

    public void LeaveRoom()
    {
         Debug.Log("📣 LeaveRoom() 호출됨");
        if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(roomId))
        {
            Debug.LogError("❌ LeaveRoom 실패 - playerId 또는 roomId 없음");
            return;
        }

        var leaveMsg = new LeaveRoomMessage(roomId, playerId);

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
