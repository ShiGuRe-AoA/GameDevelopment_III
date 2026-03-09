using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIFollowMouse : MonoBehaviour
{
    public enum OffsetMode
    {
        ScreenPixels,   // 偏移按屏幕像素（会受缩放影响）
        CanvasUnits     // 偏移按Canvas单位（推荐，更稳定）
    }

    [Header("References")]
    [Tooltip("不填则自动从父级寻找 Canvas")]
    [SerializeField] private Canvas canvas;

    [Tooltip("不填则自动使用当前物体的 RectTransform")]
    [SerializeField] private RectTransform target;

    [Header("Options")]
    [SerializeField] private OffsetMode offsetMode = OffsetMode.CanvasUnits;

    [Tooltip("ScreenPixels 模式：以屏幕像素为单位")]
    [SerializeField] private Vector2 pixelOffset = new Vector2(18f, -18f);

    [Tooltip("CanvasUnits 模式：以 Canvas 的 anchoredPosition 单位为单位（更稳定）")]
    [SerializeField] private Vector2 canvasOffset = new Vector2(18f, -18f);

    [SerializeField] private bool followEveryFrame = true;

    private RectTransform canvasRect;



    private void Reset()
    {
        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }


    private void Awake()
    {
        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        if (target == null) target = transform as RectTransform;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.transform as RectTransform;
    }

    private void OnEnable()
    {
        UpdatePosition();
    }

    private void Update()
    {
        if (followEveryFrame) UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (target == null || canvas == null || canvasRect == null) return;

        var screenPos = (Vector2)Input.mousePosition;

        Camera cam = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out var localPos))
            return;

        // localPos 是 Canvas 的局部坐标（anchoredPosition 使用的那套单位）
        if (offsetMode == OffsetMode.CanvasUnits)
        {
            target.anchoredPosition = localPos + canvasOffset;
        }
        else
        {
            // 把屏幕像素偏移转换到 Canvas 坐标系里（关键：除以 scaleFactor）
            float scale = 1f;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && scaler != null)
                scale = Mathf.Max(0.0001f, canvas.scaleFactor);

            target.anchoredPosition = localPos + pixelOffset / scale;
        }
    }
}