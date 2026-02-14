using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Quest
{
    public int id;
    public string title;
    public string description;
    public MapLocation pickupLocation;   // 수령 장소
    public MapLocation deliverLocation;  // 배달 장소
    public int goldReward;
    public bool isAccepted;
    public bool isCompleted;
}

[Serializable]
public class QuestSystem
{
    public List<Quest> availableQuests = new();
    public List<Quest> activeQuests = new();
    public List<Quest> completedQuests = new();

    public void GenerateDailyQuests(int day)
    {
        availableQuests.Clear();

        // 매일 2~3개 의뢰 생성 (더미)
        var locations = new[] { MapLocation.Shop, MapLocation.TrainingGround, MapLocation.Cafe, MapLocation.InvestigationHQ };

        int count = 2 + (day % 2); // 2~3개

        for (int i = 0; i < count; i++)
        {
            var pickup = locations[(day + i) % locations.Length];
            var deliver = locations[(day + i + 1) % locations.Length];

            // 수령과 배달이 같으면 다른 곳으로
            if (pickup == deliver)
                deliver = locations[(day + i + 2) % locations.Length];

            availableQuests.Add(new Quest
            {
                id = day * 100 + i,
                title = $"배달 의뢰 #{day}-{i + 1}",
                description = $"{GetLocationName(pickup)}에서 물건을 받아 {GetLocationName(deliver)}에 전달",
                pickupLocation = pickup,
                deliverLocation = deliver,
                goldReward = 8 + i * 4,
                isAccepted = false,
                isCompleted = false
            });
        }
    }

    public bool AcceptQuest(int questId)
    {
        var quest = availableQuests.Find(q => q.id == questId);
        if (quest == null) return false;

        quest.isAccepted = true;
        activeQuests.Add(quest);
        availableQuests.Remove(quest);
        return true;
    }

    public Quest CheckDelivery(MapLocation currentLocation)
    {
        var quest = activeQuests.Find(q => q.deliverLocation == currentLocation && q.isAccepted && !q.isCompleted);
        return quest;
    }

    public void CompleteQuest(Quest quest)
    {
        quest.isCompleted = true;
        activeQuests.Remove(quest);
        completedQuests.Add(quest);
    }

    public static string GetLocationName(MapLocation loc) => loc switch
    {
        MapLocation.Home => "집",
        MapLocation.Shop => "상점",
        MapLocation.InvestigationHQ => "조사 거점",
        MapLocation.TrainingGround => "훈련소",
        MapLocation.Cafe => "카페",
        MapLocation.QuestBoard => "의뢰 게시판",
        _ => "?"
    };
}
