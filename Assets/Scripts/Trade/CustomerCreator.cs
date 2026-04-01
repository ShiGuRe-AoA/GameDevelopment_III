using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerCreator : MonoBehaviour
{
    // 之后prefab可能需要变成结构体
    // 结构体大概会包含 Animator(使用哪个模型) 之类的
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private ShelfContainer shelfContainer;

    private void Awake()
    {
        if (customerPrefab == null)
        {
            throw new ArgumentNullException(nameof(customerPrefab));
        }
    }

    // todo: 大概需要某个计时器来执行这个东西

    void CreateCustomer()
    {
        var customer = Instantiate(customerPrefab);
        var customerController = customer.GetComponent<CustomerController>();
        customerController.Init(shelfContainer);
    }
}
