using UnityEngine;
using UnityEngine.UI;
using TMPro;
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


    public string GetSelectedTarget()
    {
        return selectedTargetId;
    }

}
