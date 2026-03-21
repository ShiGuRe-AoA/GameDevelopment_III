using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRegistry : MonoBehaviour
{
    public static ItemDatabaseSO DB_Item { get; private set; }

    public static void Init(ItemDatabaseSO db_Item)
    {
        DB_Item = db_Item;
        DB_Item.BuildCache();
    }

    public static ItemBase_SO Get(int id) => DB_Item.Get(id);
    public static ItemBase_SO Get(string id) => DB_Item.Get(id);


}