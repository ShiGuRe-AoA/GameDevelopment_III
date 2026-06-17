using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEditor.Progress;

[RequireComponent(typeof(CustomerPathAgent))]
public class CustomerController : MonoBehaviour, ITickUpdatable, IMinuteUpdatable
{
    #region Inspector

    [Header("移动代理")]
    [SerializeField] private CustomerPathAgent pathAgent;

    [Header("动画")]
    [SerializeField] private Animator runtimeAnimator;
    [SerializeField] private SpriteRenderer sr;

    [Tooltip("动画状态名前缀。默认会播放 NPC_Idle_Down / NPC_Move_Down 这种名字。")]
    [SerializeField] private string animPrefix = "NPC";

    [Header("吸引态度")]
    [SerializeField] private float maxAttractAttitude = 100f;
    [SerializeField] private float attractBaseLossPerMinute = 1f;
    [SerializeField] private float attractQueueIndexLoss = 0.75f;
    [SerializeField] private float attractTimeLossFactor = 0.05f;

    [Header("购买态度 / 小费")]
    [SerializeField] private float startAttitude = 1f;
    [SerializeField] private float minBuyAttitudeFactor = 0.5f;
    [SerializeField] private float maxWaitingTime_Buy = 10f;
    [SerializeField, Range(0f, 1f)] private float minTipRate = 0f;
    [SerializeField, Range(0f, 1f)] private float maxTipRate = 0.35f;

    [Header("购买数据")]
    [SerializeField] private int price;
    [SerializeField] private int count;
    private int curPrice;

    [Header("UI")]
    [SerializeField] private GameObject attractUI;  // " ! "
    [SerializeField] private GameObject buyUI;      // " (buyItem) "
    [SerializeField] private SpriteRenderer buyItemUI;
    [SerializeField] private TMP_Text buyCountUI;

    [Header("测试只读")]
    [SerializeField] private ItemStack buyItem;

    #endregion

    #region Runtime References

    private StateMachine<CustomerContext> customerMachine;
    private bool machineRegistered;
    private CustomerContext machineContext;

    #endregion

    #region Attitude Data

    private float attractAttitude;
    private float buyAttitude;
    private float attractingTime;
    private float buyingTime;

    private int preparedBuyCount;
    private int baseTotalPrice;
    private int currentTip;
    private int currentTotalPayment;

    #endregion

    #region Animation Data

    private string currentAnimName;

    #endregion

    #region Properties

    public float AttractAttitude => attractAttitude;
    public float BuyAttitude => buyAttitude;

    public int BaseTotalPrice => baseTotalPrice;
    public int CurrentTip => currentTip;
    public int CurrentTotalPayment => currentTotalPayment;

    public bool ShouldLeaveQueue => attractAttitude <= 0f;
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

        if (sr == null)
            Debug.LogError("not define Sprite Renderer on Customer Controller");

    }

    private void Start()
    {
        RuntimeRegisterUtility.RegisterAll(this);
    }

    private void Update()
    {
        UpdateCustomerAnimation();
    }

    #endregion

    #region Init

    public void Init(PlayerStore_Entity _storeEntity)
    {
        if(machineContext == null)
        {
            machineContext = new CustomerContext
            {
                CustomerController = this,
            };
        }
        
        machineContext.StoreEntity = _storeEntity;
        machineContext.TargetEntity = null;
        machineContext.HasQueueTarget = false;
        machineContext.QueueTargetPos = transform.position;
        machineContext.BuyItem = ItemStack.Empty;
        machineContext.Price = 0;
        machineContext.Count = 0;

        ResetRuntimeData();

        if (customerMachine == null)
            customerMachine = new StateMachine<CustomerContext>(machineContext);

        customerMachine.ChangeState(new State_CustomerIdle(customerMachine, machineContext));

        if (!machineRegistered)
        {
            StateMachineBrain.Instance.RegistryMachine(customerMachine, transform);
            machineRegistered = true;
        }
    }

    private void ResetRuntimeData()
    {
        ResetSprite();

        buyItem = ItemStack.Empty;
        price = 0;
        count = 0;
        curPrice = 0;

        preparedBuyCount = 0;
        baseTotalPrice = 0;
        currentTip = 0;
        currentTotalPayment = 0;

        BuyFinished = false;

        attractingTime = 0f;
        buyingTime = 0f;
        attractAttitude = 0f;
        buyAttitude = 0f;

        currentAnimName = null;

        CloseAttractUI();
        CloseBuyUI();

        StopWander();
        StopMove();
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

    public void BeginWanderToward(Vector2 targetPos)
    {
        if (pathAgent == null)
            return;

        pathAgent.BeginWanderToward(targetPos);
    }

    public void BeginWanderAwayFrom(Vector2 targetPos)
    {
        if (pathAgent == null)
            return;

        pathAgent.BeginWanderAwayFrom(targetPos);
    }

    public float DistanceTo(Vector2 targetPos)
    {
        Vector2 curPos = transform.position;
        return Vector2.Distance(curPos, targetPos);
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
        attractAttitude = maxAttractAttitude;
    }

    public void AttractWill()
    {
        attractingTime++;

        if (machineContext == null || machineContext.TargetEntity == null)
            return;

        int queueIndex = machineContext.TargetEntity.GetQueueIndex(this);
        if (queueIndex < 0)
            return;

        float loss = EvaluateAttractLoss(queueIndex, attractingTime);

        attractAttitude -= loss;
        attractAttitude = Mathf.Clamp(attractAttitude, 0f, maxAttractAttitude);
    }

    private float EvaluateAttractLoss(int queueIndex, float waitMinute)
    {
        float queueLoss = queueIndex * attractQueueIndexLoss;
        float timeLoss = waitMinute * attractTimeLossFactor;

        return attractBaseLossPerMinute + queueLoss + timeLoss;
    }

    public void ResetBuyAttitude()
    {
        buyingTime = 0f;
        buyAttitude = startAttitude;

        preparedBuyCount = 0;
        baseTotalPrice = 0;
        currentTip = 0;
        currentTotalPayment = 0;

        BuyFinished = false;
    }

    public void BuyWill(int unitPrice, int itemCount)
    {
        buyingTime++;

        buyAttitude = EvaluateBuyAttitude(buyingTime);

        RefreshPaymentPreview(unitPrice, itemCount);
    }

    private float EvaluateBuyAttitude(float waitMinute)
    {
        if (maxWaitingTime_Buy <= 0f)
            return startAttitude;

        float t = Mathf.Clamp01(waitMinute / maxWaitingTime_Buy);

        // 二次衰减：刚开始掉得慢，等久了掉得更明显。
        float curve = t * t;

        float minAttitude = startAttitude * minBuyAttitudeFactor;

        return Mathf.Lerp(startAttitude, minAttitude, curve);
    }

    private float GetNormalizedBuyAttitude()
    {
        float minAttitude = startAttitude * minBuyAttitudeFactor;

        if (Mathf.Approximately(startAttitude, minAttitude))
            return 1f;

        return Mathf.InverseLerp(minAttitude, startAttitude, buyAttitude);
    }

    private void RefreshPaymentPreview(int unitPrice, int itemCount)
    {
        itemCount = Mathf.Max(0, itemCount);

        baseTotalPrice = unitPrice * itemCount;

        float normalizedAttitude = GetNormalizedBuyAttitude();
        float tipRate = Mathf.Lerp(minTipRate, maxTipRate, normalizedAttitude);

        currentTip = Mathf.FloorToInt(baseTotalPrice * tipRate);
        currentTotalPayment = baseTotalPrice + currentTip;

        // 保留 curPrice 作为“当前单价预览”，方便 Inspector 看。
        curPrice = itemCount > 0
            ? Mathf.FloorToInt((float)currentTotalPayment / itemCount)
            : unitPrice;
    }

    #endregion

    #region Buying
    // 初始化 buyItem
    public bool TryPrepareBuyItem(out ItemStack item, out int itemPrice, out int itemCount)
    {
        item = ItemStack.Empty;

        itemPrice = 0;
        itemCount = 0;

        var store = machineContext.TargetEntity;

        // 当 count 不对但 item 存在时
        if(SlotController.Instance.TryGetItem(store.Containers, buyItem.itemId, out int currentCount))
        {
            item = buyItem;
            itemPrice = GetItemPrice(item);
            itemCount = Mathf.Min(GetItemCount(item), currentCount);

            price = itemPrice;
            count = itemCount;
            preparedBuyCount = itemCount;

            RefreshPaymentPreview(price, preparedBuyCount);

            buyItemUI.sprite = item.GetSprite();
            buyCountUI.text = count.ToString();

            return true;
        }


        if (!TryBuyItem(out ItemContainer buyContainer, out int buyIndex))
        {
            Debug.Log("Try Get Buy Item Failed.");
            return false;
        }

        item = buyContainer.Items[buyIndex];
        itemPrice = GetItemPrice(item);
        itemCount = GetItemCount(item);

        buyItem = item;
        price = itemPrice;
        count = itemCount;
        preparedBuyCount = itemCount;

        RefreshPaymentPreview(price, preparedBuyCount);

        buyItemUI.sprite = item.GetSprite();
        buyCountUI.text = count.ToString();

        return true;
    }

    // 获得预期 buyItem
    public bool TryConsumeBuyItem(ItemStack saleItem)
    {
        if (saleItem.IsEmpty)
            return false;

        if (buyItem.IsEmpty)
            return false;

        if (count <= 0)
            return false;

        if (buyItem.itemId != saleItem.itemId)
            return false;

        count--;

        if (buyCountUI != null)
            buyCountUI.text = count.ToString();

        if (count <= 0)
            CompleteBuy();

        return true;
    }

    public bool IsBuyItemValid()
    {
        if (!SlotController.Instance.holdingItem)
        {
            var store = machineContext.TargetEntity;
            if (SlotController.Instance.TryGetItem(store.Containers, buyItem.itemId, count)) 
                return true;

            return false;
        }

        return true;
    }

    public void CompleteBuy()
    {
        if (BuyFinished)
            return;

        RefreshPaymentPreview(price, preparedBuyCount);

        if (WorldState.Instance != null)
        {
            WorldState.Instance.coin += currentTotalPayment;
        }

        Debug.Log(
            $"Customer buy complete. Base={baseTotalPrice}, Tip={currentTip}, Total={currentTotalPayment}, BuyAttitude={buyAttitude:F2}",
            this
        );

        BuyFinished = true;
    }

    public bool OnEnter()
    {
        FadeInSprite();

        if(sr.color.a >= 1f)
        {
            return true;
        }
        return false;
    }
    public bool OnQuit()
    {
        FadeOutSprite();
        
        if(sr.color.a <= 0.1f)
        {
            CustomerCreator.Instance.RemoveCustomer(this);
            return true;
        }

        return false;
           
    }

    private void FadeInSprite()
    {
        Color col = sr.color;
        col.a += 0.01f;
        sr.color = col;
    }
    private void FadeOutSprite()
    {
        if (sr.color.a >= 1) return;

        Color col = sr.color;
        col.a -= 0.01f;
        sr.color = col;
    }

    private void ResetSprite()
    {
        Color col = sr.color;
        col.a = 0;
        sr.color = col;
        sr.flipX = false;
    }

    private bool TryBuyItem(out ItemContainer buyContainer, out int buyIndex)
    {
        buyContainer = null;
        buyIndex = -1;

        if (machineContext == null || machineContext.TargetEntity == null)
        {
            Debug.LogError("MachineContext for Buying is null", this);
            return false;
        }

        var store = machineContext.TargetEntity;

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

        if (SlotController.Instance.TryGetItem(aContainer, out List<int> aIndexs))
        {
            randomIndex = Random.Range(0, aIndexs.Count);
            buyContainer = aContainer;
            buyIndex = aIndexs[randomIndex];
            return true;
        }

        if (SlotController.Instance.TryGetItem(bContainer, out List<int> bIndexs))
        {
            randomIndex = Random.Range(0, bIndexs.Count);
            buyContainer = bContainer;
            buyIndex = bIndexs[randomIndex];
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
        int count = _buyItem.count;

        Vector2Int range = InitItemCountRange(max);

        int buyCount = Random.Range(range.x, range.y + 1);

        return Mathf.Min(buyCount, count);
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

    #region UI

    private Coroutine closeAttractUICoroutine;
    public void OpenAttractUI()
    {
        if(attractUI == null)
        {
            Debug.LogError("AttractUI is null!");
            return;
        }

        if (attractUI.activeSelf) return;

        attractUI.SetActive(true);

        if (closeAttractUICoroutine != null)
        {
            StopCoroutine(closeAttractUICoroutine);
            closeAttractUICoroutine = null;
        }

        closeAttractUICoroutine = StartCoroutine(WaitToCloseAttractUI());
    }

    private IEnumerator WaitToCloseAttractUI()
    {
        yield return new WaitForSeconds(1.5f);

        closeAttractUICoroutine = null;
        CloseAttractUI();
    }

    public void CloseAttractUI()
    {
        if (attractUI == null)
        {
            Debug.LogError("AttractUI is null!");
            return;
        }

        if (!attractUI.activeSelf) return;

        attractUI.SetActive(false);
    }

    public void OpenBuyUI()
    {
        if(buyUI == null)
        {
            Debug.LogError("BuyUI is null!");
            return;
        }

        if (buyUI.activeSelf) return;

        buyUI.SetActive(true);
    }

    public void CloseBuyUI()
    {
        if (buyUI == null)
        {
            Debug.LogError("BuyUI is null!");
            return;
        }

        if (!buyUI.activeSelf) return;

        buyUI.SetActive(false);
    }
    #endregion
}