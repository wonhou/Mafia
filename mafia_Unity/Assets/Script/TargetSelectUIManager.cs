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
    public void Show(List<string> playerIds)
    {
        gameObject.SetActive(true);
        selectedTargetId = null;
        buttonToPlayerId.Clear();
        Debug.Log("📣 TargetSelectUIManager.Show() 호출됨");

        for (int i = 0; i < 8; i++)
        {
            Transform player = playerListParent.Find($"Player{i + 1}");
            if (player == null)
            {
                Debug.LogError($"❌ Player{i + 1} 오브젝트를 찾을 수 없습니다!");
                continue;
            }

            Transform nameButtonObj = player.Find("NameButton");
            if (nameButtonObj == null)
            {
                Debug.LogError($"❌ Player{i + 1} > NameButton 오브젝트 없음!");
                continue;
            }

            Transform nameObj = nameButtonObj.Find("Name");
            if (nameObj == null)
            {
                Debug.LogError($"❌ Player{i + 1} > NameButton > Name 오브젝트 없음!");
                continue;
            }

            Button btn = nameButtonObj.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError($"❌ Player{i + 1} > NameButton 오브젝트에 Button 컴포넌트가 없음!");
                continue;
            }

            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            if (nameText == null)
            {
                Debug.LogError($"❌ Player{i + 1} > Name 텍스트 없음!");
                continue;
            }

            if (i < playerIds.Count)
            {
                string pid = playerIds[i];
                nameText.text = pid;
                btn.interactable = true;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPlayerSelected(pid, btn));

                buttonToPlayerId[btn] = pid;

                // 기본 색상: 투명
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
        Debug.Log($"🖱️ 선택됨: {playerId}");
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
