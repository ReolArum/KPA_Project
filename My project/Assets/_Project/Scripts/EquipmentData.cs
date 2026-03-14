// ===== EquipmentData.cs =====
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Combat/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("기본 정보")]
    public string equipName;
    public EquipSlot slot;
    public Sprite icon;
    [TextArea] public string description;

    [Header("등급")]
    public EquipmentGrade grade = EquipmentGrade.Common;

    [Header("스탯 보너스")]
    public int bonusSTR;
    public int bonusAGI;
    public int bonusVIT;
    public int bonusINT;
    public int bonusGUT;
    public int bonusSEN;

    [Header("특수 효과")]
    [TextArea] public string passiveDescription;
    public SchoolBonus specialBonus;        // ★ 추가: 특수 보너스

    [Header("획득 경로")]
    public int buyPrice;                    // ★ 추가: 상점 가격 (0이면 비매품)
    public bool isExplorationReward;        // ★ 추가: 탐사 보상
    public bool isEventReward;              // ★ 추가: 이벤트 보상

    public void ApplyTo(CombatBaseStats stats)
    {
        stats.STR += bonusSTR;
        stats.AGI += bonusAGI;
        stats.VIT += bonusVIT;
        stats.INT += bonusINT;
        stats.GUT += bonusGUT;
        stats.SEN += bonusSEN;
    }
}
