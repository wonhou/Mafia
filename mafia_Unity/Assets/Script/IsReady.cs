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
        if (isready)//�÷��̾ �غ���� ������
        {
            image.SetActive(false); //���� ���� �̹����� ����� ����
        }
        else
        {
            image.SetActive(true);
        }
    }
}
