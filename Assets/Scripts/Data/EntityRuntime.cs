using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityRuntime : MonoBehaviour
{
    [HideInInspector] public int EntityId;
    [HideInInspector] public WorldState WorldState;
    public abstract void Save();//TODO：这玩意肯定不长这样，有参数
    public abstract void Load();//TODO：这玩意肯+定不长这样，有返回类型
}
