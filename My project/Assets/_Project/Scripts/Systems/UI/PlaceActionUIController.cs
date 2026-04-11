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
            
        btnTalk.onClick.AddListener(OnTalkClicked);
        btnBuy.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.BuyItem));
        btnSell.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.SellItem));
        btnRest.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.Rest));
        btnUpgrade.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.UpgradeFacility));
        btnSupport.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.SupportTraining));
        btnFood.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.BuyFood));
        btnReroll.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.RerollQuests));
        btnDeliverQuest.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.DeliverQuest));
        btnAcceptQuest.onClick.AddListener(() => GameManager.Instance.OnClickPlaceAction((int)PlaceActionType.AcceptQuest));
        btnBack.onClick.AddListener(() => GameManager.Instance.OnClickBackToMap());
    }

    private void OnTalkClicked()
    {
        // 1. 현재 장소 테마 데이터 룩업
        MapLocation loc = GameManager.Instance.State.player.location;
        if (!themeDict.ContainsKey(loc)) return;

        var theme = themeDict[loc];
        
        // 2. 테마에 설정된 대화 노드가 있다면 범용 DialogueManager를 통해 VN 대화창 호출
        if (theme.talkNode != null)
        {
            DialogueManager.Instance.StartDialogue(theme.talkNode);
        }
        else
        {
            GameEvents.RaiseActionResult($"{theme.locationName}의 담당자와 대화를 나눕니다. (지정된 대화 노드 없음)");
        }
    }

    public void Refresh(GameState state)
    {
        if (state == null) return;
        
        MapLocation loc = state.player.location;
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
        btnSell.gameObject.SetActive(theme.showShopButton);
        btnUpgrade.gameObject.SetActive(theme.showInvestigateButton || loc == MapLocation.TrainingGround || loc == MapLocation.Base); 
        btnFood.gameObject.SetActive(loc == MapLocation.Cafe);
        btnRest.gameObject.SetActive(theme.showRestButton);
        btnReroll.gameObject.SetActive(theme.showQuestButton); // 게시판 리롤

        // 3. 특수 조건 버튼 제어
        if (loc == MapLocation.TrainingGround)
        {
            // 훈련 보조는 전투체가 실제 훈련 중일 때만 노출 (인덱스 버그 방지)
            bool isTraining = false;
            if (state.fighter.slotProgress < GameState.DaySlotCount)
            {
                isTraining = state.fighter.schedule[state.fighter.slotProgress].type == FighterSlotType.Training;
            }
            btnSupport.gameObject.SetActive(isTraining);
            btnUpgrade.gameObject.SetActive(true);
        }
        else
        {
            btnSupport.gameObject.SetActive(false);
        }

        // 퀘스트 관련
        bool canDeliver = QuestManager.Instance != null && QuestManager.Instance.CheckDelivery(loc) != null;
        btnDeliverQuest.gameObject.SetActive(canDeliver);
        btnAcceptQuest.gameObject.SetActive(theme.showQuestButton);
    }
}
