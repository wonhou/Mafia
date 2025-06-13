using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class TargetSelectUIManager : MonoBehaviour
{
    public static TargetSelectUIManager Instance { get; private set; }

    public Transform playerListParent;     // Player1~8이 들어있는 부모 오브젝트

    private Dictionary<Button, string> buttonToPlayerId = new();
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
        selectedTargetId = null;
        buttonToPlayerId.Clear();
        Debug.Log("📣 TargetSelectUIManager.Show() 호출됨");

        for (int i = 0; i < 8; i++)
        {
            Transform player = playerListParent.Find($"Player{i + 1}");
            if (player == null) continue;

            Transform nameButtonObj = player.Find("NameButton");
            if (nameButtonObj == null) continue;

            Button btn = nameButtonObj.GetComponent<Button>();
            if (btn == null) continue;

            if (i < playerIds.Count)
            {
                string pid = playerIds[i];

                // ✅ 밤일 경우, 시민이면 비활성화
                bool isNight = (role != null);
                bool isCitizen = role == "citizen";
                btn.interactable = !isNight || !isCitizen;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPlayerSelected(pid, btn));

                buttonToPlayerId[btn] = pid;
                btn.image.color = new Color(1f, 1f, 1f, 0f);
            }
            else
            {
                btn.interactable = false;
                btn.image.color = Color.gray;
            }
        }
    }

    void OnPlayerSelected(string playerId, Button clicked)
    {
        if (selectedTargetId == playerId)
        {
            selectedTargetId = null;
            clicked.image.color = new Color(1f, 1f, 1f, 0f);  // 투명 복원
            return;
        }

        selectedTargetId = playerId;

        foreach (var pair in buttonToPlayerId)
        {
            Button btn = pair.Key;
            // 죽은 사람 제외하고 모두 투명 처리
            if (btn.interactable)
                btn.image.color = new Color(1f, 1f, 1f, 0f);
        }

        clicked.image.color = Color.red;  // 선택된 대상만 빨간색
    }

    // 모든 버튼 비활성화
    public void DisableAllTargetButtons()
    {
        foreach (Transform child in playerListParent)
        {
            var button = child.GetComponent<Button>();
            if (button != null) button.interactable = false;
        }
    }
    public void EnableVoteButtons(RoomPlayer[] players)
    {
        foreach (Transform child in playerListParent)
        {
            var button = child.GetComponent<Button>();
            var targetId = child.name;

            // 상대가 살아 있어야만 투표 가능
            bool isAlive = players.FirstOrDefault(p => p.id == targetId)?.isAlive ?? false;
            if (button != null)
                button.interactable = isAlive;
        }
    }

    public void EnableNightTargetButtons(string role, string myId, RoomPlayer[] players)
    {
        foreach (Transform child in playerListParent)
        {
            var button = child.GetComponent<Button>();
            var targetId = child.name;

            // 자기 자신 선택 허용 여부 (의사만 허용)
            bool isSelf = (targetId == myId);
            bool canSelectSelf = (role == "doctor");

            if (isSelf && !canSelectSelf)
            {
                if (button != null) button.interactable = false;
                continue;
            }

            // 상대가 살아 있어야 함
            bool isAlive = players.FirstOrDefault(p => p.id == targetId)?.isAlive ?? false;
            if (!isAlive)
            {
                if (button != null) button.interactable = false;
                continue;
            }

            // 역할별 타겟팅 가능 여부 (현재는 모두 true)
            bool canTarget = role switch
            {
                "mafia" => true,
                "police" => true,
                "doctor" => true,
                _ => false
            };

            if (button != null)
                button.interactable = canTarget;
        }
    }

    public string GetSelectedTarget()
    {
        return selectedTargetId;
    }

}
