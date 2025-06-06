using UnityEngine;

public class IsReady : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject image;
    bool isready = false;

    void Start()
    {
        image.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isready)//플레이어가 준비되지 않으면
        {
            image.SetActive(false); //레디 상태 이미지를 띄우지 않음
        }
        else
        {
            image.SetActive(true);
        }
    }
}
