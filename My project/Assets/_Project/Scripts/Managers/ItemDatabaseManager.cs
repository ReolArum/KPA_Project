// ===== ItemDatabaseManager.cs =====
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabaseManager : MonoBehaviour
{
    public static ItemDatabaseManager Instance { get; private set; }

    [Header("Item Registry")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    private Dictionary<string, ItemData> itemDict = new Dictionary<string, ItemData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        itemDict.Clear();
        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId))
            {
                itemDict[item.itemId] = item;
            }
        }
        Debug.Log($"[ItemDatabase] Initialized with {itemDict.Count} items.");
    }

    public ItemData GetItem(string itemId)
    {
        if (itemDict.TryGetValue(itemId, out var item))
            return item;
        return null;
    }

    // 인스펙터 수정 시 런타임에 즉시 반영하기 위한 헬퍼 (테스트용)
    public void Refresh() => Initialize();
}
