using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomSceneManager : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomIdText;
    public Button startButton;

    public static RoomSceneManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void SetRoomInfo(string name, string id, bool isOwner)
    {
        Debug.Log($"🧾 SetRoomInfo 호출됨: {name}, {id}, isOwner: {isOwner}");

        roomNameText.text = name;
        roomIdText.text = id;
        startButton.interactable = isOwner;
    }
}