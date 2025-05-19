using TMPro;
using UnityEngine;

public class Pass_Name : MonoBehaviour
{
    public TMP_InputField Room_name;

    public static string room_name = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void pass_name()
    {
        room_name = Room_name.text;
        Debug.Log(Room_name.text);
    }
}
