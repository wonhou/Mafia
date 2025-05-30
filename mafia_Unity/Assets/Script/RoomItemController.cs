using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoomItemController : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerCountText;

    private string roomId;

    public void SetRoomInfo(string name, int count, string id)
    {
        roomNameText.text = name;                     // ✅ roomName 표시
        playerCountText.text = $"{count}/8";
        roomId = id;                                   // ✅ roomId는 내부 입장용으로만 저장

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() => JoinRoom());
    }

    void JoinRoom()
    {
        Debug.Log($"🚪 방 클릭 → 입장 요청: {roomId}");

        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.JoinRoomDirect(roomId, MafiaClientUnified.Instance.playerId);
            SceneManager.LoadScene("Room");
        }
        
    }
}
