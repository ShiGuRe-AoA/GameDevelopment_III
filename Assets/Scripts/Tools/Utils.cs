using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    /// <summary>
    /// 递归遍历 root 及其全部子物体，查找所有挂载了 T 组件的对象，并加入 results。
    /// </summary>
    public static void CollectComponentsInChildren<T>(Transform root, List<T> results) where T : Component
    {
        if (root == null || results == null)
            return;

        T component = root.GetComponent<T>();
        if (component != null)
        {
            results.Add(component);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectComponentsInChildren(root.GetChild(i), results);
        }
    }
}