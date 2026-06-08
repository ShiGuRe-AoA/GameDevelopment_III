using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerAttractSystem : MonoBehaviour
{
    private static CustomerAttractSystem _instance;
    public static CustomerAttractSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // 你可以根据需要在这里处理实例化逻辑
                _instance = FindObjectOfType<CustomerAttractSystem>();
                if (_instance == null)
                {
                    Debug.LogError("CustomerAttractSystem not found in the scene!");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 广播吸引事件到范围内顾客
    /// </summary>
    [SerializeField] private float range = 3f;    
    public void AttractCustomers(PlayerContext ctx)
    {
        foreach (var customer in CustomerCreator.Instance.ActiveCustomers)
        {
            float dist = Vector2.Distance(ctx.Player.position, customer.transform.position);
            if (dist <= range)
            {
                customer.BeAttractedByPlayerAction();
            }
        }
    }
}
