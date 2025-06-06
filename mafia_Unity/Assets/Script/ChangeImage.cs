using UnityEngine;
using UnityEngine.UI;

public class ChangeImage : MonoBehaviour
{
    public GameObject Panel;
    public Button[] buttons;           // 4���� ��ư
    public Button activeButton;        // ���õ� �̹����� ������ ��ư

    void Start()
    {
        Panel.SetActive(false);

        // �� ��ư�� Ŭ�� �̺�Ʈ ����
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // ���� ĸó ����
            buttons[i].onClick.AddListener(() => Change_Image(buttons[index]));
        }

        // activeButton Ŭ�� �� Panel ���
        activeButton.onClick.AddListener(TogglePanel);
    }

    void Change_Image(Button clickedButton)
    {
        activeButton.image.sprite = clickedButton.image.sprite;
    }

    void TogglePanel()
    {
        Panel.SetActive(!Panel.activeSelf);
    }
}
