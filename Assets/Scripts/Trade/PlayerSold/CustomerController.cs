using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomerController : MonoBehaviour, ITickUpdatable, IMinuteUpdatable
{
    // todo: 加寻路逻辑

    private enum State
    {
        Idle,
        Attracting,
        Buying,
        Quit
    }
    private State _state;

    private PlayerStoreContainer playerStore;

    // 需要从ShelfContainer里要什么商品, 要几个, 要完设个bool可以离开 canLeave0

    // 根据玩家态度(待在原地时长)和获得质量给钱 - 需要设计函数

    [SerializeField] private const float startAttitude = 1;    // 初始态度
    private float attractAttitude;
    private float buyAttitude;

    // 买东西最低态度基于初始态度的比例
    [SerializeField] private float minBuyAttitudeFactor = 0.5f;


    // 吸引排队时间和购买排队时间
    //private ComplexTime startAttractTime;
    //private ComplexTime startBuyTime;
    private float attractingTime;
    private float buyingTime;

    // 最低忍耐时长(无列表长度修正), 超过则开始减态度
    [SerializeField] private float minWaitingTime_Attract = 3;
    [SerializeField] private float minWaitingTime_Buy;

    // 最高忍耐时长(无列表长度修正), 超过则退出
    [SerializeField] private float maxWaitingTime_Attract = 10;
    [SerializeField] private float maxWaitingTime_Buy;

    // 需要买的, 价格及数量
    [SerializeField] private int price;
    [SerializeField] private int count;
    private int curPrice;

    [SerializeField] private ItemStack buyItem;

    private void Awake()
    {
        
    }

    private void Start()
    {
        ChangeState(State.Idle);
    }

    public void OnTickUpdate(float deltaTime)
    {
        // 这里的 TickState 有大概率要改成用 OnMinuteUpdate() 更新
        TickState();
    }

    public void OnMinuteUpdate()
    {
        MinuteState();
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

    private void MinuteState()
    {
        switch (_state)
        {
            case State.Attracting:
                MinuteAttracting();
                break;
            case State.Buying: 
                // todo: MinuteBuying();
                break;
        }
    }
    public void Init(PlayerStoreContainer _playerStore)
    {
        this.playerStore = _playerStore;
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

    // 如果售罄也要 HaveBought()


    // Tick感觉可以做寻路运动之类的, Minute用于计算态度函数之类的
    // 感觉Tick还是得用于检测状态

    public void TickIdle()
    {
        // canAttract = ...
        // todo: 顾客到货架一定范围内随机被吸引, 如果摇铃一定被吸引
        ChangeState(State.Attracting);

        // todo: 放徘徊寻路逻辑
    }

    // Enter Attract
    public void Attract()
    {
        Trade_Customer.Instance.Attract(this);

        //startAttractTime = TimeManager.Instance.GetComplexTime();
        attractingTime = 0;
        attractAttitude = 100;
    }

    public void TickAttracting()
    {
        // todo: 写Tick排队寻路

        // todo: 写等待执行 -> LostAttract() 还是 Buy() 逻辑
        // if a - attractAttitude -> 0
        LoseAttract();
        ChangeState(State.Idle);
        // if b - attractAttitude > 0
        Buy();
        ChangeState(State.Buying);

    }

    public void MinuteAttracting()
    {
        // Attracting态度函数
        AttractWill();
    }

    // customer在List里比较远 + 等待时长久 执行LoseAttract()
    public void LoseAttract()
    {
        Trade_Customer.Instance.AttractExit(this);
    }

    // customer在Buy列表里等待时长久, 态度变差, 给钱少
    // Enter Buy
    public void Buy()
    {
        Trade_Customer.Instance.Buy(this);
        Trade_Customer.Instance.AttractExit(this);

        // 要改
        //startBuyTime = TimeManager.Instance.GetComplexTime();
        buyingTime = 0;
        buyAttitude = startAttitude;

        if (TryBuyItem(out ItemStack _buyItem))
        {
            buyItem = _buyItem;
        }
        else ChangeState(State.Quit);
        
        //var need = shelfContainer.GetContainer().Items[0];
        // 上面这行我啥时候加的我忘了, 不过应该就是为了todo: buyItem = BuyItem()

        // 初始化预期数目和价格
        price = GetItemPrice(buyItem);
        count = GetItemCount(buyItem);


    }

    // 实时检测购买状态
    public void TickBuying()
    {
        if (!IsBuyItemValid(buyItem))
        {
            if (TryBuyItem(out ItemStack _buyItem))
            {
                buyItem = _buyItem;
                price = GetItemPrice(buyItem);
                count = GetItemCount(buyItem);
            }
            else ChangeState(State.Quit);
        }
        
    }

    public void MinuteBuying()
    {
        // todo: Buy态度函数
        BuyWill(price, count);
    }

    public void HaveBought()
    {
        Trade_Customer.Instance.BuyExit(this);
        //canAttract = false;

        CustomerCreator.Instance.RemoveCustomer(this);
    }



    // 吸引欲望随入队顺序及时间变化函数 (后期可能需要同其他类似函数放到一个类里)
    // 队列少一个人欲望再升一段
    private void AttractWill()
    {
        int currentListPlace = Trade_Customer.Instance.AttractPlace(this);
        // todo: 吸引函数
    }

    // 购买欲望(付钱的多少) 随时间变化函数
    private void BuyWill(int _price, int _count)
    {

        //float t = TimeManager.Instance.TimeDistToNow(startBuyTime);
        buyingTime++;
        float t = buyingTime;

        var c = minBuyAttitudeFactor;
        var T = maxWaitingTime_Buy;
        var a = (1 - c) * startAttitude / (T * T);
        if (t < T)
            buyAttitude = startAttitude - a * t * t;
        else
            buyAttitude = startAttitude * c;


        // 价格随buyAttitude的函数
        curPrice = Mathf.FloorToInt(_price * (1 + buyAttitude));
    }
    
    // 控制买什么, 随机种子之后可能需要变
    // 控制的是哪个ItemStack, 具体买多少还要另加函数

    // 当前期望购买物品是否合法
    private bool IsBuyItemValid(ItemStack item)
    {
        return !item.IsEmpty;
    }
    // 不合法时重新选择商品
    private bool TryBuyItem(out ItemStack item)
    {
        item = ItemStack.Empty;

        var r = Random.Range(0, 100);

        ItemContainer aContainer;
        int aCount;  // 槽位数

        ItemContainer bContainer;
        int bCount;

        int randomIndex;
        
        if(r >= 80)
        {
            aContainer = playerStore.shelfContainer.GetContainer();
            aCount = playerStore.shelfContainer.GetSlotCount();

            bContainer = playerStore.saleContainer.GetContainer();
            bCount = playerStore.saleContainer.GetSlotCount();
        }
        else
        {
            aContainer = playerStore.saleContainer.GetContainer();
            aCount = playerStore.saleContainer.GetSlotCount();

            bContainer = playerStore.shelfContainer.GetContainer();
            bCount = playerStore.shelfContainer.GetSlotCount();
        }

        if(SlotController.Instance.TryGetItem(aContainer, out List<ItemStack> aItems))
        {
            randomIndex = Random.Range(0, aItems.Count);
            item = aItems[randomIndex];
            return true;
        }

        if(SlotController.Instance.TryGetItem(bContainer, out List<ItemStack> bItems))
        {
            randomIndex = Random.Range(0, bItems.Count);
            item = bItems[randomIndex];
            return true;
        }

        item = ItemStack.Empty;
        return false;
    }

    // 初始化购买物品的价格
    private int GetItemPrice(ItemStack _buyItem)
    {
        return _buyItem.GetPrice();
    }

    // 初始化购买物品的数量
    private int GetItemCount(ItemStack _buyitem)
    {
        int max = _buyitem.GetStackAmount();    // 最大堆叠数
        int count = _buyitem.count;               // 实际物品数
        Vector2Int range = InitItemCountRange(max);
        
        // Random.Range(a,b) -> [a,b)
        int buyCount = Random.Range(range.x, range.y + 1);

        // 在顾客初始期望和实际物品数取较小值
        return Mathf.Min(buyCount, count);
    }
    private Vector2Int InitItemCountRange(int max)
    {
        // 按最大堆叠的百分之多少计
        // 3 的 10% 是 0.3 就向上取整
        if (max <= 1) return new Vector2Int(1, 1);
        if (max <= 10) return new Vector2Int(1, 2);
        if (max <= 30) return new Vector2Int(2, 5);
        if (max <= 50) return new Vector2Int(4, 10);
        if (max <= 100) return new Vector2Int(5, 20);
        if (max <= 300) return new Vector2Int(10, 30);

        return new Vector2Int(10, 30);
    }
    

}
