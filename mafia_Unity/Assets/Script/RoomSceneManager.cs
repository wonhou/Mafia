using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class RoomSceneManager : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomIdText;
    public Button startButton;
    public GameObject[] playerSlots;

    public static RoomSceneManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void SetRoomInfo(string name, string id, bool isOwner)
    {
        if (roomNameText == null || roomIdText == null || startButton == null)
        {
            Debug.LogWarning("❗ SetRoomInfo: UI 요소가 null입니다. 중단합니다.");
            return;
        }

        roomNameText.text = name;
        roomIdText.text = id;
        startButton.interactable = isOwner;

        StartCoroutine(WaitAndUpdatePlayerCards());
    }

    private IEnumerator WaitAndUpdatePlayerCards()
    {
        float timeout = 2f;

        while ((MafiaClientUnified.Instance == null ||
               MafiaClientUnified.Instance.currentPlayers == null ||
               MafiaClientUnified.Instance.currentPlayers.Length == 0) && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        // 조건 만족 못하더라도 호출은 시도함
        UpdatePlayerCards();
    }

    public void UpdatePlayerCards()
    {
        var players = MafiaClientUnified.Instance.currentPlayers;
        var readyMap = MafiaClientUnified.Instance.readyStatusMap;
        string myId = MafiaClientUnified.Instance.playerId;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            SetSlot(i, "", false);
        }

        var self = players.FirstOrDefault(p => p.id == myId);
        if (self != null)
        {
            bool isReady = readyMap.ContainsKey(myId) && readyMap[myId];
            SetSlot(0, FormatName(self), isReady);
        }

        int idx = 1;
        foreach (var p in players)
        {
            if (p.id == myId) continue;
            if (idx >= playerSlots.Length) break;

            bool isReady = readyMap.ContainsKey(p.id) && readyMap[p.id];
            SetSlot(idx++, FormatName(p), isReady);
        }
    }
    
    void SetSlot(int index, string nameText, bool isReady)
    {
        if (index < 0 || index >= playerSlots.Length)
        {
            Debug.LogWarning($"❗ 잘못된 슬롯 인덱스 접근 시도: {index}");
            return;
        }

        var slot = playerSlots[index];
        if (slot == null)
        {
            Debug.LogWarning($"❗ 슬롯 {index}가 null입니다.");
            return;
        }

        var nameTransform = slot.transform.Find("Name");
        if (nameTransform == null)
        {
            Debug.LogWarning($"❗ 슬롯 {index}에 'Name' 오브젝트가 없습니다.");
            return;
        }

        var name = nameTransform.GetComponent<TextMeshProUGUI>();
        if (name == null)
        {
            Debug.LogWarning($"❗ 슬롯 {index}의 'Name'에 TextMeshProUGUI가 없습니다.");
            return;
        }

        name.text = nameText;

        var readyTransform = slot.transform.Find("Ready");
        if (readyTransform != null)
        {
            var readyIcon = readyTransform.gameObject;
            readyIcon.SetActive(isReady && !string.IsNullOrEmpty(nameText));
        }
    }


    string FormatName(RoomPlayer p)
    {
        return p.isOwner ? p.name + "(Host)" : p.name;
    }
}
