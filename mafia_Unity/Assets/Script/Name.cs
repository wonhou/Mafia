using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;

public class Name : MonoBehaviour
{

    public TextMeshProUGUI text;

    public static Name Instance;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text.text = Login.nickname;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
