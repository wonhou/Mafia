using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour
{
    public static Login Instance;

    public static string nickname = "";

    public TMP_InputField InputField;          
    public WebSocketClient wsClient;            

    private void Awake()
    {
        Instance = this;
    }

    public void ButtonClick()
    {
        if (InputField == null)
        {
            Debug.LogError("❌ InputField가 Inspector에서 연결되지 않았습니다!");
            return;
        }

        if (wsClient == null)
        {
            Debug.LogError("❌ wsClient가 Inspector에서 연결되지 않았습니다!");
            return;
        }

        nickname = InputField.text;
        Debug.Log("✅ 닉네임: " + nickname);

        wsClient.Init();  // WebSocket 등록 요청
    }
}
