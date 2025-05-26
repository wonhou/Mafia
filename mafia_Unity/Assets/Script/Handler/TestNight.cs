using UnityEngine;

public class TestNight : MonoBehaviour
{
    public void OnClickNightStart()
    {
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.SendNightStart();
        }
        else
        {
            Debug.LogWarning("⚠️ MafiaClientUnified.Instance 가 null이야!");
        }
    }
}