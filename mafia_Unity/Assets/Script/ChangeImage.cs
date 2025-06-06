using UnityEngine;
using UnityEngine.UI;

public class ChangeImage : MonoBehaviour
{
    public GameObject Panel;
    public Button[] buttons;           // 4개의 버튼
    public Button activeButton;        // 선택된 이미지를 보여줄 버튼

    void Start()
    {
        Panel.SetActive(false);

        // 각 버튼에 클릭 이벤트 연결
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // 람다 캡처 방지
            buttons[i].onClick.AddListener(() => Change_Image(buttons[index]));
        }

        // activeButton 클릭 시 Panel 토글
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
