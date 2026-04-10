using System;
using System.Collections.Generic;
using UnityEngine;
using KPA.Character;

public abstract class CharacterBase : MonoBehaviour
{
    private Dictionary<Type, ICharacterModule> _modules = new Dictionary<Type, ICharacterModule>();

    protected virtual void Awake()
    {
        InitializeModules();
    }

    protected abstract void InitializeModules();

    protected void AddModule<T>(T module) where T : ICharacterModule
    {
        var type = typeof(T);
        if (!_modules.ContainsKey(type))
        {
            _modules.Add(type, module);
            module.Initialize(this);
        }
    }

    public T GetModule<T>() where T : class, ICharacterModule
    {
        if (_modules.TryGetValue(typeof(T), out var module))
        {
            return module as T;
        }
        return null;
    }

    protected virtual void Update()
    {
        foreach (var module in _modules.Values)
        {
            module.OnUpdate();
        }
    }

    protected virtual void FixedUpdate()
    {
        foreach (var module in _modules.Values)
        {
            module.OnFixedUpdate();
        }
    }
}
