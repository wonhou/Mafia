using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    // public WebSocketClient webSocketClient;
    public TMP_InputField InputField;
    public Button ok;
    public Transform chatContent;
    public GameObject chatTextPrefab;
    public ScrollRect scrollRect;
    string nickname = Login.nickname;

    void Start()
    {
        ok.onClick.AddListener(SendChat);
        InputField.onEndEdit.AddListener(HandleEndEdit);
    }

    void HandleEndEdit(string value)
    {
        // Shift+Enter
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            SendChat();
            StartCoroutine(RefocusInputField());
        }
    }

    void SendChat()
    {
        string message = InputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        GameObject chatItem = Instantiate(chatTextPrefab, chatContent);
        chatItem.GetComponent<TextMeshProUGUI>().text = nickname + ": " + message;
        
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.SendChat(message); // 🔄 변경됨
        }
        else
        {
            Debug.LogWarning("⚠️ MafiaClientUnified.Instance가 null입니다! 채팅 전송 실패"); // 🔄 추가
        }

        InputField.text = "";
        if (gameObject.activeInHierarchy)
        {
        StartCoroutine(RefocusInputField());
        }

        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator RefocusInputField()
    {
        yield return null; 
        InputField.ActivateInputField(); 
    }

    
}
