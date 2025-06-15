using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI roomNameText;  // 방 이름 표시
    public TextMeshProUGUI roomIdText;    // 방 코드 표시
    public GameObject[] playerSlots;      // Player1 ~ Player8 오브젝트들
    public int currentTurn = 1;
    public bool isNight = false;
    public TextMeshProUGUI timerText;
    private Coroutine timerCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        Debug.Log($"🔍 GameSceneManager.playerSlots 연결 상태: {string.Join(", ", playerSlots.Select(s => s != null ? s.name : "null"))}");
    }

    /// <summary>
    /// 타이머 시작
    /// </summary>
    public void StartTurnTimer(int seconds)
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(RunTimer(seconds));
    }

    private IEnumerator RunTimer(int seconds)
    {
        int timeLeft = seconds;
        while (timeLeft > 0)
        {
            if (timerText != null)
                timerText.text = $"{timeLeft}";

            yield return new WaitForSeconds(1f);
            timeLeft--;
        }

        if (timerText != null)
            timerText.text = "시간 종료!";
    }

    /// <summary>
    /// 타이머 종료 (중간에 강제 종료 시)
    /// </summary>
    public void StopTurnTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (timerText != null)
            timerText.text = "";
    }

    /// <summary>
    /// 턴 정보 표시 (예: "1번째 밤", "2번째 낮")
    /// </summary>
    public void SetTurn(int day, string phase)
    {
        currentTurn = day;
        isNight = (phase == "밤");

        if (turnText != null)
        {
            turnText.text = $"{day}번째 {phase}";
        }
    }

    public void UpdateTurnPhase(bool isNightTurn)
    {
        isNight = isNightTurn;

        if (isNight)
            currentTurn++;

        if (turnText != null)
        {
            string phase = isNight ? "밤" : "낮";
            turnText.text = $"{currentTurn}번째 {phase}";
        }
    }

    /// <summary>
    /// 방 제목과 방 코드 텍스트 표시
    /// </summary>
    public void SetRoomMeta(string name, string id)
    {
        if (roomNameText != null) roomNameText.text = name;
        if (roomIdText != null) roomIdText.text = id;
    }


    /// <summary>
    /// 플레이어 슬롯에 이름 표시 및 사망자 회색 처리
    /// </summary>
    public void UpdatePlayerUI(RoomPlayer[] players)
    {
        if (players == null || playerSlots == null || playerSlots.Length == 0)
        {
            Debug.LogWarning("❗ players 또는 playerSlots가 null이거나 비어 있음");
            return;
        }

        // 1. 슬롯 전부 초기화
        for (int i = 0; i < playerSlots.Length; i++)
        {
            GameObject slot = playerSlots[i];
            if (slot == null) continue;

            slot.SetActive(false);

            // 이름 텍스트 제거
            var nameObj = slot.transform.Find("NameButton/Name");
            if (nameObj != null)
            {
                var nameText = nameObj.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = "";                    // 텍스트 비우기
                    nameText.gameObject.SetActive(false); // UI 자체 숨기기
                }
            }

            // 배경 색상 초기화
            var nameButton = slot.transform.Find("NameButton");
            if (nameButton != null)
            {
                var bg = nameButton.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = new Color(1f, 1f, 1f, 0f); // 완전 투명화
                }
            }
        }
        // 살아있는 사람 -> 죽은 사람 순으로 정렬
        var sorted = players
            .OrderByDescending(p => p.isAlive)    // true > false
            .ThenBy(p => p.slot)         // 같은 상태면 slot 순서
            .ToList();


        // 2. 플레이어 정보 반영
        for (int i = 0; i < sorted.Count && i < playerSlots.Length; i++)
        {
            var p = sorted[i];
            GameObject slot = playerSlots[i];
            if (slot == null) continue;

            slot.SetActive(true);

            var nameText = slot.transform.Find("NameButton/Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = p.name;
                nameText.color = Color.white;
                nameText.gameObject.SetActive(true);
            }

            var bg = slot.transform.Find("NameButton")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = p.isAlive ? new Color(1f, 1f, 1f, 0f) : Color.gray;
            }
        }
    }
    public void ResetUI()
    {
        // 턴 텍스트 초기화
        if (turnText != null)
            turnText.text = "";

        // 플레이어 슬롯 초기화
        if (playerSlots != null)
        {
            foreach (var slot in playerSlots)
            {
                if (slot != null)
                    slot.SetActive(false);
            }
        }
    }
    public void ClearUI()
    {
        // 선택 UI 끄기
        TargetSelectUIManager.Instance?.DisableAllTargetButtons();

        // 턴 텍스트 초기화
        if (turnText != null)
            turnText.text = "";

        // 플레이어 슬롯 초기화
        if (playerSlots != null)
        {
            foreach (var slot in playerSlots)
            {
                if (slot != null)
                    slot.SetActive(false);
            }
        }
    }
}