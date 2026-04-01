using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    [SerializeField] private ShelfContainer shelfContainer;

    private bool isBuyying = false;     // 是不是正在买
    private bool haveBought = false;    // 买没买过东西
    private bool isAttracted = false;   // 被没被吸引来

    // 需要从ShelfContainer里要什么商品, 要几个, 要完设个bool可以离开 canLeave
    // 根据玩家态度(待在原地时长)和获得质量给钱 - 需要设计函数

    private void Awake()
    {
        isBuyying = false;
        haveBought = false;
        isAttracted = false;
    }

    public void Init(ShelfContainer _shelfContainer)
    {
        this.shelfContainer = _shelfContainer;
    }

    // todo:被吸引来和离开的函数
    // 或者顾客每次到某些范围内就自动进入一个可被吸引的List,其中有的不会直接排队
    // Trade部分应该会另起一个类, 将顾客放到List里, 然后顾客再引这个list看前面有多少个,随人数和时间欲望递减
    // 可能用WorldState检测站在某个单元格上的顾客可以买
    public void Attract()
    {

    }

    public void Buy()
    {

    }

    public void Leave()
    {

    }

    
}
