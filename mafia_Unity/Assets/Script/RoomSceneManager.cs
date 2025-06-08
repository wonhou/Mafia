using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
        roomNameText.text = name;
        roomIdText.text = id;
        startButton.interactable = isOwner;

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
        var slot = playerSlots[index];
        var name = slot.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        var readyIcon = slot.transform.Find("Ready")?.gameObject;

        name.text = nameText;

        if (readyIcon != null)
        {
            readyIcon.SetActive(isReady && !string.IsNullOrEmpty(nameText));
        }
    }

    string FormatName(RoomPlayer p)
    {
        return p.isOwner ? p.name + "(Host)" : p.name;
    }
}
