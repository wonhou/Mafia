using UnityEngine;
using NativeWebSocket;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[System.Serializable]
public class ServerMessage
{
    public string type;
    public string playerId;
    public string playerName;
    public string sender;
    public string message;
    public string role;
    public string roomId;
    public string roomName;
}

[System.Serializable]
public class ListPlayersMessage
{
    public string type = "list_players";
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
public class SimpleMessage
{
    public string message;
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
    public bool isAlive;
}

[System.Serializable]
public class PlayerEliminatedMessage
{
    public string type;
    public string[] deadPlayers;
    public string reason;
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

[System.Serializable]
public class PlayerInfo
{
    public string id;
    public string name;
    public string roomId;
}

[System.Serializable]
public class PlayerListMessage
{
    public string type;
    public PlayerInfo[] players;
}

[System.Serializable]
public class RoomSummary
{
    public string roomId;
    public string roomName;
    public int playerCount;
}

[System.Serializable]
public class RoomListMessage
{
    public string type;
    public RoomSummary[] rooms;
}

[System.Serializable]
public class ReadyMessage
{
    public string type = "set_ready";
    public bool isReady;
}

[System.Serializable]
public class UpdateReadyMessage
{
    public string type;
    public ReadyPlayerStatus[] players;
}

[System.Serializable]
public class ReadyPlayerStatus
{
    public string playerId;
    public bool isReady;
}

[System.Serializable]
public class ListRoomsMessage
{
    public string type = "list_rooms";
}

public class MafiaClientUnified : MonoBehaviour
{
    public static MafiaClientUnified Instance { get; private set; }

    public System.Action OnConnected;
    public TMP_InputField chatInput;
    public TMP_Text chatLog;
    public Button startGameButton;
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomIdText;

    public string playerId;
    public string playerName;
    public string roomName;
    public string roomId;
    public string currentRole;
    public RoomPlayer[] currentPlayers;
    public Dictionary<string, bool> readyStatusMap = new();
    public bool isOwner = false;

    private WebSocket websocket;
    private WebSocketState lastState = WebSocketState.Closed;
    private bool isRegistered = false;
    private bool roomCreated = false;
    private bool isReady = false;

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
                        Debug.Log("📡 접속 중인 플레이어 목록 수신됨");

                        PlayerListMessage list = JsonUtility.FromJson<PlayerListMessage>(message);

                        if (list.players != null)
                        {
                            foreach (var player in list.players)
                            {
                                string roomStatus = string.IsNullOrEmpty(player.roomId) ? "🟢 로비" : $"🏠 {player.roomId}";
                                Debug.Log($"👤 {player.name} ({player.id}) - {roomStatus}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("⚠ 플레이어 목록이 null입니다.");
                        }

                        var listUI = FindFirstObjectByType<PlayerListUIManager>();
                        if (listUI != null)
                        {
                            listUI.UpdatePlayerList(list.players);
                        }
                        break;

                    case "room_info":
                        RoomInfoMessage roomInfo = JsonUtility.FromJson<RoomInfoMessage>(message);
                        Debug.Log($"🏠 방 정보 수신됨 - RoomID: {roomInfo.roomId}, RoomName: {roomInfo.roomName}, 방장 여부: {roomInfo.isOwner}");

                        isOwner = roomInfo.isOwner;
                        roomName = roomInfo.roomName;
                        roomId = roomInfo.roomId;
                        currentPlayers = roomInfo.players;

                        // ✅ RoomScene에서만 UI 갱신
                        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                        Debug.Log($"🧭 현재 씬 이름: {sceneName}");

                        if (sceneName == "Room")
                        {
                            if (RoomSceneManager.Instance != null && RoomSceneManager.Instance.gameObject != null)
                            {
                                Debug.Log("✅ RoomSceneManager 인스턴스 존재함 → 코루틴 실행");
                                StartCoroutine(WaitAndSetRoomInfo(roomInfo));
                            }
                            else
                            {
                                Debug.LogWarning("🚫 RoomSceneManager.Instance가 null이거나 이미 파괴됨");
                            }

                            if (Object.FindFirstObjectByType<ReadyButtonHandler>() != null)
                            {
                                StartCoroutine(WaitUntilReadyButtonAppearsAndSet(roomInfo));
                            }
                            else
                            {
                                Debug.LogWarning("⚠️ ReadyButtonHandler가 씬에 존재하지 않음");
                            }
                        }
                        else if (sceneName == "Game_Room")
                        {
                            Debug.Log("🕐 GameSceneManager 인스턴스 대기 시작");
                            StartCoroutine(WaitUntilGameSceneManagerReady(roomInfo));
                        }

                        break;

                    case "left_room":
                        Debug.Log($"🚪 방 나가기 완료! roomId: {root.roomId}");
                        break;

                    case "update_ready":
                        UpdateReadyMessage readyStatus = JsonUtility.FromJson<UpdateReadyMessage>(message);

                        if (readyStatusMap == null)
                        {
                            readyStatusMap = new Dictionary<string, bool>();
                        }

                        readyStatusMap.Clear();

                        if (readyStatus.players == null) break;

                        foreach (var p in readyStatus.players)
                        {
                            readyStatusMap[p.playerId] = p.isReady;
                        }

                        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                        if (sceneName == "Room")
                        {
                            RoomSceneManager.Instance?.UpdatePlayerCards();
                        }
                        else if (sceneName == "Game_Room")
                        {
                            GameSceneManager.Instance?.UpdatePlayerUI(currentPlayers);
                        }

                        break;


                    case "room_list":
                        Debug.Log("📥 방 목록 수신됨");
                        var listMsg = JsonUtility.FromJson<RoomListMessage>(message);

                        RoomListManager manager = FindFirstObjectByType<RoomListManager>();
                        if (manager != null)
                        {
                            manager.DisplayRoomList(listMsg.rooms);
                        }
                        else
                        {
                            Debug.LogError("❌ RoomListManager가 씬에 없어서 방 목록 표시 실패!");
                        }
                        break;

                    case "night_start":
                        ChatManager.Instance?.AddSystemMessage("밤이 되었습니다. 마피아, 의사, 경찰은 행동을 선택하세요.");
                        break;
                    case "day_start":
                        ChatManager.Instance?.AddSystemMessage("낮이 되었습니다. 자유롭게 토론을 시작하세요.");
                        break;
                    case "vote_start":
                        ChatManager.Instance?.AddSystemMessage("투표가 시작되었습니다. 처형할 사람을 선택하세요.");
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
                break;

            case "room_created":
                roomId = msg.roomId;
                roomName = msg.roomName;
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
                if (string.IsNullOrEmpty(msg.sender) || string.IsNullOrEmpty(msg.message))
                {
                    Debug.LogWarning("⚠️ chat 메시지 누락 또는 null 발생");
                    return;
                }

                Debug.Log($"💬 {msg.sender}: {msg.message}");

                if (chatLog != null)
                {
                    chatLog.text += $"{msg.sender}: {msg.message}\n";
                }
                else
                {
                    Debug.LogWarning("⚠️ chatLog가 null입니다 (UI 미연결)");
                }
                break;

            case "your_role":
                currentRole = msg.role;

                string roleKor = currentRole switch {
                    "mafia" => "마피아",
                    "doctor" => "의사",
                    "police" => "경찰",
                    _ => "시민"
                };

                Debug.Log($"🎭 역할: {roleKor}");

                // ✅ 시스템 메시지 출력
                if (ChatManager.Instance != null)
                {
                    ChatManager.Instance.AddSystemMessage($"당신의 직업은 <b>{roleKor}</b> 입니다");
                    ChatManager.Instance.AddSystemMessage("밤이 시작되었습니다. 각자의 역할에 맞는 행동을 선택해주세요.");
                }
                else
                {
                    Debug.LogWarning("⚠️ ChatManager.Instance가 null입니다!");
                }
                
                if (GameSceneManager.Instance != null)
                {
                    GameSceneManager.Instance.SetRoomMeta(roomName, roomId);
                    GameSceneManager.Instance.SetTurn(1, "밤");
                }
                else
                {
                    Debug.LogWarning("❗ GameSceneManager.Instance가 null입니다");
                }

                StartCoroutine(WaitAndShowTargetSelectUI());
                break;

            case "night_start":
                var night = JsonUtility.FromJson<SimpleMessage>(msg.message);
                ChatManager.Instance?.AddSystemMessage(night.message);
                break;

            case "day_start":
                var day = JsonUtility.FromJson<SimpleMessage>(msg.message);
                ChatManager.Instance?.AddSystemMessage(day.message);
                break;

            case "night_end":
                string targetId = TargetSelectUIManager.Instance.GetSelectedTarget();
                if (!string.IsNullOrEmpty(targetId))
                {
                    string role = MafiaClientUnified.Instance.currentRole;
                    string action = role switch {
                        "mafia" => "kill",
                        "doctor" => "save",
                        "police" => "investigate",
                        _ => null
                    };

                    if (!string.IsNullOrEmpty(action))
                    {
                        MafiaClientUnified.Instance.SendNightAction(action, targetId);
                        Debug.Log($"📤 자동 행동 전송됨: {action} → {targetId}");
                    }
                }
                break;
            case "player_eliminated":
                var eliminated = JsonUtility.FromJson<PlayerEliminatedMessage>(msg.message);

                foreach (string deadId in eliminated.deadPlayers)
                {
                    var player = currentPlayers.FirstOrDefault(p => p.id == deadId);
                    if (player != null) player.isAlive = false;
                }

                GameSceneManager.Instance?.UpdatePlayerUI(currentPlayers);

                if (eliminated.deadPlayers.Length > 0)
                {
                    string deadNames = string.Join(", ", eliminated.deadPlayers.Select(id =>
                    {
                        var p = currentPlayers.FirstOrDefault(x => x.id == id);
                        return p != null ? p.name : id;
                    }));

                    ChatManager.Instance?.AddSystemMessage($"{deadNames}님이 밤에 사망했습니다.", Color.red);
                }
                else
                {
                    // ✅ 아무도 죽지 않았고, reason이 "saved"인 경우
                    if (!string.IsNullOrEmpty(eliminated.reason) && eliminated.reason == "saved")
                    {
                        // 구조상 누가 살아났는지는 알 수 없으니 메시지만 출력
                        ChatManager.Instance?.AddSystemMessage("의사에 의해 한 명이 살아났습니다.", Color.cyan);
                    }
                    else
                    {
                        ChatManager.Instance?.AddSystemMessage("아무도 죽지 않았습니다.");
                    }
                }

                // UI 갱신
                if (TargetSelectUIManager.Instance != null)
                {
                    List<string> aliveIds = currentPlayers.Where(p => p.isAlive).Select(p => p.id).ToList();
                    TargetSelectUIManager.Instance.Show(aliveIds);
                }
                break;
            
            case "vote_result":
                string votedId = msg.message;

                if (!string.IsNullOrEmpty(votedId))
                {
                    var votedPlayer = currentPlayers.FirstOrDefault(p => p.id == votedId);
                    string votedName = votedPlayer != null ? votedPlayer.name : votedId;

                    ChatManager.Instance?.AddSystemMessage($"{votedName}님이 투표로 처형당했습니다.", Color.red);
                    if (votedPlayer != null) votedPlayer.isAlive = false;
                }
                else
                {
                    ChatManager.Instance?.AddSystemMessage("투표 결과 동률입니다. 아무도 처형되지 않았습니다.");
                }

                GameSceneManager.Instance?.UpdatePlayerUI(currentPlayers);
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

    private IEnumerator WaitAndShowTargetSelectUI()
    {
        // 최대 2초 정도 대기 (Instance 등록될 때까지)
        float timeout = 2f;
        while (TargetSelectUIManager.Instance == null && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        if (TargetSelectUIManager.Instance != null)
        {
            List<string> aliveIds = currentPlayers
                .Where(p => p.isAlive)
                .Select(p => p.id)
                .ToList();

            TargetSelectUIManager.Instance.Show(aliveIds);
        }
        else
        {
            Debug.LogWarning("🚫 타겟 UI 초기화 시간 초과");
        }
    }

    private IEnumerator WaitAndSetRoomInfo(RoomInfoMessage roomInfo)
    {
        float timeout = 3f;

        while (timeout > 0f)
        {
            if (RoomSceneManager.Instance != null && RoomSceneManager.Instance.gameObject != null)
            {
                break;
            }
            yield return null;
            timeout -= Time.deltaTime;
        }

        if (RoomSceneManager.Instance != null && RoomSceneManager.Instance.gameObject != null)
        {
            RoomSceneManager.Instance.SetRoomInfo(roomInfo.roomName, roomInfo.roomId, roomInfo.isOwner);
        }
        else
        {
            Debug.LogWarning("🚫 RoomSceneManager.Instance가 null이거나 이미 파괴됨");
        }
    }

    private IEnumerator WaitUntilReadyButtonAppearsAndSet(RoomInfoMessage roomInfo)
    {
        float timeout = 3f;
        ReadyButtonHandler readyHandler = null;

        while ((readyHandler = Object.FindFirstObjectByType<ReadyButtonHandler>()) == null && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        if (readyHandler != null)
        {
            // ✅ 현재 플레이어가 방장인지 판단
            string myId = MafiaClientUnified.Instance.playerId;
            bool isMeOwner = false;

            foreach (var p in roomInfo.players)
            {
                if (p.id == myId)
                {
                    isMeOwner = p.isOwner;
                    break;
                }
            }
            readyHandler.SetReadyButtonState(!isMeOwner); // 방장이 아니면 Ready 버튼 활성화
        }
        else
        {
            Debug.LogWarning("⚠️ ReadyButtonHandler 초기화 실패");
        }
    }

    private IEnumerator WaitUntilGameSceneManagerReady(RoomInfoMessage roomInfo)
    {
        float timeout = 3f;

        while ((GameSceneManager.Instance == null || GameSceneManager.Instance.playerSlots == null
            || GameSceneManager.Instance.playerSlots.Length < 8
            || GameSceneManager.Instance.playerSlots.Any(s => s == null)) && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        if (GameSceneManager.Instance != null)
        {
            Debug.Log("✅ GameSceneManager 준비 완료 → UI 갱신 실행");
            GameSceneManager.Instance.SetRoomMeta(roomInfo.roomName, roomInfo.roomId);
            GameSceneManager.Instance.UpdatePlayerUI(roomInfo.players);
        }
        else
        {
            Debug.LogWarning("❗ GameSceneManager 준비 실패 (null 또는 슬롯 누락)");
        }
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
        _ = websocket.SendText(json);
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

    public void JoinRoomDirect(string customRoomId, string playerId)
    {
        // ✅ 내부 저장 추가
        this.roomId = customRoomId;
        this.playerId = playerId;

        JoinRoomMessage joinRoomMsg = new JoinRoomMessage(customRoomId, playerId);
        string json = JsonUtility.ToJson(joinRoomMsg);
        websocket.SendText(json);

        Debug.Log("📤 JoinRoomDirect 전송됨: " + json);
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

        // 🚫 방에 없는 상태면 서버에 leave_room 보내지 않음
        if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(roomId))
        {
            return;
        }

        var leaveMsg = new LeaveRoomMessage(roomId, playerId);
        string json = JsonUtility.ToJson(leaveMsg);
        websocket.SendText(json);
        Debug.Log("📤 LeaveRoom 메시지 전송됨: " + json);

        // ✅ 내부 상태 초기화
        roomId = "";
        roomName = "";
        isOwner = false;

        if (roomNameText != null) roomNameText.text = "";
        if (roomIdText != null) roomIdText.text = "";
        if (startGameButton != null) startGameButton.interactable = false;

        Debug.Log("🧹 내부 상태 초기화 완료 (roomId 제거, UI 리셋)");
    }

    public void RequestPlayerList()
    {
        var msg = new ListPlayersMessage();  // ✅ 클래스로 생성
        string json = JsonUtility.ToJson(msg);
        websocket.SendText(json);
        Debug.Log("📤 플레이어 목록 요청 전송됨: " + json);
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

    public void SendReady()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            isReady = !isReady;  // 🔄 상태 토글

            var msg = new ReadyMessage
            {
                isReady = isReady
            };

            string json = JsonUtility.ToJson(msg);
            websocket.SendText(json);
            Debug.Log("📤 Ready 상태 전송됨 (토글): " + json);
        }
        else
        {
            Debug.LogWarning("⚠️ WebSocket이 연결되어 있지 않음. Ready 전송 실패");
        }
    }

    public void ResetReadyState()
    {
        isReady = false;
    }

    public void SendNightStart()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            string msg = "{\"type\":\"night_start\"}";
            websocket.SendText(msg);
            Debug.Log("🌙 밤 시작 메시지 전송됨!");
        }
    }

    public void SendNightAction(string action, string target)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var payload = new
            {
                type = "night_action",
                action = action,
                target = target
            };

            string json = JsonUtility.ToJson(payload);
            websocket.SendText(json);
            Debug.Log("🌙 밤 행동 전송됨: " + json);
        }
    }
        public void SendDayStart()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.SendText("{\"type\":\"day_start\"}");
            Debug.Log("☀️ 낮 시작 메시지 전송됨!");
        }
    }

    public void SendVoteStart()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.SendText("{\"type\":\"vote_start\"}");
            Debug.Log("🗳️ 투표 시작 메시지 전송됨!");
        }
    }

    public void SendVote(string target)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var msg = new { type = "vote", target = target };
            string json = JsonUtility.ToJson(msg);
            websocket.SendText(json);
            Debug.Log("🗳️ 투표 전송됨: " + json);
        }
    }

    public void RequestRoomList()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var msg = new { type = "list_rooms" };
            string json = JsonUtility.ToJson(msg);
            websocket.SendText(json);
            Debug.Log("📤 방 목록 요청 전송됨!");
        }
    }

    public void Logout()
    {
        Debug.Log("🔒 로그아웃 시도");

        if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(roomId))
        {
            LeaveRoom();  // 서버에 leave_room 전송
        }

        // 연결 종료 요청
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.Close();  // 서버에서 연결 종료 로그 뜸
        }

        // 내부 상태 초기화
        playerId = null;
        playerName = null;
        roomId = null;
        roomName = null;
        isOwner = false;
        isReady = false;
        currentPlayers = null;
        readyStatusMap.Clear();

        Debug.Log("🧹 내부 상태 초기화 완료 (Logout)");
    }
    public bool IsConnected()
    {
        return websocket != null && websocket.State == WebSocketState.Open;
    }

    public void SendRaw(string json)
    {
        if (IsConnected())
        {
            websocket.SendText(json);
            Debug.Log("📡 SendRaw 전송됨: " + json);
        }
        else
        {
            Debug.LogWarning("❗ WebSocket이 연결되지 않음!");
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
        LeaveRoom();
        if (websocket != null && websocket.State == WebSocketState.Open)
            await websocket.Close();
    }
}
