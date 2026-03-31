// ===== EquipmentDatabase.cs =====
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Combat/Equipment Database")]
public class EquipmentDatabase : ScriptableObject
{
    public List<EquipmentData> allEquipment = new List<EquipmentData>();

    public List<EquipmentData> GetBySlot(EquipSlot slot)
    {
        return allEquipment.FindAll(e => e.slot == slot);
    }

    public List<EquipmentData> GetByGrade(EquipmentGrade grade)
    {
        return allEquipment.FindAll(e => e.grade == grade);
    }

    public List<EquipmentData> GetShopItems()
    {
        return allEquipment.FindAll(e => e.buyPrice > 0);
    }

    public List<EquipmentData> GetExplorationRewards()
    {
        return allEquipment.FindAll(e => e.isExplorationReward);
    }
}
