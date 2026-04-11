using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("Player HUD")]
    [SerializeField] private Slider playerHPSlider;
    [SerializeField] private TMP_Text playerHPText;

    [Header("Enemy HUD")]
    [SerializeField] private Slider enemyHPSlider;
    [SerializeField] private TMP_Text enemyHPText;
    [SerializeField] private TMP_Text enemyNameText;

    [Header("Skill Bar")]
    [SerializeField] private Button[] skillButtons;

    [Header("Result Popup")]
    [SerializeField] private GameObject panelResult;
    [SerializeField] private TMP_Text textResultMessage;
    [SerializeField] private Button btnReturnToMain;

    void Awake()
    {
        if (panelResult) panelResult.SetActive(false);
        if (btnReturnToMain) btnReturnToMain.onClick.AddListener(OnReturnToMainClicked);
    }

    void OnEnable()
    {
        // 전투 중 발생하는 데이터 갱신 이벤트 구독 (예: GameEvents.OnCombatUpdate)
        // GameEvents.OnBattleResult += HandleBattleResult;
    }

    void OnDisable()
    {
        // GameEvents.OnBattleResult -= HandleBattleResult;
    }

    private void OnReturnToMainClicked()
    {
        // 메인 씬으로 복귀 (BattleSceneData.CompleteBattle 이후 호출됨)
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_MainGame");
    }

    public void UpdatePlayerHP(float current, float max)
    {
        if (playerHPSlider) playerHPSlider.value = current / max;
        if (playerHPText) playerHPText.text = $"{current:0} / {max:0}";
    }

    public void UpdateEnemyHP(float current, float max)
    {
        if (enemyHPSlider) enemyHPSlider.value = current / max;
        if (enemyHPText) enemyHPText.text = $"{current:0} / {max:0}";
    }

    public void ShowBattleResult(string message)
    {
        if (panelResult) panelResult.SetActive(true);
        if (textResultMessage) textResultMessage.text = message;
    }
}
