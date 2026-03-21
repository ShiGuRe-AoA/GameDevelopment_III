using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ItemFeature : ScriptableObject
{
    protected ItemBase_SO parent;

    public void Init(ItemBase_SO parent)
    {
        this.parent = parent;
    }
}
[CreateAssetMenu(menuName = "Game/Item/ItemBaseSO")]
public class ItemBase_SO : ScriptableObject
{
    public int ID_num;  //机器阅读的数字ID用于哈希查找
    public string ID_str;   //开发者使用的字符ID避免混淆
    public string Name; //UI名称
    public int StackAmount; //最大堆叠
    public Sprite ItemSprite;   //物品贴图，暂定同时用于物品栏显示和掉落物显示

    public List<ScriptableObject> Features; //物品类别

    public T GetFeature<T>() where T : ItemFeature
    {
        foreach (var f in Features)
        {
            if (f is T typed)
                return typed;
        }
        return null;
    }
    public void Init()
    {
        foreach (var f in Features)
        {
            if (f is ItemFeature itf)
                itf.Init(this);
        }
    }
}
