using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaleContainer : ItemContainer_Base
{
    // 当前可用的数量(表现为 对外展示 ? 个商品)
    [SerializeField] private int interactableSlotCount;

    public bool IsOpen { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        SlotController.Instance.RefreshAll(container);
    }

    private void Start()
    {
        // test
        // 测试
        SlotController.Instance.TryAddItem(10100, 36, container);
        Debug.Log($"DebugItemAddComplete");
        SlotController.Instance.TryAddItem(10100, 12, container);
        SlotController.Instance.TryAddItem(10100, 4, container);
        SlotController.Instance.TryAddItem(10101, 36, container);
        SlotController.Instance.TryAddItem(10102, 5, container);
        SlotController.Instance.TryAddItem(14001, 32, container);
        SlotController.Instance.TryAddItem("Hoe_1", 1, container);
        SlotController.Instance.TryAddItem("WateringCan_1", 1, container);

    }
}
