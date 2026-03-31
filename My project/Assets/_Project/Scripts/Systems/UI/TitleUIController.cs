using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnExit;

    void Start()
    {
        if (btnStart) btnStart.onClick.AddListener(OnStartClicked);
        if (btnSettings) btnSettings.onClick.AddListener(OnSettingsClicked);
        if (btnExit) btnExit.onClick.AddListener(OnExitClicked);
    }

    private void OnStartClicked()
    {
        GameManager.Instance.OnClickStart();
    }

    private void OnSettingsClicked()
    {
        // TODO: 환경설정 팝업 로직 (필요 시)
        Debug.Log("Settings Clicked");
    }

    private void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
