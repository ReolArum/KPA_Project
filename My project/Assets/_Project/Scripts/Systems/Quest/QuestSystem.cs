using System;
using System.Collections.Generic;

[Serializable]
public class Quest
{
    public int id;
    public string questId; // QuestData SO 식별용
    public string title;
    public string description;
    public MapLocation pickupLocation;  
    public MapLocation deliverLocation; 
    public int goldReward;
    public int repReward;
    public bool isAccepted;
    public bool isCompleted;
}

[Serializable]
public class QuestSystem
{
    public List<Quest> availableQuests = new();
    public List<Quest> activeQuests = new();
    public List<Quest> completedQuests = new();
    
    // 로직은 모두 QuestManager로 이동됨
}
