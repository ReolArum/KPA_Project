using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private TMP_Text textCount;
    [SerializeField] private GameObject selectionHighlight;

    public void SetItem(InventoryItem item, System.Func<string, ItemData> itemLookup)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        ItemData data = itemLookup?.Invoke(item.itemDataId);
        if (data != null)
        {
            imgIcon.sprite = data.icon;
            imgIcon.enabled = true;
            textCount.text = item.count > 1 ? item.count.ToString() : "";
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        imgIcon.enabled = false;
        textCount.text = "";
        selectionHighlight.SetActive(false);
    }
}
