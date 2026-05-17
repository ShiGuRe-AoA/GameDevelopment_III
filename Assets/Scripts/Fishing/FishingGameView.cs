using System;
using UnityEngine;

public class FishingGameView : MonoBehaviour
{
    public void Open(FishingSession session, Action<bool> onFinished)
    {
        Debug.Log($"进入钓鱼界面，测试直接成功：{session.HookedFish.itemId}");
        onFinished?.Invoke(true);
    }
}