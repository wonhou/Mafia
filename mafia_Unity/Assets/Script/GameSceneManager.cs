using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI roomNameText;  // 방 이름 표시
    public TextMeshProUGUI roomIdText;    // 방 코드 표시
    public GameObject[] playerSlots;      // Player1 ~ Player8 오브젝트들

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        Debug.Log($"🔍 GameSceneManager.playerSlots 연결 상태: {string.Join(", ", playerSlots.Select(s => s != null ? s.name : "null"))}");
    }

    /// <summary>
    /// 턴 정보 표시 (예: "1번째 밤", "2번째 낮")
    /// </summary>
    public void SetTurn(int day, string phase)
    {
        if (turnText != null)
        {
            turnText.text = $"{day}번째 {phase}";
        }
    }

    /// <summary>
    /// 방 제목과 방 코드 텍스트 표시
    /// </summary>
    public void SetRoomMeta(string name, string id)
    {
        if (roomNameText != null) roomNameText.text = name;
        if (roomIdText != null) roomIdText.text = id;
    }

    /// <summary>
    /// 플레이어 슬롯에 이름 표시 및 사망자 회색 처리
    /// </summary>
    public void UpdatePlayerUI(RoomPlayer[] players)
    {
        if (players == null || playerSlots == null || playerSlots.Length == 0)
        {
            Debug.LogWarning("❗ players 또는 playerSlots가 null이거나 비어 있음");
            return;
        }

        for (int i = 0; i < playerSlots.Length; i++)
        {
            GameObject slot = playerSlots[i];
            if (slot == null)
            {
                Debug.LogWarning($"❗ 슬롯 {i}가 null입니다.");
                continue;
            }

            if (i >= players.Length)
            {
                slot.SetActive(false); // 빈 슬롯은 비활성화
                continue;
            }

            RoomPlayer p = players[i];

            // 슬롯 활성화
            slot.SetActive(true);

            // ✅ 이름 표시 시도: NameButton/Name 경로 탐색
            var nameObj = slot.transform.Find("NameButton/Name");
            if (nameObj == null)
            {
                Debug.LogWarning($"❗ 슬롯 {i}: 'NameButton/Name' 경로를 찾을 수 없습니다");
            }
            else
            {
                var nameText = nameObj.GetComponent<TextMeshProUGUI>();
                if (nameText == null)
                {
                    Debug.LogWarning($"❗ 슬롯 {i}: TextMeshProUGUI 컴포넌트가 없습니다");
                }
                else
                {
                    nameText.text = p.name;
                    nameText.color = Color.white;        // 혹시 알파가 0일 경우 대비
                    nameText.gameObject.SetActive(true);
                }
            }

            // ✅ 회색 처리: PlayerX/NameButton (Image)
            var nameButton = slot.transform.Find("NameButton");
            if (nameButton != null)
            {
                var bg = nameButton.GetComponent<Image>();
                if (bg != null)
                {
                        bg.color = p.isAlive
                            ? new Color(1f, 1f, 1f, 0f)  // 살아 있으면 투명
                            : Color.gray;               // 죽으면 회색
                }
            }
        }
    }
}