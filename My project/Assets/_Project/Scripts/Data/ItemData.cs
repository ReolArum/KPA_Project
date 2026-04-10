using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "KPA/Item/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemId;
    public string itemName;
    public string itemDescription;
    public Sprite icon;

    [Header("Settings")]
    public ItemCategory category;
    public int maxStack = 99;
    public int price = 100;

    [Header("Effects")]
    public float statBonusAmount; // 예: 소모품 사용 시 회복량 등
}
