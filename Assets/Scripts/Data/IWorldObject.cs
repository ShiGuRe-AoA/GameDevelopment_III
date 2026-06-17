using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWorldObject
{
    int ObjectId { get; }
    Vector3 WorldPos { get; }

    void ObjectInit(int objectId, Vector3 worldPos, WorldState worldState);
}
