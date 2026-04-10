using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStoreContainer : MonoBehaviour
{
    [Header("Shelf Panel")]
    public Transform StorePanel;

    public ShelfContainer shelfContainer;
    public SaleContainer saleContainer;

    // 仔细想想, shelfLevel不应该放到shelf里面,应该是一个比较全局的变量, 脚本里的应该只能get到全局里的值
    [SerializeField] private int shelfLevel;

    public bool IsOpen { get; private set; }

    // 之后这些似乎需要到InputManager实现交互
    // 或者Input.GetButtonDown然后执行OpenShelf,再次就CloseShelf
    public void OpenShelf()
    {
        StorePanel.gameObject.SetActive(true);
        shelfContainer.Refresh();
        saleContainer.Refresh();

        IsOpen = true;
    }

    public void CloseShelf()
    {
        StorePanel.gameObject.SetActive(false);
        IsOpen = false;
    }

    // 还得有个对外的UI显示, 大概类似于手持虚影那种
    // 就是在saleSlotCount内的为卖品, 对外展示
    
    // 每次打开Shelf的时候刷新一下看看是否


    public void UpgradeShelf(int ShelfLevel)
    {
        this.shelfLevel = ShelfLevel; // 因为我感觉Shelf还是得和玩家挂钩

    }

}
