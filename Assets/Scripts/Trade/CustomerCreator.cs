using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerCreator : MonoBehaviour
{

    private static CustomerCreator _instance;
    public static CustomerCreator Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindAnyObjectByType<CustomerCreator>()
                    ?? throw new InvalidOperationException("CustomerCreator not found in scene!");
            }
            return _instance;
        }
    } 
    private struct Customer_Anim
    {
        public CustomerController customer;
        public int animOrder;
        public ComplexTime leaveTime;
    }
    // 之后prefab可能需要变成结构体
    // 结构体大概会包含 Animator(使用哪个模型) 之类的
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private RuntimeAnimatorController[] customerAnims;
    [SerializeField] private ShelfContainer shelfContainer;

    // 上一次生成顾客的时间
    private ComplexTime createTime;
    // 生成顾客间隔游戏内分钟
    [SerializeField] private float createDist = 5;

    // 当时间到集市日, customerCount < maxCustomerCount时 为true
    private bool canCreate;

    // 用于存储场景中有的Anim, 防止同一模型同时出现
    [SerializeField] private List<Customer_Anim> curCustomers;  // ReadOnly
    [SerializeField] private List<Customer_Anim> leaveCustomers;    // ReadOnly
    
    // 离开后重新生成的最小间隔
    [SerializeField] private float minLeaveTime = 20;

    [SerializeField] private int customerCount = 0; // ReadOnly

    // 场景中最多同时出现顾客数
    [SerializeField] private int maxCustomerCount;


    private void Awake()
    {
        if(shelfContainer == null)
        {
            shelfContainer = FindObjectOfType<ShelfContainer>()
                ?? throw new ArgumentNullException(nameof(shelfContainer));
        }
        if (customerPrefab == null)
        {
            throw new ArgumentNullException(nameof(customerPrefab));
        }
    }

    // todo: 大概需要某个计时器来执行这个东西
    private void Update()
    {
        //if (canCreate)
        //{
        //    canCreate = false;
        //    CreateCustomer();
        //}
    }

    // 在 Creator 内执行
    public void CreateCustomer()
    {
        // 生成顾客预制体
        GameObject customer = Instantiate(customerPrefab);
        // 查找顾客的 Controller 组件
        CustomerController customerCtrl = customer.GetComponent<CustomerController>()
            ?? throw new ArgumentException(nameof(CustomerController));
        customerCtrl.Init(shelfContainer);
        // 查找顾客的 Animator 组件并为其赋值
        Animator customerAnim = customer.GetComponent<Animator>()
            ?? throw new ArgumentException(nameof(Animator));

        int animOrder = AnimOrder();
        customerAnim.runtimeAnimatorController = customerAnims[animOrder];
        // 将生成的顾客放进当前场景列表里
        curCustomers.Add(BindCustomerAnim(customerCtrl, animOrder));

        customerCount++;
    }

    // 大概需要让 CustomerController 自己执行?
    public void RemoveCustomer(CustomerController customer)
    {
        // todo: 从场景删除角色
        var cur = curCustomers;
        var leave = leaveCustomers;

        leave.Add(CurCustomer2CurBind(customer));
        cur.Remove(CurCustomer2CurBind(customer));

        customerCount--;
    }

    // 将分散的 CustomerController 与 Animator 绑定
    private Customer_Anim BindCustomerAnim(CustomerController _customer, int _animOrder)
    {
        Customer_Anim result = new Customer_Anim { customer = _customer, animOrder = _animOrder };
        return result;
    }

    // 在 curCustomers 列表中寻找某 CustomerController 对应的 Customer_Anim
    private Customer_Anim CurCustomer2CurBind(CustomerController _customer)
    {
        var cur = curCustomers;
        return cur.FirstOrDefault(a => a.customer == _customer);
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
            randomOrder = UnityEngine.Random.Range(0, customerAnims.Count() - 1);

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
