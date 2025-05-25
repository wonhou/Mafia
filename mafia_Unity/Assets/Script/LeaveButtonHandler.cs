using UnityEngine;

public class LeaveButtonHandler : MonoBehaviour
{
    public void OnClickLeave()
    {
        if (MafiaClientUnified.Instance != null)
        {
            Debug.Log("🔘 Leave 버튼 클릭됨 → LeaveRoom() 호출");
            MafiaClientUnified.Instance.LeaveRoom();
        }
        else
        {
            Debug.LogWarning("⚠️ MafiaClientUnified.Instance가 null이어서 LeaveRoom 호출 실패");
        }
    }
}