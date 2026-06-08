using System.Collections.Generic;
using UnityEngine;

public class CustomerController : MonoBehaviour, ITickUpdatable, IMinuteUpdatable
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("态度")]
    [SerializeField] private const float startAttitude = 1f;
    [SerializeField] private const float minBuyAttitudeFactor = 0.5f;
    [SerializeField] private const float maxWaitingTime_Buy = 10f;

    private float attractAttitude;
    private float buyAttitude;
    private float attractingTime;
    private float buyingTime;

    [Header("购买数据")]
    [SerializeField] private int price;
    [SerializeField] private int count;
    private int curPrice;

    [SerializeField] private ItemStack buyItem;

    private StateMachine<CustomerContext> customerMachine;
    private CustomerContext machineContext;

    public float AttractAttitude => attractAttitude;
    public bool BuyFinished { get; private set; }

    public void Init(PlayerStore_Entity _storeEntity)
    {
        machineContext = new CustomerContext
        {
            CustomerController = this,
            StoreEntity = _storeEntity,
            TargetEntity = null,

            HasQueueTarget = false,
            QueueTargetPos = transform.position,

            BuyItem = ItemStack.Empty,
            Price = 0,
            Count = 0
        };

        buyItem = ItemStack.Empty;
        BuyFinished = false;

        customerMachine = new StateMachine<CustomerContext>(machineContext);
        customerMachine.ChangeState(new State_CustomerIdle(customerMachine, machineContext));

        StateMachineBrain.Instance.RegistryMachine(customerMachine, transform);
    }

    // 使用 bool 未来统计本次吸引了多少人
    public bool BeAttractedByPlayerAction()
    {
        if (machineContext == null)
        {
            Debug.LogError("CustomerController.BeAttractedByPlayerAction: machineContext is null!");
            return false;
        }

        if (machineContext.StoreEntity == null)
        {
            Debug.LogError("CustomerController.BeAttractedByPlayerAction: StoreEntity is null!");
            return false;
        }

        if (machineContext.TargetEntity != null)
            return false;

        // TODO: 玩家执行某操作时调用，例如摇铃 / 开始摆摊 / 招呼顾客
        machineContext.TargetEntity = machineContext.StoreEntity;
        return true;
    }

    public void SetQueueTarget(Vector2 targetPos)
    {
        if (machineContext == null)
            return;

        machineContext.QueueTargetPos = targetPos;
        machineContext.HasQueueTarget = true;
    }

    // MoveTo具体函数可能要改
    public void MoveTo(Vector2 targetPos)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );
    }

    public void OnTickUpdate(float deltaTime)
    {
        // 状态机由 StateMachineBrain 统一 Update。
    }

    public void OnMinuteUpdate()
    {
        if (customerMachine == null)
            return;

        customerMachine.SendEvent(new FsmEvent(FsmEventType.MinutePassed, this));
    }

    public void ResetAttractAttitude()
    {
        attractingTime = 0f;
        attractAttitude = 100f;
    }

    public void AttractWill()
    {
        attractingTime++;

        if (machineContext == null || machineContext.TargetEntity == null)
            return;

        int queueIndex = machineContext.TargetEntity.GetQueueIndex(this);
        if (queueIndex < 0)
            return;

        // TODO: 替换成正式吸引欲望函数。
        float queuePenalty = 1f + queueIndex * 0.35f;
        attractAttitude -= queuePenalty;
        attractAttitude = Mathf.Max(0f, attractAttitude);
    }

    public void ResetBuyAttitude()
    {
        buyingTime = 0f;
        buyAttitude = startAttitude;
        BuyFinished = false;
    }

    public void BuyWill(int _price, int _count)
    {
        buyingTime++;
        float t = buyingTime;

        var c = minBuyAttitudeFactor;
        var T = maxWaitingTime_Buy;
        var a = (1 - c) * startAttitude / (T * T);

        if (t < T)
            buyAttitude = startAttitude - a * t * t;
        else
            buyAttitude = startAttitude * c;

        curPrice = Mathf.FloorToInt(_price * (1 + buyAttitude));
    }

    public bool TryPrepareBuyItem(out ItemStack item, out int itemPrice, out int itemCount)
    {
        item = ItemStack.Empty;
        itemPrice = 0;
        itemCount = 0;

        if (!TryBuyItem(out item))
            return false;

        itemPrice = GetItemPrice(item);
        itemCount = GetItemCount(item);

        buyItem = item;
        price = itemPrice;
        count = itemCount;

        return true;
    }

    public bool IsBuyItemValid(ItemStack item)
    {
        return !item.IsEmpty;
    }

    public void CompleteBuy()
    {
        BuyFinished = true;
    }

    public void OnQuit()
    {
        CustomerCreator.Instance.RemoveCustomer(this);
    }

    private bool TryBuyItem(out ItemStack item)
    {
        item = ItemStack.Empty;

        if (machineContext == null || machineContext.TargetEntity == null)
            return false;

        PlayerStore_Entity store = machineContext.TargetEntity;

        ItemContainer aContainer;
        ItemContainer bContainer;

        int randomIndex;
        int r = Random.Range(0, 100);

        if (r >= 80)
        {
            aContainer = store.shelfContainer;
            bContainer = store.saleContainer;
        }
        else
        {
            aContainer = store.saleContainer;
            bContainer = store.shelfContainer;
        }

        if (SlotController.Instance.TryGetItem(aContainer, out List<ItemStack> aItems))
        {
            randomIndex = Random.Range(0, aItems.Count);
            item = aItems[randomIndex];
            return true;
        }

        if (SlotController.Instance.TryGetItem(bContainer, out List<ItemStack> bItems))
        {
            randomIndex = Random.Range(0, bItems.Count);
            item = bItems[randomIndex];
            return true;
        }

        return false;
    }

    private int GetItemPrice(ItemStack _buyItem)
    {
        return _buyItem.GetPrice();
    }

    private int GetItemCount(ItemStack _buyitem)
    {
        int max = _buyitem.GetStackAmount();
        int actualCount = _buyitem.count;
        Vector2Int range = InitItemCountRange(max);

        int buyCount = Random.Range(range.x, range.y + 1);

        return Mathf.Min(buyCount, actualCount);
    }

    private Vector2Int InitItemCountRange(int max)
    {
        if (max <= 1) return new Vector2Int(1, 1);
        if (max <= 10) return new Vector2Int(1, 2);
        if (max <= 30) return new Vector2Int(2, 5);
        if (max <= 50) return new Vector2Int(4, 10);
        if (max <= 100) return new Vector2Int(5, 20);
        if (max <= 300) return new Vector2Int(10, 30);

        return new Vector2Int(10, 30);
    }
}