using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{

    public int hotbarSize = 10; // 1~0 = 10 格（0 通常映射第10格）
    public int selectedIndex = 0;
    public InventoryContainer inventoryContainer;

    void Update()
    {
        int idx = GetHotbarNumberKeyDown(hotbarSize);
        if (idx != -1)
        {
            Debug.Log($"InputDetected:{idx}");
            inventoryContainer.SetCurrentSlot(idx);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f)
        {
            inventoryContainer.PreviousSlot();
        }
        else if (scroll < 0f)
        {
            inventoryContainer.NextSlot();
        }
    }

    // 返回 0..hotbarSize-1；没按返回 -1
    static int GetHotbarNumberKeyDown(int hotbarSize)
    {
        // 1..9 -> 0..8
        int max9 = Mathf.Min(9, hotbarSize);
        for (int n = 1; n <= max9; n++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + n)))
                return n - 1;
        }

        // 0 -> 第10格（索引 9）
        if (hotbarSize >= 10 && Input.GetKeyDown(KeyCode.Alpha0))
            return 9;

        return -1;
    }
}
