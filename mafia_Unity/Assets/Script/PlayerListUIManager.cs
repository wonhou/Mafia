using UnityEngine;
using TMPro;

public class PlayerListUIManager : MonoBehaviour
{
    public Transform playerListParent;
    public GameObject playerItemPrefab;

    public void UpdatePlayerList(PlayerInfo[] players)
    {
        foreach (Transform child in playerListParent)
        {
            Destroy(child.gameObject);
        }

        foreach (PlayerInfo player in players)
        {
            GameObject go = Instantiate(playerItemPrefab, playerListParent);
            go.GetComponentInChildren<TextMeshProUGUI>().text = $"{player.name} {(string.IsNullOrEmpty(player.roomId) ? "" : "(방 참여 중)")}";
        }
    }
}
