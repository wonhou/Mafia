using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FindRoom : MonoBehaviour
{
    public TMP_InputField roomIdInput;
    public Button findButton;

    void Start()
    {
        if (findButton != null)
        {
            findButton.onClick.AddListener(OnFindClick);
        }
    }

    void OnFindClick()
    {
        string roomId = roomIdInput.text.Trim();

        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogWarning("❌ 입력된 방 코드가 없습니다");
            return;
        }

        if (MafiaClientUnified.Instance == null)
        {
            Debug.LogError("❌ MafiaClientUnified.Instance가 null입니다!");
            return;
        }

        string playerId = MafiaClientUnified.Instance.playerId;

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("❌ playerId가 null입니다! 아직 서버 등록 안 된 것 같아요");
            return;
        }

        MafiaClientUnified.Instance.JoinRoomDirect(roomId, playerId);
        Debug.Log($"📨 방 코드로 직접 입장 시도: {roomId}");
    }
}
