using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRegistry : MonoBehaviour
{
    public static ItemDatabaseSO DB_Item { get; private set; }
    public static TileDatabaseSO DB_Tile { get; private set; }

    public static void Init(ItemDatabaseSO db_Item, TileDatabaseSO db_Tile)
    {
        DB_Item = db_Item;
        DB_Item.BuildCache();

        DB_Tile = db_Tile;
        DB_Tile.BuildCache();
    }

    public static ItemBase_SO Get(int id) => DB_Item.Get(id);
    public static ItemBase_SO Get(string id) => DB_Item.Get(id);
    public static TileBase_SO GetTile(int id) => DB_Tile.Get(id);
    public static TileBase_SO GetTile(string id) => DB_Tile.Get(id);


}