using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    [SerializeField] private ShelfContainer shelfContainer;

    // 加个bool是否被玩家吸引 isAttracted
    // 需要从ShelfContainer里要什么商品, 要几个, 要完设个bool可以离开 canLeave

    public void Init(ShelfContainer _shelfContainer)
    {
        this.shelfContainer = _shelfContainer;
    }

    // todo:被吸引来和离开的函数


    
}
