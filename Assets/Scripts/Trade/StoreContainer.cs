using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreContainer : ItemContainer_Base
{
    [Header("Store Panel")]
    public Transform StorePanel;

    public bool IsOpen {  get; private set; }

    protected override void Awake()
    {
        base.Awake();
        SlotController.Instance.RefreshAll(container);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Store每天或每周的存货不一样, 调用Refresh, 根据当季的东西卖, 非当季的要高价卖, 即乘值修正
    // 乘值修正分为季节修正和好感度修正

    public void OpenStorePanel()
    {
        StorePanel.gameObject.SetActive(true);
        IsOpen = true;
    }

    public void CloseStorePanel()
    {
        StorePanel.gameObject.SetActive(false);
        IsOpen = false;
    }
}
