using System.Collections;
using System.Collections.Generic;
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
                _instance = FindObjectOfType<Trade_Customer>();
                if(_instance == null)
                {
                    Debug.LogError("Trade_Customer not found");
                }
            }
            return _instance;
        }
    }
    // 可被吸引(不会排队)
    private List<CustomerController> customers_canBeAttracted = new List<CustomerController>();
    // 正在排队(可能会流失)
    private List<CustomerController> customers_beAttracted = new List<CustomerController>();
    // 正在买(不会流失)
    private List<CustomerController> customers_isBuyying = new List<CustomerController>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
