using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class RoomListManager : MonoBehaviour
{
    public Button refreshButton;
    public Transform roomListParent;
    public GameObject roomItemPrefab;

    void Start()
    {
        RequestRoomList();

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(RequestRoomList);
        }
    }

    public void RequestRoomList()
    {
        StartCoroutine(SendListRoomWhenConnected());
        MafiaClientUnified.Instance?.RequestPlayerList();
    }

    private IEnumerator SendListRoomWhenConnected()
    {
        while (MafiaClientUnified.Instance == null || !MafiaClientUnified.Instance.IsConnected())
        {
            Debug.Log("🕐 WebSocket 연결 대기 중...");
            yield return new WaitForSeconds(0.1f);
        }

        var msg = new ListRoomsMessage();  // type: "list_rooms"
        string json = JsonUtility.ToJson(msg);
        MafiaClientUnified.Instance.SendRaw(json);
    }

    public void DisplayRoomList(RoomSummary[] rooms)
    {
        // 기존 목록 삭제
        foreach (Transform child in roomListParent)
        {
            Destroy(child.gameObject);
        }

        // 새 방 리스트 추가
        foreach (RoomSummary room in rooms)
        {
            GameObject go = Instantiate(roomItemPrefab, roomListParent);
            RoomItemController controller = go.GetComponent<RoomItemController>();

            if (controller != null)
            {
                controller.SetRoomInfo(room.roomName, room.playerCount, room.roomId);
            }
        }
    }

}
