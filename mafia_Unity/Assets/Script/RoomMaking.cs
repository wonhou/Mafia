using TMPro;
using UnityEngine;

public class RoomMaking : MonoBehaviour
{
    public TextMeshProUGUI title;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        title.text = Pass_Name.room_name;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
