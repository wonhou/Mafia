using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class TargetSelectUIManager : MonoBehaviour
{
    public static TargetSelectUIManager Instance { get; private set; }

    public Transform playerListParent;

    private Dictionary<Button, string> buttonToPlayerId = new();
    private Dictionary<string, Button> playerIdToButton = new();
    private Dictionary<Button, Image> buttonToImage = new();
    private string selectedTargetId = null;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Show(List<string> playerIds, string role = null)
    {
        gameObject.SetActive(true);
        buttonToPlayerId.Clear();
        playerIdToButton.Clear();
        buttonToImage.Clear();
        selectedTargetId = null;

        for (int i = 0; i < 8; i++)
        {
            Transform player = playerListParent.Find($"Player{i + 1}");
            if (player == null) continue;

            Transform nameButtonObj = player.Find("NameButton");
            if (nameButtonObj == null) continue;

            Button btn = nameButtonObj.GetComponent<Button>();
            if (btn == null) continue;

            Image img = btn.GetComponent<Image>();
            if (img == null) continue;

            // 스프라이트가 없다면 기본 스프라이트 생성
            if (img.sprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                img.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            }

            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            img.color = new Color(1f, 1f, 1f, 0f);

            buttonToImage[btn] = img;
            btn.onClick.RemoveAllListeners();

            if (i < playerIds.Count)
            {
                string pid = playerIds[i];
                buttonToPlayerId[btn] = pid;
                playerIdToButton[pid] = btn;

                TMP_Text nameText = nameButtonObj.GetComponentInChildren<TMP_Text>();
                var playerData = MafiaClientUnified.Instance.currentPlayers.FirstOrDefault(p => p.id == pid);
                if (nameText != null)
                    nameText.text = playerData?.name ?? pid;

                if (playerData == null || !playerData.isAlive)
                {
                    img.color = Color.gray;
                    btn.interactable = false;
                    continue;
                }

                bool allow = ShouldEnableButton(role, pid);
                btn.interactable = allow;

                if (allow)
                {
                    btn.onClick.AddListener(() => OnPlayerSelected(pid));
                }
            }
            else
            {
                btn.interactable = false;
                img.color = Color.gray;
            }
        }
    }

    void OnPlayerSelected(string playerId)
    {
        if (selectedTargetId != null && playerIdToButton.TryGetValue(selectedTargetId, out var prevBtn))
        {
            if (buttonToImage.TryGetValue(prevBtn, out var prevImg))
            {
                prevImg.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        selectedTargetId = playerId;

        if (playerIdToButton.TryGetValue(playerId, out var btn) &&
            buttonToImage.TryGetValue(btn, out var img))
        {
            img.color = Color.red;
        }
    }

    public void RefreshSelectedState()
    {
        foreach (var pair in playerIdToButton)
        {
            if (!buttonToImage.TryGetValue(pair.Value, out var img)) continue;

            img.color = (pair.Key == selectedTargetId) ? Color.red : new Color(1f, 1f, 1f, 0f);
        }
    }

    public void DisableAllTargetButtons()
    {
        foreach (Transform child in playerListParent)
        {
            var button = child.GetComponentInChildren<Button>();
            if (button != null) button.interactable = false;
        }
    }

    private bool ShouldEnableButton(string role, string pid)
    {
        var playerData = MafiaClientUnified.Instance.currentPlayers.FirstOrDefault(p => p.id == pid);
        if (playerData == null || !playerData.isAlive)
            return false;

        bool isVoteTime = (role == "vote");
        bool isDoctor = (role == "doctor");
        bool isPolice = (role == "police");
        bool isMafia = (role == "mafia");
        bool isSelf = (pid == MafiaClientUnified.Instance.playerId);

        bool isNight = isMafia || isDoctor || isPolice;

        if (isVoteTime)
            return true;

        if (isNight)
            return (!isSelf || isDoctor);

        return false;
    }

    public string GetSelectedTarget()
    {
        return selectedTargetId;
    }
}
