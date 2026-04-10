using UnityEngine;
using KPA.Character;

public class VisualModule : ICharacterModule
{
    private CharacterBase _owner;
    private GameObject _modelPrefab;
    private GameObject _currentModel;

    public VisualModule(GameObject modelPrefab)
    {
        _modelPrefab = modelPrefab;
    }

    public void Initialize(CharacterBase owner)
    {
        _owner = owner;
        SpawnModel();
    }

    private void SpawnModel()
    {
        if (_modelPrefab == null) return;
        
        _currentModel = Object.Instantiate(_modelPrefab, _owner.transform);
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.identity;
    }

    public GameObject GetModel() => _currentModel;

    public void OnUpdate() { }
    public void OnFixedUpdate() { }
}
