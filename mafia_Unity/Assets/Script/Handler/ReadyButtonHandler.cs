using UnityEngine;
using UnityEngine.UI;

public class ReadyButtonHandler : MonoBehaviour
{
    public Button readyButton;

    public void SetReadyButtonState(bool isEnabled)
    {
        if (readyButton != null)
        {
            readyButton.interactable = isEnabled;
        }
    }

    public void OnClickReady()
    {
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.SendReady();
        }
    }
}