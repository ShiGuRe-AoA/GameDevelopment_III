using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    private ShelfContainer shelfContainer;

    private bool isBuyying = false;     // 是不是正在买
    private bool canAttract = false;    // 可不可被吸引

    // 需要从ShelfContainer里要什么商品, 要几个, 要完设个bool可以离开 canLeave
    // 根据玩家态度(待在原地时长)和获得质量给钱 - 需要设计函数

    private void Awake()
    {
        isBuyying = false;
        canAttract = false;
    }

    public void Init(ShelfContainer _shelfContainer)
    {
        this.shelfContainer = _shelfContainer;
    }

    // todo:被吸引来和离开的函数
    // 或者顾客每次到某些范围内就自动进入一个可被吸引的List,其中有的不会直接排队
    // Trade 部分应该会另起一个类, 将顾客放到 List 里, 然后顾客再引这个list看前面有多少个, 随人数和时间欲望递减
    // 可能用 WorldState 检测站在某个单元格上的顾客可以买

    // canAttract + customer走到一定范围内再执行 Attract()
    public void Attract()
    {
        Trade_Customer.Instance.Attract(this);
        canAttract = false;
    }

    // customer在List里比较远 + 等待时长久 执行LoseAttract()
    public void LoseAttract()
    {
        Trade_Customer.Instance.AttractExit(this);
        canAttract = true;
    }

    // customer在Buy列表里等待时长久, 态度变差, 给钱少
    public void Buyying()
    {
        Trade_Customer.Instance.Buy(this);
        Trade_Customer.Instance.AttractExit(this);
        // todo: 根据Buy队列前几个的意图推断自己的意图, 从ShelfContainer里找Item
        //
        var need = shelfContainer.GetContainer().Items[0];
    }

    public void HaveBought()
    {
        Trade_Customer.Instance.BuyExit(this);
        canAttract = false;
    }

    
}
