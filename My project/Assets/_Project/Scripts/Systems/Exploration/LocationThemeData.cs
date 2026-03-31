using UnityEngine;

[CreateAssetMenu(fileName = "NewLocationTheme", menuName = "KPA/UI/LocationThemeData")]
public class LocationThemeData : ScriptableObject
{
    public MapLocation location;
    public string      locationName;
    public Color       themeColor = Color.cyan;
    
    [Header("Visuals")]
    public Sprite backgroundSprite;    // 장소 배경 이미지 (더미용)
    public Sprite iconSprite;          // 장소 아이콘 (더미용)

    [Header("Display Settings")]
    public bool showTalkButton = true;
    public bool showInvestigateButton = false;
    public bool showShopButton = false;
    public bool showQuestButton = false;
    public bool showRestButton = false;

    [TextArea]
    public string description = "이 장소에 대한 설명입니다.";
}
