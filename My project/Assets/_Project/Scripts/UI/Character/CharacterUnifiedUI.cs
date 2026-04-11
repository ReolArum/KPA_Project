using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterUnifiedUI : MonoBehaviour
{
    public static CharacterUnifiedUI Instance { get; private set; }

    [Header("Tabs")]
    [SerializeField] private GameObject panelStatus;
    [SerializeField] private GameObject panelInventory;
    [SerializeField] private Button btnTabStatus;
    [SerializeField] private Button btnTabInventory;

    [Header("Content Tabs")]
    [SerializeField] private CharacterStatusTab statusTab;
    [SerializeField] private CharacterInventoryTab inventoryTab;

    [Header("Window Control")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button btnClose;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        windowRoot.SetActive(false); // 초기에는 닫힘

        btnTabStatus.onClick.AddListener(() => SwitchTab(true));
        btnTabInventory.onClick.AddListener(() => SwitchTab(false));
        btnClose.onClick.AddListener(Close);
    }

    public void Open()
    {
        windowRoot.SetActive(true);
        SwitchTab(true); // 기본적으로 상태창 오픈
        Refresh();
    }

    public void Close() => windowRoot.SetActive(false);
    public void Toggle() 
    {
        if (windowRoot.activeSelf) Close();
        else Open();
    }

    private void SwitchTab(bool isStatus)
    {
        panelStatus.SetActive(isStatus);
        panelInventory.SetActive(!isStatus);
        Refresh();
    }

    public void Refresh()
    {
        if (!windowRoot.activeSelf) return;
        
        // 현재 활성화된 탭의 내용 리프레시
        if (panelStatus.activeSelf) RefreshStatus();
        if (panelInventory.activeSelf) RefreshInventory();
    }

    private void RefreshStatus()
    {
        if (statusTab != null) statusTab.Refresh(GameManager.Instance.State.fighter);
    }

    private void RefreshInventory()
    {
        if (inventoryTab != null) inventoryTab.Refresh(GameManager.Instance.State.inventory);
    }
}
