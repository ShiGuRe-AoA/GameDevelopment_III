using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店面板 UI 控制器。
/// 挂载在商店面板根 GameObject 上，负责：
/// 1. 根据 ShopInventorySO 动态生成商品按钮
/// 2. 处理购买扣除金钱 + 添加物品到背包（含失败回滚）
/// 3. 面板生命周期管理
/// </summary>
public class ShopPanelController : MonoBehaviour
{
    [Header("商品按钮生成")]
    [SerializeField] private Transform contentParent;          // 挂有 Horizontal/VerticalLayoutGroup 的父物体
    [SerializeField] private ShopGoodsButton goodsButtonPrefab; // 商品按钮预制体

    private readonly List<ShopGoodsButton> activeButtons = new();
    [SerializeField] private ShopInventorySO currentInventory;

    // ================================================================================
    // 面板开关
    // ================================================================================
    public void Open(ShopInventorySO inventory)
    {
        if (inventory == null)
        {
            Debug.LogError("[ShopPanelController] Open 失败：ShopInventorySO 为空");
            return;
        }

        currentInventory = inventory;
        gameObject.SetActive(true);
        BuildButtons();
        Debug.Log($"[ShopPanelController] 商店已打开：{inventory.StoreName}，共 {inventory.Goods.Count} 件商品");
    }

    public void Close()
    {
        if (!gameObject.activeSelf) return;

        DestroyAllButtons();
        gameObject.SetActive(false);
        currentInventory = null;
        Debug.Log("[ShopPanelController] 商店已关闭");
    }

    // ================================================================================
    // 按钮生成
    // ================================================================================
    private void BuildButtons()
    {
        DestroyAllButtons();

        if (currentInventory == null || currentInventory.Goods.Count == 0)
        {
            Debug.Log("[ShopPanelController] 商品列表为空，不生成按钮");
            return;
        }

        foreach (ShopItemDefSO goods in currentInventory.Goods)
        {
            if (goods == null || goods.Item == null)
            {
                Debug.LogWarning("[ShopPanelController] 跳过无效商品条目");
                continue;
            }

            ShopGoodsButton btn = Instantiate(goodsButtonPrefab, contentParent);
            btn.Init(goods, this);
            activeButtons.Add(btn);
        }
    }

    private void DestroyAllButtons()
    {
        foreach (ShopGoodsButton btn in activeButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        activeButtons.Clear();
    }

    // ================================================================================
    // 购买逻辑
    // ================================================================================
    /// <summary>尝试购买指定商品，返回是否购买成功</summary>
    public bool TryBuy(ShopItemDefSO goods)
    {
        if (goods == null || goods.Item == null)
        {
            Debug.LogError("[ShopPanelController] TryBuy 失败：商品数据为空");
            return false;
        }

        int price = goods.Price;
        int itemId = goods.Item.ID_num;

        // 1. 扣钱
        if (!WorldState.Instance.TrySpendCoin(price))
        {
            Debug.Log($"[ShopPanelController] 金币不足：需要 {price}，当前 {WorldState.Instance.coin}");
            return false;
        }

        // 2. 添加物品到背包
        ItemContainer backpack = WorldState.Instance.backpackContainer;
        if (backpack == null)
        {
            // 回滚扣钱
            WorldState.Instance.coin += price;
            Debug.LogError("[ShopPanelController] backpackContainer 为空，已回滚");
            return false;
        }

        bool added = SlotController.Instance.TryAddItem(itemId, 1, backpack);
        if (!added)
        {
            // 回滚扣钱
            WorldState.Instance.coin += price;
            Debug.Log($"[ShopPanelController] 背包已满，无法添加 {goods.Item.Name}，已回滚");
            return false;
        }

        Debug.Log($"[ShopPanelController] 购买成功：{goods.Item.Name}，-{price} 金币");
        return true;
    }

    // ================================================================================
    // 生命周期
    // ================================================================================
    private void OnDisable()
    {
        // 面板关闭（含手动关闭、父物体禁用等）时必须清理按钮和长按状态
        DestroyAllButtons();
    }
}
