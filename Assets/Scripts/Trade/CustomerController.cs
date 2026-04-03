using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    private enum State
    {
        Idle,
        Attracting,
        Buying,
        Quit
    }
    private State _state;

    private ShelfContainer shelfContainer;

    //private bool isBuyying = false;     // 是不是正在买
    //private bool canAttract = false;    // 可不可被吸引, 可能会被删, 现在是临时条件

    // 需要从ShelfContainer里要什么商品, 要几个, 要完设个bool可以离开 canLeave

    // 根据玩家态度(待在原地时长)和获得质量给钱 - 需要设计函数

    [SerializeField] private const float startAttitude = 100;    // 初始态度
    private float attractAttitude;
    private float buyAttitude;

    // 买东西最低态度基于初始态度的比例
    [SerializeField] private float minBuyAttitudeFactor = 0.5f;

    private ComplexTime startAttractTime;
    private ComplexTime startBuyTime;

    // 最低忍耐时长(无列表长度修正), 超过则开始减态度
    [SerializeField] private float minWaitingTime_Attract = 3;
    [SerializeField] private float minWaitingTime_Buy;

    // 最高忍耐时长(无列表长度修正), 超过则退出
    [SerializeField] private float maxWaitingTime_Attract = 10;
    [SerializeField] private float maxWaitingTime_Buy;

    private void Awake()
    {
        //isBuyying = false;
        //canAttract = false;
    }

    private void Start()
    {
        ChangeState(State.Idle);
    }

    private void Update()
    {
        TickState();
    }

    private void ChangeState(State newState)
    {
        ExitState(_state);
        _state = newState;
        EnterState(_state);
    }

    private void EnterState(State state)
    {
        switch (state)
        {
            case State.Idle:
                break;
            case State.Attracting:
                // 进行时执行Attract()
                Attract();
                break;
            case State.Buying:
                // 进行时执行Buy()
                Buy();
                break;
            case State.Quit:
                // 进行时执行HaveBought()
                HaveBought();
                break;

        }
    }

    private void ExitState(State state)
    {
        switch (state)
        {
            case State.Attracting:
                break;
            case State.Buying:
                break;
        }
    }

    private void TickState()
    {
        switch (_state)
        {
            case State.Idle:
                TickIdle();
                break;
            case State.Attracting:
                TickAttracting();
                break;
            case State.Buying:
                TickBuying();
                break;
        }
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
    
    // CanAttract() 持续
    // Attract() 一次
    // Attracting() 持续 -> LoseAttract() 一次
    //              或者 -> Buy() 一次

    // Buy() 一次
    // Buyying() 持续 -> HaveBought() 一次

    public void TickIdle()
    {
        // 顾客到货架一定范围内随机被吸引, 如果摇铃一定被吸引
        // canAttract = ...
        // 里面要放寻路逻辑
        ChangeState(State.Attracting);
    }

    public void Attract()
    {
        Trade_Customer.Instance.Attract(this);
        
        startAttractTime = TimeManager.Instance.GetComplexTime();
        attractAttitude = 100;
    }

    public void TickAttracting()
    {
        // todo: 写等待执行 -> LostAttract() 还是 Buy() 逻辑
        // if a
        LoseAttract();
        ChangeState(State.Idle);
        // if b
        Buy();
        ChangeState(State.Buying);

    }

    // customer在List里比较远 + 等待时长久 执行LoseAttract()
    public void LoseAttract()
    {
        Trade_Customer.Instance.AttractExit(this);
    }

    // customer在Buy列表里等待时长久, 态度变差, 给钱少
    public void Buy()
    {
        Trade_Customer.Instance.Buy(this);
        Trade_Customer.Instance.AttractExit(this);
        
        startBuyTime = TimeManager.Instance.GetComplexTime();
        buyAttitude = startAttitude;
        // todo: 根据Buy队列前几个的意图推断自己的意图, 从ShelfContainer里找Item
        //
        //var need = shelfContainer.GetContainer().Items[0];

    }

    public void TickBuying()
    {
        BuyWill();
    }

    public void HaveBought()
    {
        Trade_Customer.Instance.BuyExit(this);
        //canAttract = false;
    }



    // 吸引欲望随入队顺序及时间变化函数 (后期可能需要同其他类似函数放到一个类里)
    // 队列少一个人欲望再升一段
    private void AttractWill()
    {
        int currentListPlace = Trade_Customer.Instance.AttractPlace(this);
    }

    // 购买欲望(付钱的多少) 随时间变化函数
    private void BuyWill()
    {
        float t = TimeManager.Instance.TimeDistToNow(startBuyTime);
        var c = minBuyAttitudeFactor;
        var T = maxWaitingTime_Buy;
        var a = (1 - c) * startAttitude / (T * T);
        if (t < T)
            buyAttitude = startAttitude - a * t * t;
        else
            buyAttitude = startAttitude * c;
        // 钱随buyAttitude的函数

    }
    
}
