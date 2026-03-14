using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game/Tile/TileDataSO")]
public class TileBase_SO : ScriptableObject
{
    public int ID_Num;
    public string ID_Str;
    public TileBase Tile;
}
