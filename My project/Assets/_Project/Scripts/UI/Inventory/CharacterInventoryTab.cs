using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInventoryTab : MonoBehaviour
{
    [Header("Grid Setup")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Category Filters")]
    [SerializeField] private Button btnFilterAll;
    [SerializeField] private Button btnFilterConsumable;
    [SerializeField] private Button btnFilterGift;
    [SerializeField] private Button btnFilterInfo;
    [SerializeField] private Button btnFilterPart;

    private List<InventorySlotUI> _slots = new List<InventorySlotUI>();
    private ItemCategory? _currentFilter = null;

    private void Awake()
    {
        btnFilterAll.onClick.AddListener(() => SetFilter(null));
        btnFilterConsumable.onClick.AddListener(() => SetFilter(ItemCategory.Consumable));
        btnFilterGift.onClick.AddListener(() => SetFilter(ItemCategory.Gift));
        btnFilterInfo.onClick.AddListener(() => SetFilter(ItemCategory.Info));
        btnFilterPart.onClick.AddListener(() => SetFilter(ItemCategory.Part));
    }

    private void SetFilter(ItemCategory? category)
    {
        _currentFilter = category;
        Refresh(GameManager.Instance.State.inventory);
    }

    public void Refresh(Inventory inventory)
    {
        // 1. 슬롯 풀링/생성
        int needed = Mathf.Max(inventory.items.Count, 20); // 최소 20칸 노출
        while (_slots.Count < needed)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            _slots.Add(go.GetComponent<InventorySlotUI>());
        }

        // 2. 데이터 필터링 및 출력 (아이템 데이터 룩업 로직은 추후 ItemDatabase에 연결)
        var displayItems = (_currentFilter == null) ? inventory.items : inventory.GetItemsByCategory(_currentFilter.Value, null);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < displayItems.Count)
                _slots[i].SetItem(displayItems[i], null); // TODO: ItemData 룩업 함수 연결
            else
                _slots[i].Clear();
        }
    }
}
