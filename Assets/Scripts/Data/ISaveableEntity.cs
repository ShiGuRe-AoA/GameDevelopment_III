using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveableEntity
{
    EntitySaveData Save();
    void Load(EntitySaveData data);
}