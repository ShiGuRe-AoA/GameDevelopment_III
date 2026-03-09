using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRegistry : MonoBehaviour
{
    public static ItemDatabaseSO DB { get; private set; }

    public static void Init(ItemDatabaseSO db)
    {
        DB = db;
        DB.BuildCache();
    }

    public static ItemBase_SO Get(int id) => DB.Get(id);
}