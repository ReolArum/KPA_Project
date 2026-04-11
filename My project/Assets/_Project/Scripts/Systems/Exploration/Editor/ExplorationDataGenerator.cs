using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ExplorationDataGenerator
{
    [MenuItem("KPA/Generate Sample Stage")]
    public static void Generate()
    {
        // ScriptableObject 생성
        ExplorationStageData stage = ScriptableObject.CreateInstance<ExplorationStageData>();
        stage.stageName = "테스트 구역 (실험실)";
        stage.limitTime = 120f;
        stage.maxEnemyTickets = 5;
        
        // 1. 시작 노드 추가
        stage.nodes.Add(new DialogueNodeData { 
            nodeId = "Start", 
            worldPosition = Vector3.zero 
        });
        
        // 2. 이벤트 노드 추가 (Hazard)
        var eventNode = new DialogueNodeData { 
            nodeId = "Hazard_Event", 
            worldPosition = new Vector3(5, 0, 0),
            eventType = ExplorationEventType.Hazard
        };
        
        // 선택지 1: 우회
        var choice1 = new DialogueChoiceData { 
            label = "조심해서 우회하기"
        };
        choice1.effects.Add(new DialogueEffect { type = DialogueEffectType.Time, amount = 15 });
        eventNode.choices.Add(choice1);
        
        // 선택지 2: 강행
        var choice2 = new DialogueChoiceData { 
            label = "강행 돌파 (위험)"
        };
        choice2.effects.Add(new DialogueEffect { type = DialogueEffectType.Time, amount = 5 });
        choice2.effects.Add(new DialogueEffect { type = DialogueEffectType.Gold, amount = 50 });
        eventNode.choices.Add(choice2);
        
        stage.nodes.Add(eventNode);
        
        // 3. 탈출 노드 추가
        stage.nodes.Add(new DialogueNodeData { 
            nodeId = "Exit", 
            worldPosition = new Vector3(10, 0, 0),
            eventType = ExplorationEventType.Exit 
        });

        // 폴더 생성 확인
        if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects/Exploration"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            
            AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Exploration");
        }

        // 에셋 저장
        string path = "Assets/_Project/ScriptableObjects/Exploration/SampleExplorationStage.asset";
        AssetDatabase.CreateAsset(stage, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"탐사 샘플 데이터가 성공적으로 생성되었습니다: {path}");
        
        // 생성된 에셋 포커스
        Selection.activeObject = stage;
    }
}
