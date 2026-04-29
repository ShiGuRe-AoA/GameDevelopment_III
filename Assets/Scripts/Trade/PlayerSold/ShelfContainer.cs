using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfContainer : ItemContainer_Base
{
    // 仔细想想, shelfLevel不应该放到shelf里面,应该是一个比较全局的变量, 脚本里的应该只能get到全局里的值
    [SerializeField] private int shelfLevel;
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
