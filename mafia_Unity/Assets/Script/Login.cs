using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    public static Login Instance;

    public static string nickname = "";

    public TMP_InputField InputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Awake()
    {
        Instance = this;
    }

    public void ButtonClick()
    {
        nickname = InputField.text;
        Debug.Log(InputField.text);
    }
}
