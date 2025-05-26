using UnityEngine;

public class TestVote : MonoBehaviour
{
    public void OnClickVoteStart()
    {
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.SendVoteStart();
        }
        else
        {
            Debug.LogWarning("⚠️ MafiaClientUnified.Instance가 null입니다.");
        }
    }
}
