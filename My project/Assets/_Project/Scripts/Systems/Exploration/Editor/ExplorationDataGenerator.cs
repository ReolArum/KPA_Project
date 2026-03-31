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
        stage.maxChoices = 5;
        
        // 1. 시작 노드 추가
        stage.nodes.Add(new ExplorationNodeData { 
            nodeId = "Start", 
            worldPosition = Vector3.zero 
        });
        
        // 2. 이벤트 노드 추가 (Hazard)
        var eventNode = new ExplorationNodeData { 
            nodeId = "Hazard_Event", 
            worldPosition = new Vector3(5, 0, 0),
            eventType = ExplorationEventType.Hazard
        };
        
        // 선택지 1: 우회
        eventNode.choices.Add(new ExplorationChoiceData { 
            label = "조심해서 우회하기", 
            timePenalty = 15,
            goldReward = 0
        });
        
        // 선택지 2: 강행
        eventNode.choices.Add(new ExplorationChoiceData { 
            label = "강행 돌파 (위험)", 
            timePenalty = 5,
            goldReward = 50
        });
        
        stage.nodes.Add(eventNode);
        
        // 3. 탈출 노드 추가
        stage.nodes.Add(new ExplorationNodeData { 
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
