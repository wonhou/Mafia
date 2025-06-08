using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VoteMafia : MonoBehaviour
{
    public GameObject YesOrNo;
    public List<Button> Players;
    public Button[] Dicision;
    public TextMeshProUGUI yostitle;
    string buttonText;
    Button killed;

    void Start()
    {
        YesOrNo.SetActive(false);

        for (int i = 0; i < Players.Count; i++)
        {
            Button btn = Players[i];
            btn.onClick.AddListener(() => KillPlayer(btn));
        }
        Dicision[0].onClick.AddListener(() => Yes());
        Dicision[1].onClick.AddListener(() => No());
    }

    void KillPlayer(Button clickedButton)
    {
        YesOrNo.SetActive(true);
        ControlAllButtons(false);
        buttonText = clickedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        yostitle.text = buttonText + "를 죽이시겠습니까?";
        killed = clickedButton;
    }

    void Yes()
    {
        Debug.Log(buttonText + "를 죽이셨습니다.");
        Players.Remove(killed);
        killed.interactable = false;
        ControlAllButtons(true);
        YesOrNo.SetActive(false);
    }

    void No()
    {
        YesOrNo.SetActive(false);
        ControlAllButtons(true);
    }

    void ControlAllButtons(bool conditions)
    {
        foreach (Button btn in Players)
        {
            btn.interactable = conditions; //버튼을 누를 수 있게 할 것이냐
        }
    }
}