using UnityEngine;

public class TestDay : MonoBehaviour
{
    public void OnClickDayStart()
    {
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.SendDayStart();
        }
        else
        {
            Debug.LogWarning("⚠️ MafiaClientUnified.Instance가 null입니다.");
        }
    }
}