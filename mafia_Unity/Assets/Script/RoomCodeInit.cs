using TMPro;
using UnityEngine;

public class RoomCodeInit : MonoBehaviour
{

    public TextMeshProUGUI title;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        title.text = MafiaClientUnified.Instance.roomId;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
