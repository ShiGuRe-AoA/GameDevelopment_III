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
    private class Customer_Anim
    {
        public Customer_Anim(CustomerController _customer, int _animOrder)
        {
            customer = _customer;
            animOrder = _animOrder;
            leaveTime = new ComplexTime();
        }

        public CustomerController customer;
        public int animOrder;
        public ComplexTime leaveTime;
    }

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

    // 用于存储场景中有的Anim, 防止同一模型同时出现
    [SerializeField] private List<Customer_Anim> curCustomers = new List<Customer_Anim>();      // ReadOnly
    [SerializeField] private List<Customer_Anim> leaveCustomers = new List<Customer_Anim>();    // ReadOnly
    
    // 离开后重新生成的最小间隔
    [SerializeField] private float minLeaveTime = 20;

    [SerializeField] private int customerCount = 0; // ReadOnly

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
        //createTime = 0;

        maxCustomerCount = Mathf.Min(maxCustomerCount, anims.Length);
    }


    public void OnMinuteUpdate()
    {
        if (!isTradeDay) return;

        //createTime++;
        if (customerCount < maxCustomerCount)
        {
            if (TimeManager.Instance.TimeDistToNow(createTime) >= createDist)
                CreateCustomer();
        }
    }

    // 在 Creator 内执行
    public void CreateCustomer()
    {
        // 标记本次生成顾客的时间点
        createTime = TimeManager.Instance.GetComplexTime();
        //createTime = 0;
        
        // 生成顾客预制体
        GameObject customer = Instantiate(customerPrefab);
        // 查找顾客的 Controller 组件
        CustomerController ctrl = customer.GetComponent<CustomerController>();
        if (ctrl == null)
        {
            Debug.LogError($"Customer Controller on customer: {customer.name} not found", customer);
        }
        ctrl.Init(playerStore);
        // 查找顾客的 Animator 组件并为其赋值
        Animator anim = customer.GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError($"Animator on customer: {customer.name} not found", customer);
        }

        int animOrder = AnimOrder();
        anim.runtimeAnimatorController = anims[animOrder];
        // 将生成的顾客放进当前场景列表里
        curCustomers.Add(BindCustomerAnim(ctrl, animOrder));

        customerCount++;
    }

    
    public void CreateCustomerTest()
    {
        // create时还要在几个生成点位分配
        createTime = TimeManager.Instance.GetComplexTime();

        var cur = curCustomers;
        var leave = leaveCustomers;

        // 假如场景里的不够多则实例化, 否则重新调用
        if(cur.Count + leave.Count < maxCustomerCount)
        {
            GameObject customer = Instantiate(customerPrefab);
            
            CustomerController ctrl = customer.GetComponent<CustomerController>();
            if (ctrl == null)
            {
                Debug.LogError($"Customer Controller on customer: {customer.name} not found", customer);
            }
            ctrl.Init(playerStore);

            Animator anim = customer.GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogError($"Animator on customer: {customer.name} not found", customer);
            }

            int animOrder = AnimOrder();
            anim.runtimeAnimatorController = anims[animOrder];

            curCustomers.Add(BindCustomerAnim(ctrl, animOrder));
        }
        else
        {
            int animOrder = AnimOrder();
            var bind = CustomerOrAnim2Bind(animOrder, leave);
            
            leave.Remove(bind);
            cur.Add(bind);
        }

        customerCount++;
    }

    // 让 CustomerController 自己执行 Remove
    public void RemoveCustomer(CustomerController customer)
    {
        // todo: 从场景删除角色
        var cur = curCustomers;
        var leave = leaveCustomers;

        var curBind = CustomerOrAnim2Bind(customer, cur);
        var leaveTime = TimeManager.Instance.GetComplexTime();

        leave.Add(AddLeaveTime2CurBind(curBind, leaveTime));
        cur.Remove(curBind);

        customerCount--;
    }

    // 将分散的 CustomerController 与 Animator 绑定
    private Customer_Anim BindCustomerAnim(CustomerController _customer, int _animOrder)
    {
        Customer_Anim result = new Customer_Anim(_customer, _animOrder);
        return result;
    }

    private Customer_Anim AddLeaveTime2CurBind(Customer_Anim _customer_Anim, ComplexTime _leaveTime)
    {
        _customer_Anim.leaveTime = _leaveTime;
        return _customer_Anim;
    }

    // 在 curCustomers 列表中寻找某 CustomerController 对应的 Customer_Anim
    private Customer_Anim CustomerOrAnim2Bind(CustomerController _customer, List<Customer_Anim> binds)
    {
        return binds.FirstOrDefault(a => a.customer == _customer);
    }

    private Customer_Anim CustomerOrAnim2Bind(int _animOrder, List<Customer_Anim> binds)
    {
        return binds.FirstOrDefault(a => a.animOrder == _animOrder);
    }



    // 寻找合适的 Animator
    private int AnimOrder()
    {
        var cur = curCustomers;
        var leave = leaveCustomers;

        // 用表存已经试过的 Animator
        HashSet<int> triedOrders = new HashSet<int>();

        int randomOrder;
        Customer_Anim curResult;
        Customer_Anim leaveResult;

        while (true)
        {
            randomOrder = UnityEngine.Random.Range(0, anims.Count());

            if (triedOrders.Contains(randomOrder))
            {
                continue;
            }

            triedOrders.Add(randomOrder);

            // 如果当前场景不存在 Order
            if (!TryGetAnim(cur, randomOrder, out curResult))
            {
                return randomOrder;
            }

            // 如果当前场景存在 Order, 则看已滚蛋的人里有没有 Order
            if(TryGetAnim(leave, randomOrder, out leaveResult))
            {
                // 滚蛋了一定时长后就当生面孔了
                if (TimeManager.Instance.TimeDistToNow(leaveResult.leaveTime) > minLeaveTime)
                {
                    return randomOrder;
                }
            }
        }
    }

    // 查找有无符合条件的 animOrder, 顺便找其对应的 Customer_Anim
    private bool TryGetAnim(List<Customer_Anim> customers, int targetOrder, out Customer_Anim result)
    {
        foreach(var c in customers)
        {
            if(c.animOrder == targetOrder)
            {
                result = c;
                return true;
            }
        }

        result = default;
        return false;
    }


}
