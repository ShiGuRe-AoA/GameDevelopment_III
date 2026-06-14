using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 金币显示 UI 组件。
/// 挂载在金币 UI 根 GameObject 上，负责：
/// 1. 实时同步 WorldState.coin 到 TMP_Text
/// 2. coin 变化时对图标播放放大→复原的缩放动效
/// </summary>
public class CoinDisplayUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Transform iconTarget; // 缩放动效目标（金币图标）

    [Header("动效参数")]
    [SerializeField] private float scaleTo      = 1.3f;
    [SerializeField] private float scaleUpTime  = 0.1f;
    [SerializeField] private float scaleDownTime = 0.15f;

    private int lastDisplayedCoin;

    private void OnEnable()
    {
        WorldState.OnCoinChanged += OnCoinChanged;
        // 初始化时立即同步一次
        if (WorldState.Instance != null)
        {
            lastDisplayedCoin = WorldState.Instance.coin;
            UpdateText(lastDisplayedCoin);
        }
    }

    private void OnDisable()
    {
        WorldState.OnCoinChanged -= OnCoinChanged;
        iconTarget?.DOKill();
    }

    private void OnDestroy()
    {
        iconTarget?.DOKill();
    }

    private void OnCoinChanged(int newValue)
    {
        UpdateText(newValue);
        PlayBounceAnimation();
    }

    private void UpdateText(int value)
    {
        if (coinText != null)
            coinText.text = value.ToString();
    }

    private void PlayBounceAnimation()
    {
        if (iconTarget == null) return;

        iconTarget.DOKill(complete: true);
        iconTarget.localScale = Vector3.one;

        iconTarget.DOScale(scaleTo, scaleUpTime)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (iconTarget != null)
                    iconTarget.DOScale(1f, scaleDownTime).SetEase(Ease.InBack);
            });
    }
}
