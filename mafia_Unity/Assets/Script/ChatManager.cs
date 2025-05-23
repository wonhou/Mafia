using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ChatManager : MonoBehaviour
{
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
        // Shift+Enter �ٹٲ��� �����ϰ�, Enter�� ó��
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

        InputField.text = "";

        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator RefocusInputField()
    {
        yield return null; // �� ������ ���
        InputField.ActivateInputField(); // Ŀ�� ����
    }
}
