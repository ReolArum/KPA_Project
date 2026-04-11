using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public string itemDataId; // ScriptableObject ID or Name
    public int count;

    public InventoryItem(string id, int count)
    {
        this.itemDataId = id;
        this.count = count;
    }
}

[Serializable]
public class Inventory
{
    public List<InventoryItem> items = new List<InventoryItem>();
    public const int MaxSlots = 25; // 임시 고정 크기

    public void AddItem(string itemId, int amount, int maxStack)
    {
        // 1. 기존 스택에 합치기 시도
        foreach (var item in items)
        {
            if (item.itemDataId == itemId && item.count < maxStack)
            {
                int canAdd = maxStack - item.count;
                int toAdd = Mathf.Min(canAdd, amount);
                item.count += toAdd;
                amount -= toAdd;
                if (amount <= 0) return;
            }
        }

        // 2. 남은 수량이 있으면 새로운 슬롯 생성
        while (amount > 0 && items.Count < MaxSlots)
        {
            int toAdd = Mathf.Min(amount, maxStack);
            items.Add(new InventoryItem(itemId, toAdd));
            amount -= toAdd;
        }
    }

    public void RemoveItem(string itemId, int amount)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].itemDataId == itemId)
            {
                int toRemove = Mathf.Min(items[i].count, amount);
                items[i].count -= toRemove;
                amount -= toRemove;
                if (items[i].count <= 0) items.RemoveAt(i);
                if (amount <= 0) return;
            }
        }
    }

    public bool HasItem(string itemId, int count = 1)
    {
        int total = 0;
        foreach (var item in items)
        {
            if (item.itemDataId == itemId)
            {
                total += item.count;
                if (total >= count) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 카테고리의 아이템들만 필터링하여 반환
    /// </summary>
    public List<InventoryItem> GetItemsByCategory(ItemCategory category, System.Func<string, ItemData> itemLookup)
    {
        List<InventoryItem> filtered = new List<InventoryItem>();
        foreach (var item in items)
        {
            ItemData data = itemLookup?.Invoke(item.itemDataId);
            if (data != null && data.category == category)
            {
                filtered.Add(item);
            }
        }
        return filtered;
    }
}
