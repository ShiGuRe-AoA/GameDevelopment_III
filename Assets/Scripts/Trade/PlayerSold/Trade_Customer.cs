using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

// 顾客买玩家商品
public class Trade_Customer : MonoBehaviour
{
    private static Trade_Customer _instance;
    public static Trade_Customer Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindAnyObjectByType<Trade_Customer>();
                if(_instance == null)
                {
                    Debug.LogError("Trade_Customer not found in scene.");
                }
            }
            return _instance;
        }
    }

    // 正在排队(可能会流失)
    [SerializeField] private List<CustomerController> customers_beAttracted = new List<CustomerController>();
    // 正在买(不会流失)
    [SerializeField] private List<CustomerController> customers_isBuyying = new List<CustomerController>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Clear()
    {
        customers_beAttracted.Clear();
        customers_isBuyying.Clear();
    }

    public void Attract(CustomerController customer)
    {
        var c = customers_beAttracted;
        if (c.Contains(customer)) return;
        else c.Add(customer);
    }

    public void AttractExit(CustomerController customer)
    {
        var c = customers_beAttracted;
        if (c.Contains(customer)) c.Remove(customer);
        else return;
    }

    // 顾客在Attract列表中的位置
    public int AttractPlace(CustomerController customer)
    {
        var c = customers_beAttracted;
        return c.IndexOf(customer);
    }

    public void Buy(CustomerController customer)
    {
        var c = customers_isBuyying;
        if(c.Contains(customer)) return;
        else c.Add(customer);
    }

    public void BuyExit(CustomerController customer)
    {
        var c = customers_isBuyying;
        if(c.Contains(customer)) c.Remove(customer);
        else return;
    }
}
