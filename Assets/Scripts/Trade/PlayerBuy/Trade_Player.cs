using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trade_Player : MonoBehaviour
{
    private static Trade_Player _instance;
    public static Trade_Player Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<Trade_Player>();
                if(_instance == null)
                {
                    Debug.LogError("Trade_Player not found in scene");
                }
            }
            return _instance;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
