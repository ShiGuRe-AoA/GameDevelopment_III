using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WorldObjectSaveData
{
    public int ObjectId;
    public Vector3 WorldPos;
}

public struct WorldObjectRuntimeData
{
    public int ObjectId;
    public Vector3 WorldPos;
}

public static class WorldObjectSaveUtility 
{
    public static WorldObjectSaveData SaveBase(IWorldObject obj)
    {
        return new WorldObjectSaveData
        {
            ObjectId = obj.ObjectId,
            WorldPos = obj.WorldPos,
        };
    }   

    public static void LoadBase(WorldObjectSaveData data, WorldObjectRuntimeData target)
    {
        target.ObjectId = data.ObjectId;
        target.WorldPos = data.WorldPos;
    }
}