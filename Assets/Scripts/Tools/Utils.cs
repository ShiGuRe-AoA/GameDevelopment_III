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

    /// <summary>
    /// 给 List<T> 创建一个包含前 Count 项的只读窗口
    /// </summary>
    
    public static IReadOnlyList<T> ReadOnly<T>(IList<T> origin, Func<int> getCount)
    {
        return new ReadOnlyList<T>(origin, getCount);
    }

    private class ReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly IList<T> origin;
        private readonly Func<int> getCount;

        public ReadOnlyList(IList<T> _origin, Func<int> _getCount)
        {
            this.origin = _origin ?? throw new ArgumentNullException(nameof(origin));
            this.getCount = _getCount ?? throw new ArgumentNullException(nameof(getCount));
        }

        public int Count
        {
            get
            {
                int count = getCount();
                if (count < 0) return 0;
                if (count > origin.Count) return origin.Count;
                return count;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return origin[index];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return origin[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}