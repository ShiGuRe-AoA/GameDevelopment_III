using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerCreator : MonoBehaviour, IMinuteUpdatable
{

    private static CustomerCreator _instance;
    public static CustomerCreator Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindAnyObjectByType<CustomerCreator>();
                if(_instance == null)
                {
                    Debug.LogError("Customer Creator not found in scene.");
                }
            }
            return _instance;
        }
    } 

    Dictionary<CustomerController, int> _Customer_Anim = new();
    Dictionary<int, ComplexTime> _Anim_LeaveTime = new();

    // 之后prefab可能需要变成结构体
    // 结构体大概会包含 Animator(使用哪个模型) 之类的
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private RuntimeAnimatorController[] anims;
    [SerializeField] private PlayerStore_Entity storeEntity;

    [Header("生成范围")]
    [SerializeField] private float Radius = 5f;
    [SerializeField] private int maxSpawnTryCount = 30;

    [Header("每轮参数")]
    [SerializeField] private int waveIntervalHours = 2;  // 每多少游戏小时触发一轮
    [SerializeField] private int maxCustomerCount;       // 每轮最多生成顾客数（<= anims.Length）

    // 生成顾客间隔游戏内分钟
    [SerializeField] private float createDist = 5;

    // 离开后重新生成的最小间隔
    [SerializeField] private float minLeaveTime = 20;

    private ComplexTime lastWaveTime;
    private int customersRemainingThisWave;
    private float currentDist;

    private HashSet<int> activeAnims = new();
    private HashSet<CustomerController> activeCustomers = new();
    private Queue<CustomerController> pooledCustomers = new();

    public IReadOnlyCollection<CustomerController> ActiveCustomers => activeCustomers;


    private void Awake()
    {
        if(storeEntity == null)
        {
            storeEntity = FindObjectOfType<PlayerStore_Entity>();
            if(storeEntity == null)
            {
                Debug.LogError("Player Store Entity not found in scene.");
            }
        }
        if (customerPrefab == null)
        {
            Debug.LogError("customer Prefab not found.");
        }
    }

    private void Start()
    {
        RuntimeRegisterUtility.RegisterAll(this);
        lastWaveTime = TimeManager.Instance.GetComplexTime();
        customersRemainingThisWave = Mathf.Min(maxCustomerCount, anims.Length);
        currentDist = 0f;
    }

    public void OnMinuteUpdate()
    {
        // 检查是否到达新的一轮
        float minutesSinceLastWave = TimeManager.Instance.TimeDistToNow(lastWaveTime);
        if (minutesSinceLastWave >= waveIntervalHours * 60f)
        {
            StartNewWave();
        }

        if (customersRemainingThisWave <= 0) return;

        currentDist++;

        if (currentDist >= createDist)
            CreateCustomer();
    }

    private void StartNewWave()
    {
        lastWaveTime = TimeManager.Instance.GetComplexTime();
        customersRemainingThisWave = Mathf.Min(maxCustomerCount, anims.Length);
        currentDist = 0f;

        // 新轮开始：清空活跃动画占用，允许本轮重用所有 anim
        activeAnims.Clear();

        Debug.Log($"[CustomerCreator] 新的一轮顾客生成开始，本轮最多 {customersRemainingThisWave} 人");
    }

    public void CreateCustomer()
    {
        if (!TryGetAvailableAnimOrder(out int animOrder))
        {
            Debug.LogError("Failed to get Anim Order.");
            return;
        }

        CustomerController ctrl = GetAvailableCustomerController();
        if (ctrl == null) return;

        Vector2 spawnPos = GetRandomSpawnPositionAroundStore();
        ctrl.transform.position = spawnPos;

        ApplyAnimOrder(ctrl, animOrder);

        activeCustomers.Add(ctrl);
        activeAnims.Add(animOrder);
        _Customer_Anim[ctrl] = animOrder;

        ctrl.Init(storeEntity);
        ctrl.gameObject.SetActive(true);

        customersRemainingThisWave--;
        currentDist = 0f;
    }

    private CustomerController GetAvailableCustomerController()
    {
        Debug.Log("Enter Get Customer Ctrl");

        // 从对象池内获取 Ctrl
        if (pooledCustomers.Count > 0)
        {
            CustomerController ctrl = pooledCustomers.Dequeue();
            return ctrl;
        }

        // 当前场景 Ctrl 总数足够则不获取
        if (activeCustomers.Count + pooledCustomers.Count >= maxCustomerCount)
            return null;

        // 若允许则新生成 Ctrl
        GameObject customerObj = Instantiate(customerPrefab);

        // 新对象先关掉，统一交给 CreateCustomer 激活
        customerObj.SetActive(false);

        CustomerController newCtrl = customerObj.GetComponent<CustomerController>();

        if (newCtrl == null)
        {
            Debug.LogError("Customer Prefab does not have CustomerController.");
            Destroy(customerObj);
            return null;
        }

        return newCtrl;
    }

    // 随机点位生成
    private Vector2 GetRandomSpawnPositionAroundStore()
    {
        Vector2 center;

        if (storeEntity != null)
        {
            if (WorldState.Instance.TryGetEntityOccupiedCells(storeEntity, out List<Vector3Int> storeCells)
                && WorldState.Instance.TryGetCenterCell(storeCells, out Vector3Int centerCell))
            {
                center = WorldState.Instance.CellToWorld(centerCell);
            }
            else
            {
                // 实体尚未注册到格子时，回退到实体自身位置
                Debug.LogWarning("[CustomerCreator] storeEntity 未在格子系统中，回退到 transform.position", this);
                center = storeEntity.transform.position;
            }
        }
        else
        {
            Debug.LogError("[CustomerCreator] StoreEntity is null, use creator position as spawn center.", this);
            center = transform.position;
        }

        for (int i = 0; i < maxSpawnTryCount; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * Radius;
            Vector2 candidateWorldPos = center + randomOffset;

            Vector3Int candidateCell = WorldState.Instance.WorldToCell(candidateWorldPos);

            if (!WorldState.Instance.CheckEmpty(candidateCell))
                continue;

            return WorldState.Instance.CellToWorld(candidateCell);
        }

        Debug.LogWarning(
            $"Failed to find empty spawn cell around store after {maxSpawnTryCount} tries. Use store position fallback.",
            this
        );

        return center;
    }

    // 从 anims 中随机一个当前未在场, 冷却完成的 animOrder
    private bool TryGetAvailableAnimOrder(out int animOrder)
    {
        // 候选
        List<int> candidates = new();

        // 所有可选的 animOrder
        for(int i = 0; i < anims.Length; i++)
        {
            // anim 在场景已存在
            if (activeAnims.Contains(i))
                continue;

            // anim 满足 离场后过了一定时间的条件
            if (_Anim_LeaveTime.TryGetValue(i, out ComplexTime leaveTime))
            {
                float leaveDist = TimeManager.Instance.TimeDistToNow(leaveTime);

                if (leaveDist < minLeaveTime)
                    continue;
            }

            candidates.Add(i);
        }

        // 无候选
        if(candidates.Count == 0)
        {
            animOrder = -1;
            return false;
        }

        // 候选里随机
        int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
        animOrder = candidates[randomIndex];
        return true;
    }

    // 给 ctrl 用本次生成的 animOrder
    private void ApplyAnimOrder(CustomerController ctrl, int animOrder)
    {
        Animator anim = ctrl.GetComponentInChildren<Animator>();
        anim.runtimeAnimatorController = anims[animOrder];
    }

    // 播过出场动画后执行
    public void RemoveCustomer(CustomerController customer)
    {
        activeCustomers.Remove(customer);

        int animOrder = _Customer_Anim[customer];
        activeAnims.Remove(animOrder);

        _Customer_Anim.Remove(customer);

        ComplexTime leaveTime = TimeManager.Instance.GetComplexTime();
        _Anim_LeaveTime[animOrder] = leaveTime;

        ReturnCustomerToPool(customer);

    }

    // Return 2 Pool 时必须在场景内有过自然的出场动画, 或者范围内不可见
    private void ReturnCustomerToPool(CustomerController customer)
    {
        if (customer == null) return;

        customer.gameObject.SetActive(false);
        pooledCustomers.Enqueue(customer);
    }

    /// <summary>
    /// 调试用：检查某个 animOrder 是否正在场上。
    /// </summary>
    public bool IsAnimOrderActive(int animOrder)
    {
        return activeAnims.Contains(animOrder);
    }

    /// <summary>
    /// 调试用：获取当前某个顾客使用的 animOrder。
    /// </summary>
    public bool TryGetCurrentAnimOrder(CustomerController customer, out int animOrder)
    {
        return _Customer_Anim.TryGetValue(customer, out animOrder);
    }


}
