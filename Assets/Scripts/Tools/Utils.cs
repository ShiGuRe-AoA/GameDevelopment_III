using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public static class Utils
{
    /// <summary>
    /// 递归遍历 root 及其全部子物体，查找所有挂载了 T 组件的对象，并加入 results。
    /// 当传入参数 count 时则从头查找 count 个挂载了 T 组件的对象
    /// </summary>
    public static void CollectComponentsInChildren<T>(Transform root, List<T> results, int? count = null) where T : Component
    {
        if (root == null || results == null || count.HasValue && results.Count == count)
            return;

        T component = root.GetComponent<T>();
        if (component != null)
        {
            results.Add(component);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectComponentsInChildren(root.GetChild(i), results, count);
        }
    }
}