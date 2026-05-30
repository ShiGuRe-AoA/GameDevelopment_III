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

    Dictionary<CustomerController, int> _Customer_Anim;
    Dictionary<int, ComplexTime> _Anim_LeaveTime;

    // 之后prefab可能需要变成结构体
    // 结构体大概会包含 Animator(使用哪个模型) 之类的
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private RuntimeAnimatorController[] anims;
    [SerializeField] private PlayerStoreContainer playerStore;

    // 上一次生成顾客的时间
    private ComplexTime createTime;
    //private float createTime;
    // 生成顾客间隔游戏内分钟
    [SerializeField] private float createDist = 5;

    // 当时间到集市日, customerCount < maxCustomerCount时 为true
    private bool isTradeDay;  // 是否到集市日

    [SerializeField] private HashSet<int> activeAnims = new();
    [SerializeField] private HashSet<CustomerController> activeCustomers = new();
    [SerializeField] private Queue<CustomerController> pooledCustomers = new();
    
    // 离开后重新生成的最小间隔
    [SerializeField] private float minLeaveTime = 20;

    // 场景中最多同时出现顾客数
    [SerializeField] private int maxCustomerCount; // 一定要 <= customerAnims


    private void Awake()
    {
        if(playerStore == null)
        {
            playerStore = FindObjectOfType<PlayerStoreContainer>();
            if(playerStore == null)
            {
                Debug.LogError("Player Store Container not found in scene.");
            }
        }
        if (customerPrefab == null)
        {
            Debug.LogError("customer Prefab not found.");
        }
    }

    private void Start()
    {
        createTime = TimeManager.Instance.GetComplexTime();
        
        maxCustomerCount = Mathf.Min(maxCustomerCount, anims.Length);
    }


    public void OnMinuteUpdate()
    {
        if (!isTradeDay) return;

        //createTime++;
        if (activeCustomers.Count < maxCustomerCount)
        {
            if (TimeManager.Instance.TimeDistToNow(createTime) >= createDist)
                CreateCustomer();
        }
    }

    public void CreateCustomer()
    {
        if (!TryGetAvailableAnimOrder(out int animOrder)) return;

        CustomerController ctrl = GetAvailableCustomerController();
        if (ctrl == null) return;

        ApplyAnimOrder(ctrl, animOrder);

        activeCustomers.Add(ctrl);
        activeAnims.Add(animOrder);
        _Customer_Anim[ctrl] = animOrder;

        ctrl.gameObject.SetActive(true);

        createTime = TimeManager.Instance.GetComplexTime();
    }

    private CustomerController GetAvailableCustomerController()
    {
        // 从对象池内获取 Ctrl
        if (pooledCustomers.Count > 0)
        {
            CustomerController ctrl = pooledCustomers.Dequeue();

            ctrl.gameObject.SetActive(true);
            return ctrl;
        }
        
        // 当前场景 Ctrl 总数足够则不获取
        if (activeCustomers.Count + pooledCustomers.Count >= maxCustomerCount)
            return null;

        // 若允许则新生成 Ctrl
        GameObject customerObj = Instantiate(customerPrefab);

        CustomerController newCtrl = customerObj.GetComponent<CustomerController>();

        newCtrl.Init(playerStore);

        return newCtrl;
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
        Animator anim = ctrl.GetComponent<Animator>();
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
