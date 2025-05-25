using TMPro;
using UnityEngine;

public class RoomCodeInit : MonoBehaviour
{
    public TextMeshProUGUI roomIdText;
    public TextMeshProUGUI roomNameText;

    public static RoomCodeInit Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateRoomUI(string id, string name)
    {
        if (roomIdText != null)
            roomIdText.text = id;

        if (roomNameText != null)
            roomNameText.text = name;
    }
}
