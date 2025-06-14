using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonHandler : MonoBehaviour
{
    public void OnClickStartGame()
    {
        var client = MafiaClientUnified.Instance;

        if (client == null || !client.isOwner)
            return;

        if (client.currentPlayers == null || client.currentPlayers.Length == 0)
            return;

        bool hasNotReady = false;
        foreach (var p in client.currentPlayers)
        {
            if (!p.isOwner && !p.id.StartsWith("ai_"))
            {
                if (!client.readyStatusMap.ContainsKey(p.id) || !client.readyStatusMap[p.id])
                {
                    hasNotReady = true;
                    break;
                }
            }
        }

        if (hasNotReady)
        {
            Debug.LogWarning("⛔ Ready하지 않은 유저가 있음. 게임 시작 불가");
            return;
        }

        client.OnClickStartGame(); // 서버에 start_game 전송
    }
}
