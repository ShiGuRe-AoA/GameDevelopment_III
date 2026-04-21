using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StateMachineBrain : MonoBehaviour
{
    public static StateMachineBrain Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple StateMachineBrain instances detected.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    //public List<IStateMachineRunner> stateMachines = new List<IStateMachineRunner>();
    private Dictionary<Transform, IStateMachineRunner> statemachine_Dict = new();

    public bool RegistryMachine(IStateMachineRunner newEvent, Transform obj)
    {
        if (statemachine_Dict.ContainsKey(obj))
        {
            Debug.Log($"Multy Machine Detected: {obj.name}");
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
