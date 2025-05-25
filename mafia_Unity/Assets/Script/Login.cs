using UnityEngine;
using TMPro;

public class Login : MonoBehaviour
{
    public static string nickname; // 🔹 전역 저장용

    public TMP_InputField inputField;
    public MafiaClientUnified mafiaClientUnified;
    public SceneChange sceneChanger;

    public void ButtonClick()
    {
        Debug.Log("🟡 로그인 시작");

        if (inputField == null || string.IsNullOrEmpty(inputField.text))
        {
            Debug.LogError("❌ inputField가 null이거나 비어있음");
            return;
        }

        nickname = inputField.text.Trim();

        if (mafiaClientUnified == null)
        {
            mafiaClientUnified = MafiaClientUnified.Instance;
            if (mafiaClientUnified == null)
            {
                Debug.LogError("❌ mafiaClientUnified 인스턴스 없음!");
                return;
            }
        }


        mafiaClientUnified.ConnectToServer();
    }
}

