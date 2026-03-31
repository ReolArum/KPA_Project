using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlaceActionUIController : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Image imgBackground;
    [SerializeField] private Image imgThemeOverlay;
    [SerializeField] private TMP_Text textPlaceTitle;
    [SerializeField] private TMP_Text textPlaceDescription;

    [Header("Buttons")]
    [SerializeField] private Button btnTalk;
    [SerializeField] private Button btnBuy;
    [SerializeField] private Button btnSell;
    [SerializeField] private Button btnRest;
    [SerializeField] private Button btnUpgrade;
    [SerializeField] private Button btnSupport;
    [SerializeField] private Button btnFood;
    [SerializeField] private Button btnReroll;
    [SerializeField] private Button btnAcceptQuest;
    [SerializeField] private Button btnDeliverQuest;
    [SerializeField] private Button btnBack;

    [Header("Theme Data")]
    [SerializeField] private List<LocationThemeData> themes;

    private Dictionary<MapLocation, LocationThemeData> themeDict = new();

    void Awake()
    {
        foreach (var t in themes)
            themeDict[t.location] = t;
    }

    public void Refresh(GameState state)
    {
        if (state == null) return;
        
        MapLocation loc = state.playerLocation;
        if (!themeDict.ContainsKey(loc)) return;

        var theme = themeDict[loc];

        // 1. 시각적 테마 적용
        if (textPlaceTitle) textPlaceTitle.text = theme.locationName;
        if (textPlaceDescription) textPlaceDescription.text = theme.description;
        if (imgThemeOverlay) imgThemeOverlay.color = new Color(theme.themeColor.r, theme.themeColor.g, theme.themeColor.b, 0.2f);
        if (imgBackground && theme.backgroundSprite) imgBackground.sprite = theme.backgroundSprite;

        // 2. 버튼 활성화 제어 (장소별 스펙 반영)
        btnTalk.gameObject.SetActive(theme.showTalkButton);
        btnBuy.gameObject.SetActive(theme.showShopButton);
        btnSell.gameObject.SetActive(theme.showShopButton); // 상점이면 같이 노출
        btnUpgrade.gameObject.SetActive(theme.showInvestigateButton || loc == MapLocation.TrainingGround); // 훈련장 업그레이드
        btnFood.gameObject.SetActive(loc == MapLocation.Cafe);
        btnRest.gameObject.SetActive(theme.showRestButton);
        btnReroll.gameObject.SetActive(theme.showQuestButton); // 게시판 리롤

        // 3. 특수 조건 버튼 제어
        if (loc == MapLocation.TrainingGround)
        {
            // 훈련 보조는 전투체가 실제 훈련 중일 때만 노출
            bool isTraining = state.fighterSchedule[state.fighterSlotProgress].type == FighterSlotType.Training;
            btnSupport.gameObject.SetActive(isTraining);
            btnUpgrade.gameObject.SetActive(true);
        }
        else
        {
            btnSupport.gameObject.SetActive(false);
        }

        // 퀘스트 관련
        bool canDeliver = state.quests.CheckDelivery(loc) != null;
        btnDeliverQuest.gameObject.SetActive(canDeliver);
        btnAcceptQuest.gameObject.SetActive(theme.showQuestButton);
    }
}
