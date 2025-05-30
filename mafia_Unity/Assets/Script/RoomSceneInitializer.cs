using UnityEngine;

public class RoomSceneInitializer : MonoBehaviour
{

    void Start()
    {
        Debug.Log("🏁 Room 씬 진입 - Start() 호출됨");

        if (MafiaClientUnified.Instance == null)
        {
            Debug.LogError("❌ MafiaClientUnified.Instance == null");
            return;
        }

        // ✅ 연결되어 있으면 다시 연결하지 않음
        MafiaClientUnified.Instance.OnConnected = () =>
        {
            Debug.Log("📡 WebSocket 연결 완료 후 CreateRoom 호출");
        };

        MafiaClientUnified.Instance.ConnectToServer();
    }

    void OnDestroy()
    {
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.LeaveRoom();
        }
    }

}