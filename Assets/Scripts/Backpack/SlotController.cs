using System;
using System.Collections.Generic;
using UnityEngine;

public struct ItemStack
{
    public int itemId;
    public int count;
    public int extra;

    public bool IsEmpty => count <= 0 || itemId < 0;
    public static ItemStack Empty => new ItemStack { itemId = -1, count = 0, extra = 0 };

    public void Clear()
    {
        itemId = -1;
        count = 0;
        extra = 0;
    }
    public int GetPrice()
    {
        var def = ItemRegistry.Get(itemId);
        return def.BasePrice;
    }
}
public class ContainerView
{
    public readonly ItemContainer Container;
    public readonly List<ItemSlotUI> UISlots;

    public ContainerView(ItemContainer container, List<ItemSlotUI> uiSlots)
    {
        Container = container;
        UISlots = uiSlots;
    }

    public int UISlotCount => UISlots.Count;
}
public class ViewMapPair
{
    public ContainerView view;
    private Func<int, int> map;//container to view

    public ViewMapPair(ContainerView view, Func<int, int> map)
    {
        this.view = view;
        this.map = map;
    }
    public ViewMapPair(ContainerView view)
    {
        this.view = view;
        this.map = index => index;
    }
    public int GetViewIndex(int index) => map(index);
}
public class ItemContainer
{
    public string Name { get; set; }
    public readonly ItemStack[] Items;
    public List<ViewMapPair> ViewMaps = new();
    public int SlotCount => Items.Length;

    public ItemContainer(int slotCount, string name)
    {
        this.Name = name;
        Items = new ItemStack[slotCount];
        for (int i = 0; i < slotCount; i++) Items[i] = ItemStack.Empty;
    }
    public void RegistryView(ContainerView view, Func<int, int> map)
    {
        ViewMaps.RemoveAll(v => v.view == view);
        ViewMaps.Add(new ViewMapPair(view, map));
    }
    public void RegistryView(ContainerView view)
    {
        ViewMaps.RemoveAll(v => v.view == view);
        ViewMaps.Add(new ViewMapPair(view));
    }
}





public class SlotController : MonoBehaviour
{
    private static SlotController _instance;
    public static SlotController Instance
    {
        get
        {
            if (_instance == null)
            {
                // 你可以根据需要在这里处理实例化逻辑
                _instance = FindObjectOfType<SlotController>();
                if (_instance == null)
                {
                    Debug.LogError("SlotController not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("手持物虚影")]
    public GameObject HoldingSlot_obj;
    public ItemSlotUI HoldingSlot;
    private ItemContainer holdingContainer;
    // State（你也可以改成 holdingContainer.Items[0].IsEmpty 来判断）
    public bool holdingItem;
    public void InitHoldingContainer()
    {
        holdingContainer = new ItemContainer(1,"holdingContainer");
        List<ItemSlotUI> slots = new List<ItemSlotUI>();
        slots.Add(HoldingSlot);
        ContainerView holdingView = new ContainerView(holdingContainer, slots);
        holdingContainer.RegistryView(holdingView);
        ClearHoldingItem();
    }
    private void Awake()
    {
        InitHoldingContainer();
    }
    public List<ItemSlotUI> LoadItemSlots(Transform parent)
    {
        List<ItemSlotUI> slots = new List<ItemSlotUI>();
        foreach (Transform child in parent)
        {
            slots.Add(child.GetComponentInChildren<ItemSlotUI>());
        }
        return slots;
    }

    //获取某单元格物品信息
    public bool TryGetItem(ItemContainer container, int containerIndex, out ItemStack item)
    {
        if (container.Items[containerIndex].IsEmpty)
        {
            item = default;
            return false;
        }
        
        item = container.Items[containerIndex];

        return true;

    }

    //尝试添加物品
    public bool TryAddItem(int newItemID, int count, ItemContainer container)
    {
        if (count <= 0) return true;
        ItemBase_SO def = ItemRegistry.Get(newItemID);
        if (def == null)
        {
            Debug.LogError($"ItemRegistry.Get({newItemID}) 返回 null");
            return false;
        }

        // 先尝试堆叠
        for (int i = 0; i < container.SlotCount; i++)
        {
            if (container.Items[i].IsEmpty) continue;
            if (container.Items[i].itemId != newItemID) continue;

            int max = def.StackAmount;
            int canAdd = max - container.Items[i].count;
            if (canAdd <= 0) continue;

            int add = Mathf.Min(canAdd, count);
            container.Items[i].count += add;
            count -= add;

            RefreshSlot(container, i);

            if (count <= 0) return true;
        }

        // 再找空格
        for (int i = 0; i < container.SlotCount; i++)
        {
            if (!container.Items[i].IsEmpty) continue;

            int put = Mathf.Min(def.StackAmount, count);
            container.Items[i].itemId = newItemID;
            container.Items[i].count = put;
            count -= put;

            RefreshSlot(container, i);

            if (count <= 0) return true;
        }

        return false;
    }
    public bool TryAddItem(string newItemID, int count, ItemContainer container)
    {
        if (count <= 0) return true;

        ItemBase_SO def = ItemRegistry.Get(newItemID);
        if (def == null)
        {
            Debug.LogError($"ItemRegistry.Get({newItemID}) 返回 null");
            return false;
        }

        // 先尝试堆叠
        for (int i = 0; i < container.SlotCount; i++)
        {
            if (container.Items[i].IsEmpty) continue;
            if (ItemRegistry.Get(container.Items[i].itemId).ID_str != newItemID) continue;

            int max = def.StackAmount;
            int canAdd = max - container.Items[i].count;
            if (canAdd <= 0) continue;

            int add = Mathf.Min(canAdd, count);
            container.Items[i].count += add;
            count -= add;

            RefreshSlot(container, i);

            if (count <= 0) return true;
        }

        // 再找空格
        for (int i = 0; i < container.SlotCount; i++)
        {
            if (!container.Items[i].IsEmpty) continue;

            int put = Mathf.Min(def.StackAmount, count);
            container.Items[i].itemId = ItemRegistry.Get(newItemID).ID_num;
            container.Items[i].count = put;
            count -= put;

            RefreshSlot(container, i);

            if (count <= 0) return true;
        }

        return false;
    }
    public bool TryAddItem(ItemStack newStack, ItemContainer container)
    {
        if (newStack.count <= 0) return true;

        ItemBase_SO def = ItemRegistry.Get(newStack.itemId);
        if (def == null)
        {
            Debug.LogError($"ItemRegistry.Get({newStack.itemId}) 返回 null");
            return false;
        }

        // 先尝试堆叠
        for (int i = 0; i < container.SlotCount; i++)
        {
            if (container.Items[i].IsEmpty) continue;
            if (container.Items[i].itemId != newStack.itemId) continue;

            int max = def.StackAmount;
            int canAdd = max - container.Items[i].count;
            if (canAdd <= 0) continue;

            int add = Mathf.Min(canAdd, newStack.count);
            container.Items[i].count += add;
            newStack.count -= add;

            RefreshSlot(container, i);

            if (newStack.count <= 0) return true;
        }

        // 再找空格
        for (int i = 0; i < container.SlotCount; i++)
        {
            if (!container.Items[i].IsEmpty) continue;

            int put = Mathf.Min(def.StackAmount, newStack.count);
            container.Items[i].itemId = newStack.itemId;
            container.Items[i].count = put;
            newStack.count -= put;

            RefreshSlot(container, i);

            if (newStack.count <= 0) return true;
        }

        return false;
    }

    //设置某单元格物品数量（慎用）
    public void SetItemCount(ItemContainer container, int containerIndex, int count)
    {
        if (container == null)
        {
            Debug.LogError("SetItemCount failed: container is null.");
            return;
        }

        if (containerIndex < 0 || containerIndex >= container.SlotCount)
        {
            Debug.LogError($"SetItemCount failed: index out of range. index={containerIndex}, slotCount={container.SlotCount}");
            return;
        }

        ref ItemStack stack = ref container.Items[containerIndex];

        // 空槽不能直接设置为正数，因为没有 itemId 可参考堆叠上限
        if (stack.IsEmpty)
        {
            if (count > 0)
            {
                Debug.LogWarning($"SetItemCount ignored: slot {containerIndex} is empty, cannot set positive count without itemId.");
            }
            else
            {
                stack.Clear();
                RefreshSlot(container, containerIndex);
            }
            return;
        }

        // <= 0 直接清空
        if (count <= 0)
        {
            stack.Clear();
            RefreshSlot(container, containerIndex);
            return;
        }

        ItemBase_SO def = ItemRegistry.Get(stack.itemId);
        if (def == null)
        {
            Debug.LogError($"SetItemCount failed: ItemRegistry.Get({stack.itemId}) returned null.");
            return;
        }

        // 限制到最大堆叠
        stack.count = Mathf.Min(count, def.StackAmount);

        RefreshSlot(container, containerIndex);
    }
    public bool TryDivideStack(ref ItemStack origin, int count, out ItemStack result)
    {
        result = default;
        if (count <= 0 || count > origin.count) return false;

        result = new ItemStack
        {
            itemId = origin.itemId,
            count = count,
            extra = origin.extra
        };

        origin.count -= count;
        if (origin.count <= 0) origin.Clear();

        return true;
    }

    //同步指定单元格UI
    public void RefreshSlot(ItemContainer container, int containerIndex)
    {
        int temp = 0;
        foreach (var viewPair in container.ViewMaps)
        {
            temp++;
            int idx = viewPair.GetViewIndex(containerIndex);
               // Debug.Log($"UIIndex:{containerIndex} ViewIndex:{idx}");
            if(idx < 0 || idx >= viewPair.view.UISlotCount)
            {
                continue;
            }

            ItemStack s = container.Items[containerIndex];
            ItemSlotUI ui = viewPair.view.UISlots[idx];

            if (s.IsEmpty)
            {
                ui.HideSlot(); // 你已有：隐藏icon/数量
                continue;
            }

            var def = ItemRegistry.Get(s.itemId);
            ui.ShowSlot(); 
            ui.SetIcon(def.ItemSprite);
            ui.SetNum(s.count.ToString());
        }

    }

    //全部单元格UI强制同步一遍
    public void RefreshAll(ItemContainer container) 
    {
        Debug.Log("2333" + container.SlotCount);
        for (int i = 0; i < container.SlotCount; i++)
            RefreshSlot(container, i);
    }

    //清空手持物
    public void ClearHoldingItem()
    {
        holdingContainer.Items[0].Clear();
        holdingItem = false;
        RefreshSlot(holdingContainer, 0);
    }

    //拿起全部（玩家操作，禁止除玩家输入控制器ItemSlotUI以外直接调用,懒得写friend类了，切记，切记！！！）
    public void PickUpAll(ItemContainer container, int containerIndex)
    {
        if (container.Items[containerIndex].IsEmpty) return;

        holdingContainer.Items[0] = container.Items[containerIndex];
        holdingItem = true;

        container.Items[containerIndex].Clear();

        RefreshSlot(container, containerIndex);
        RefreshSlot(holdingContainer, 0);
        //ShowPhantom();
    }

    //放下所有或交换物体，取决指定格是否为空（玩家操作，禁止除玩家输入控制器ItemSlotUI以外直接调用,懒得写friend类了，切记，切记！！！）
    public void PlaceAllOrSwap(ItemContainer container, int containerIndex)
    {
        //int idx = view.GetContainerIndex(uiSlotIndex);

        // 目标空：直接放下
        if (container.Items[containerIndex].IsEmpty)
        {
            container.Items[containerIndex] = holdingContainer.Items[0];
            RefreshSlot(container, containerIndex);
            ClearHoldingItem();
            //HidePhantom();
            return;
        }

        // 同物品：尝试合并
        if (container.Items[containerIndex].itemId == holdingContainer.Items[0].itemId)
        {
            var def = ItemRegistry.Get(holdingContainer.Items[0].itemId);
            int max = def.StackAmount;

            int canAdd = max - container.Items[containerIndex].count;
            if (canAdd <= 0) return;

            int add = Mathf.Min(canAdd, holdingContainer.Items[0].count);
            container.Items[containerIndex].count += add;
            holdingContainer.Items[0].count -= add;

            if (holdingContainer.Items[0].count <= 0)
                ClearHoldingItem();

            RefreshSlot(container, containerIndex);
            RefreshSlot(holdingContainer, 0);
            return;
        }

        // 不同物品：交换
        var temp = container.Items[containerIndex];
        container.Items[containerIndex] = holdingContainer.Items[0];
        holdingContainer.Items[0] = temp;

        RefreshSlot(container, containerIndex);
        RefreshSlot(holdingContainer, 0);
    }

    //拿一半（玩家操作，禁止除玩家输入控制器ItemSlotUI以外直接调用,懒得写friend类了，切记，切记！！！）
    public void PickUpHalf(ItemContainer container, int containerIndex)
    {
        
        var src = container.Items[containerIndex];
        if (src.IsEmpty) return;

        int take = src.count / 2;
        if (take <= 0) take = 1;

        if (TryDivideStack(ref container.Items[containerIndex], take, out holdingContainer.Items[0]))
        {
            holdingItem = true;
            RefreshSlot(container, containerIndex);
            RefreshSlot(holdingContainer, 0);
        }
    }

    //放一个（玩家操作，禁止除玩家输入控制器ItemSlotUI以外直接调用,懒得写friend类了，切记，切记！！！）
    public void PlaceOne(ItemContainer container, int containerIndex)
    {
        if (!holdingItem || holdingContainer.Items[0].count <= 0)
        {
            ClearHoldingItem();
            return;
        }

        // 目标空：放 1
        if (container.Items[containerIndex].IsEmpty)
        {
            container.Items[containerIndex] = new ItemStack
            {
                itemId = holdingContainer.Items[0].itemId,
                count = 1,
                extra = holdingContainer.Items[0].extra
            };

            holdingContainer.Items[0].count--;
            if (holdingContainer.Items[0].count <= 0) ClearHoldingItem();

            RefreshSlot(container, containerIndex);
            RefreshSlot(holdingContainer, 0);
            return;
        }

        // 目标非空但不是同物品：不做
        if (container.Items[containerIndex].itemId != holdingContainer.Items[0].itemId)
            return;

        // 同物品：+1（不超过上限）
        var def = ItemRegistry.Get(holdingContainer.Items[0].itemId);
        if (container.Items[containerIndex].count >= def.StackAmount)
            return;

        container.Items[containerIndex].count++;
        holdingContainer.Items[0].count--;
        if (holdingContainer.Items[0].count <= 0) ClearHoldingItem();

        RefreshSlot(container, containerIndex);
        RefreshSlot(holdingContainer, 0);
    }

    //收集全部同类（玩家操作，禁止除玩家输入控制器ItemSlotUI以外直接调用,懒得写friend类了，切记，切记！！！）
    public void CollectAll(ItemContainer container)
    {
        if (!holdingItem) return;

        ref ItemStack held = ref holdingContainer.Items[0];
        if (held.IsEmpty) { holdingItem = false; return; }

        var def = ItemRegistry.Get(held.itemId);
        if (def == null) return;

        int max = def.StackAmount;

        for (int i = 0; i < container.SlotCount; i++)
        {
            ref ItemStack s = ref container.Items[i];
            if (s.IsEmpty) continue;
            if (s.itemId != held.itemId) continue;
            if (s.count == max) continue;

            int free = max - held.count;
            if (free <= 0) break; // 手持满了，结束

            int take = Mathf.Min(free, s.count); // 从容器拿
            if (take <= 0) continue;

            s.count -= take;
            held.count += take;

            if (s.count <= 0) s.Clear();

            RefreshSlot(container, i);
        }

        RefreshSlot(holdingContainer, 0);

        if (held.count <= 0) ClearHoldingItem(); // 理论上不会发生，但防御一下
    }

    //好像没用到但不敢删
    private void ShowPhantom() => HoldingSlot_obj.SetActive(true);
    private void HidePhantom() => HoldingSlot_obj.SetActive(false);
}
