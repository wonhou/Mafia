using TMPro;
using UnityEngine;

public class RoomMaking : MonoBehaviour
{
    public TMP_InputField roomNameInput;      // 유저가 입력하는 InputField
    public TextMeshProUGUI title;             // 상단 제목 표시용

    public void OnClickOK()
    {
        if (roomNameInput == null)
        {
            Debug.LogError("❌ roomNameInput 연결 안 됨");
            return;
        }

        Pass_Name.room_name = roomNameInput.text.Trim();
        Debug.Log("✅ OK 버튼 클릭됨 - 저장된 방 이름: " + Pass_Name.room_name);

        // ✅ MafiaClientUnified를 통해 방 생성 요청
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.CreateRoom();
        }
        else
        {
            Debug.LogError("❌ MafiaClientUnified.Instance가 null입니다! 서버에 방 생성 요청 실패");
        }
    }
}
