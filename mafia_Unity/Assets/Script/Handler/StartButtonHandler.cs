using UnityEngine;

public class StartButtonHandler : MonoBehaviour
{
    public void OnClickStartGame()
    {
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.OnClickStartGame();
        }
        else
        {
            Debug.LogWarning("⚠️ MafiaClientUnified.Instance가 null입니다! 게임 시작 실패");
        }
    }
}
