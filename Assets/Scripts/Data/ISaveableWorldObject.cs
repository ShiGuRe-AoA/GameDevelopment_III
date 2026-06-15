using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveableWorldObject
{
    WorldObjectSaveData Save();
    void Load(WorldObjectSaveData data);
}
