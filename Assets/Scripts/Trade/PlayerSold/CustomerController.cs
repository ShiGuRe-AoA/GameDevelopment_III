using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomerPathAgent))]
public class CustomerController : MonoBehaviour, ITickUpdatable, IMinuteUpdatable
{
    #region Inspector

    [Header("移动代理")]
    [SerializeField] private CustomerPathAgent pathAgent;

    [Header("动画")]
    [SerializeField] private Animator runtimeAnimator;

    [Tooltip("动画状态名前缀。默认会播放 NPC_Idle_Down / NPC_Move_Down 这种名字。")]
    [SerializeField] private string animPrefix = "NPC";

    [Header("态度")]
    [SerializeField] private float startAttitude = 1f;
    [SerializeField] private float minBuyAttitudeFactor = 0.5f;
    [SerializeField] private float maxWaitingTime_Buy = 10f;

    [Header("购买数据")]
    [SerializeField] private int price;
    [SerializeField] private int count;
    private int curPrice;

    [Header("UI")]
    [SerializeField] private SpriteRenderer attractUI;  // " ! "
    [SerializeField] private SpriteRenderer buyUI;      // " (buyItem) "

    [Header("测试只读")]
    [SerializeField] private ItemStack buyItem;

    #endregion

    #region Runtime References

    private StateMachine<CustomerContext> customerMachine;
    private CustomerContext machineContext;

    #endregion

    #region Attitude Data

    private float attractAttitude;
    private float buyAttitude;
    private float attractingTime;
    private float buyingTime;

    #endregion

    #region Animation Data

    private string currentAnimName;

    #endregion

    #region Properties

    public SpriteRenderer AttractUI => attractUI;
    public SpriteRenderer BuyUI => buyUI;

    public float AttractAttitude => attractAttitude;
    public bool BuyFinished { get; private set; }

    public CustomerPathAgent PathAgent => pathAgent;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (pathAgent == null)
            pathAgent = GetComponent<CustomerPathAgent>();

        if (runtimeAnimator == null)
            runtimeAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateCustomerAnimation();
    }

    #endregion

    #region Init

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

        StopMove();

        customerMachine = new StateMachine<CustomerContext>(machineContext);
        customerMachine.ChangeState(new State_CustomerIdle(customerMachine, machineContext));

        StateMachineBrain.Instance.RegistryMachine(customerMachine, transform);
    }

    #endregion

    #region Attract / Queue

    // 使用 bool 未来统计本次吸引了多少人
    public bool BeAttractedByPlayerAction()
    {
        if (machineContext == null)
        {
            Debug.LogError("CustomerController.BeAttractedByPlayerAction: machineContext is null!", this);
            return false;
        }

        if (machineContext.StoreEntity == null)
        {
            Debug.LogError("CustomerController.BeAttractedByPlayerAction: StoreEntity is null!", this);
            return false;
        }

        if (machineContext.TargetEntity != null)
            return false;

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

    public bool HasArrivedQueueTarget()
    {
        if (machineContext == null || !machineContext.HasQueueTarget)
            return false;

        if (pathAgent == null)
            return false;

        return pathAgent.HasArrived(machineContext.QueueTargetPos);
    }

    #endregion

    #region Move / Wander Bridge

    /// <summary>
    /// CustomerMachine.Attracting 调用的移动入口。
    /// 这里默认认为目标是 QueueTarget，所以到达后面朝 Up。
    /// </summary>
    public void MoveTo(Vector2 targetPos)
    {
        if (pathAgent == null)
            return;

        pathAgent.MoveTo(targetPos, faceUpWhenArrive: true);
    }

    public void StopMove()
    {
        if (pathAgent == null)
            return;

        // suppressSeparation = true:
        // 进入 Buying / 停在 QueueTarget 后，不再被软分离推离队列点。
        pathAgent.StopMove(suppressSeparation: true);
    }

    public void FaceUp()
    {
        if (pathAgent == null)
            return;

        pathAgent.FaceUp();

        // 强制刷新动画，避免还停在旧方向 Idle / Move。
        currentAnimName = null;
        UpdateCustomerAnimation();
    }

    public void BeginWander()
    {
        if (pathAgent == null)
            return;

        pathAgent.BeginWander();
    }

    public void StopWander()
    {
        if (pathAgent == null)
            return;

        pathAgent.StopWander();
    }

    public void UpdateWander()
    {
        if (pathAgent == null)
            return;

        pathAgent.UpdateWander();
    }

    #endregion

    #region Tick / Minute

    public void OnTickUpdate(float deltaTime)
    {
        // 状态机由 StateMachineBrain 统一 Update。
        // 移动由 CustomerPathAgent.FixedUpdate 处理。
    }

    public void OnMinuteUpdate()
    {
        if (customerMachine == null)
            return;

        customerMachine.SendEvent(new FsmEvent(FsmEventType.MinutePassed, this));
    }

    #endregion

    #region Attitude

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
        float c = minBuyAttitudeFactor;
        float T = maxWaitingTime_Buy;
        float a = (1f - c) * startAttitude / (T * T);

        if (t < T)
            buyAttitude = startAttitude - a * t * t;
        else
            buyAttitude = startAttitude * c;

        curPrice = Mathf.FloorToInt(_price * (1f + buyAttitude));
    }

    #endregion

    #region Buying

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
        {
            Debug.LogError("MachineContext for Buying is null", this);
            return false;
        }

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

        Debug.Log("no item in container is legal");
        return false;
    }

    private int GetItemPrice(ItemStack _buyItem)
    {
        return _buyItem.GetPrice();
    }

    private int GetItemCount(ItemStack _buyItem)
    {
        int max = _buyItem.GetStackAmount();
        int actualCount = _buyItem.count;
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

    #endregion

    #region Animation

    private void UpdateCustomerAnimation()
    {
        if (runtimeAnimator == null || pathAgent == null)
            return;

        bool isMoving = pathAgent.IsMoving;
        Direction dir = pathAgent.FacingDir;

        string animName = isMoving
            ? GetMoveAction(dir)
            : GetIdleAction(dir);

        if (currentAnimName == animName)
            return;

        runtimeAnimator.Play(animName);
        currentAnimName = animName;
    }

    private string GetIdleAction(Direction dir)
    {
        return dir switch
        {
            Direction.Up => $"{animPrefix}_Idle_Up",
            Direction.Left => $"{animPrefix}_Idle_Left",
            Direction.Right => $"{animPrefix}_Idle_Right",
            Direction.Down => $"{animPrefix}_Idle_Down",
            _ => $"{animPrefix}_Idle_Down"
        };
    }

    private string GetMoveAction(Direction dir)
    {
        return dir switch
        {
            Direction.Up => $"{animPrefix}_Move_Up",
            Direction.Left => $"{animPrefix}_Move_Left",
            Direction.Right => $"{animPrefix}_Move_Right",
            Direction.Down => $"{animPrefix}_Move_Down",
            _ => $"{animPrefix}_Move_Down"
        };
    }

    #endregion
}