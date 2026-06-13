using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 商店商品按钮组件。
/// 挂载在商品按钮预制体上，负责：
/// 1. 显示商品图标、名称、价格
/// 2. 点击触发单次购买 → 播放缩放动画
/// 3. 长按触发连续购买 → 每次均走完整校验 + 播放动画
/// </summary>
public class ShopGoodsButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI 组件引用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Transform scaleTarget; // 购买缩放目标（默认 iconImage.transform）

    [Header("长按参数")]
    [SerializeField] private float longPressThreshold = 0.5f;
    [SerializeField] private float repeatInterval   = 0.15f;

    [Header("缩放动画")]
    [SerializeField] private float scaleTo   = 1.25f;
    [SerializeField] private float scaleUpTime  = 0.08f;
    [SerializeField] private float scaleDownTime = 0.1f;

    private ShopItemDefSO       itemDef;
    private ShopPanelController controller;
    private Coroutine           longPressCoroutine;
    private bool                isPressed;

    // ================================================================================
    // Init
    // ================================================================================
    public void Init(ShopItemDefSO def, ShopPanelController ctrl)
    {
        itemDef    = def;
        controller = ctrl;

        if (scaleTarget == null)
            scaleTarget = iconImage != null ? iconImage.transform : transform;

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (itemDef?.Item == null) return;

        if (iconImage != null)  iconImage.sprite = itemDef.Item.ItemSprite;
        if (nameText  != null)  nameText.text    = itemDef.Item.Name;
        if (priceText != null)  priceText.text   = itemDef.Price.ToString();
    }

    // ================================================================================
    // 指针事件
    // ================================================================================
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 首次点击立即购买一次
        TryBuyOnce();

        // 开始长按检测
        isPressed = true;
        longPressCoroutine = StartCoroutine(LongPressRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)  => StopLongPress();
    public void OnPointerExit(PointerEventData eventData) => StopLongPress();

    private void OnDisable() => StopLongPress();
    private void OnDestroy()
    {
        StopLongPress();
        scaleTarget?.DOKill();
    }

    // ================================================================================
    // 长按
    // ================================================================================
    private void StopLongPress()
    {
        isPressed = false;
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private System.Collections.IEnumerator LongPressRoutine()
    {
        yield return new WaitForSeconds(longPressThreshold);
        while (isPressed && itemDef != null && controller != null)
        {
            TryBuyOnce();
            yield return new WaitForSeconds(repeatInterval);
        }
    }

    // ================================================================================
    // 购买 & 动画
    // ================================================================================
    private void TryBuyOnce()
    {
        if (controller == null || itemDef == null) return;

        if (controller.TryBuy(itemDef))
            PlayBuyAnimation();
    }

    private void PlayBuyAnimation()
    {
        if (scaleTarget == null) return;

        // Kill 旧动画避免叠加异常
        scaleTarget.DOKill(complete: true);
        scaleTarget.localScale = Vector3.one;

        scaleTarget.DOScale(scaleTo, scaleUpTime)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (scaleTarget != null)
                    scaleTarget.DOScale(1f, scaleDownTime).SetEase(Ease.InBack);
            });
    }
}
