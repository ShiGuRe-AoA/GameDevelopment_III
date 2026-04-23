using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class ActionPair
{
    public string ActionState;
    public ActionDefinition_SO ActionDefinition;
}

public class StateMachineBrain : MonoBehaviour
{
    public static StateMachineBrain Instance { get; private set; }

    public List<ActionPair> PlayerRegistry_Raw = new();
    public Dictionary<string, ActionDefinition_SO> PlayerRegistry;//HashAnimationState - Def

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple StateMachineBrain instances detected.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadActionRegistry();
    }
    //public List<IStateMachineRunner> stateMachines = new List<IStateMachineRunner>();
    private Dictionary<Transform, IStateMachineRunner> statemachine_Dict = new();

    public void LoadActionRegistry()
    {
        foreach(var raw in PlayerRegistry_Raw)
        {
            if(raw.ActionState != string.Empty)
            {
                PlayerRegistry.Add(raw.ActionState, raw.ActionDefinition);
            }
        }
    }
    public bool RegistryMachine(IStateMachineRunner newEvent, Transform obj)
    {
        if (statemachine_Dict.ContainsKey(obj))
        {
            Debug.Log($"Multiple Machine Detected: {obj.name}");
            return false;
        }

        statemachine_Dict.Add(obj, newEvent);

        return true;
    }

    public bool TryGetStatemachine(Transform obj, out IStateMachineRunner outcome)
    {
        return statemachine_Dict.TryGetValue(obj, out outcome);
    }

    private void Update()
    {
        foreach(var pair in statemachine_Dict)
        {
            pair.Value.Update();
        }
    }
    private void FixedUpdate()
    {
        foreach (var pair in statemachine_Dict)
        {
            pair.Value.FixedUpdate();
        }
    }
}
