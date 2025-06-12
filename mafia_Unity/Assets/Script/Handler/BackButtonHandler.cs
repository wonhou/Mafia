using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonHandler : MonoBehaviour
{
    public void OnBackClicked()
    {
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.Logout();
        }
    }
}