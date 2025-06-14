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
    string nickname;
    public TMP_Text chatLog;

    public static ChatManager Instance { get; private set; }

    bool sendChatPending = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnEnable()
    {
        InputField.onEndEdit.AddListener(HandleEndEdit);
    }

    void OnDisable()
    {
        InputField.onEndEdit.RemoveListener(HandleEndEdit);
    }

    void Start()
    {
        nickname = Login.nickname;
        MafiaClientUnified.Instance?.SetChatInput(InputField);

        // ✅ 엔터로 입력
        InputField.onEndEdit.AddListener(HandleEndEdit);

        // ✅ OK 버튼 클릭 시
        ok.onClick.AddListener(() =>
        {
            sendChatPending = true;
            InputField.DeactivateInputField();  // 이걸 호출하면 OnEndEdit이 먼저 실행됨
        });
    }

    void HandleEndEdit(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        // 👉 중복 방지: 엔터 or 버튼 중 하나만 허용
        if (sendChatPending || (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)))
        {
            SendChat();
            sendChatPending = false;

            if (gameObject.activeInHierarchy)
                StartCoroutine(RefocusInputField());
        }
    }

    void SendChat()
    {
        string message = InputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // 🛡️ 밤에는 마피아만 채팅 가능
        bool isNight = GameSceneManager.Instance != null && GameSceneManager.Instance.isNight;
        string role = MafiaClientUnified.Instance?.currentRole ?? "";

        if (isNight && role != "mafia")
        {
            Debug.LogWarning("🚫 밤에는 마피아만 채팅할 수 있습니다!");
            return;
        }

        // 💬 채팅 전송
        if (MafiaClientUnified.Instance != null)
        {
            MafiaClientUnified.Instance.SendChat(message);
        }
        else
        {
            Debug.LogWarning("⚠️ MafiaClientUnified.Instance가 null입니다! 채팅 전송 실패");
        }

        // 🧹 입력창 초기화 및 포커스 재설정
        InputField.text = "";
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RefocusInputField());    
        }

        // 📜 채팅창 아래로 스크롤
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator RefocusInputField()
    {
        yield return null;
        InputField.ActivateInputField();
    }

    public void AddSystemMessage(string msg, Color? colorOverride = null)
    {
        GameObject chatItem = Instantiate(chatTextPrefab, chatContent);
        TextMeshProUGUI text = chatItem.GetComponent<TextMeshProUGUI>();

        text.text = $"[SYSTEM] {msg}";
        text.color = new Color(1f, 0.9f, 0.3f);  // 약간 노란색
        text.fontStyle = FontStyles.Bold;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void AddChatMessage(string sender, string message)
    {
        GameObject chatItem = Instantiate(chatTextPrefab, chatContent);
        TextMeshProUGUI text = chatItem.GetComponent<TextMeshProUGUI>();
        text.text = $"{sender}: {message}";
        text.color = Color.white;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}

