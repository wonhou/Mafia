using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    public static Login Instance;

    public static string nickname = "";

    string[] rand_nick = {"ShadowFox", "SilentWolf", "CrimsonCrow", "NightThorn", "GhostTrigger", "LunaVibe", "EchoFang", "MaskedIris", "AshReaper", "VelvetTrap","BloodWraith", "DarkWhisper", "GraveSilence",
    "HollowEyes"," CursedFang", "PhantomEcho", "RavenHex", "CrimsonShade", "SinisterBloom", "BlackMorrow", "Deadveil", "AshenSoul", "NocturneGrin", "RottenGaze", "BleedingMoon", "Hauntveil", "TwistedLullaby",
    "FrostbiteKiss", "BuriedSmile", "GloomCaller", "WhisperingHollow", "EbonClaw", "ScreamingDust", "GrimPulse", "MournShroud", "ChillStalker", "Duskwither", "HexedGrin", "ShiverLoom", "NightBleeder"};

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

    public void Random_Button_Click()
    {
        nickname = rand_nick[Random.Range(0, rand_nick.Length)];
        InputField.text = nickname;
    }
}
