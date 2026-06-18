using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 使 UI Panel 可被鼠标拖动。
/// 挂载到 Panel 的根 GameObject 上（需要有 RectTransform）。
/// 拖拽标题栏时，可额外指定一个 DragHandle（子物体），只在该区域内响应拖动。
/// </summary>
public class UIDragPanel : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("拖动目标（留空则移动自身）")]
    [SerializeField] private RectTransform targetPanel;

    [Header("拖动把手（留空则整个 Panel 都可拖动）")]
    [SerializeField] private RectTransform dragHandle;

    [Header("限制在 Canvas 内")]
    [SerializeField] private bool clampToCanvas = true;

    [Header("移动到最前")]
    [SerializeField] private bool bringToFrontOnDrag = true;

    private Canvas parentCanvas;
    private Vector2 offset;

    private void Awake()
    {
        if (targetPanel == null)
            targetPanel = transform as RectTransform;

        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragHandle != null && !RectTransformUtility.RectangleContainsScreenPoint(
            dragHandle, eventData.position, eventData.pressEventCamera))
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetPanel, eventData.position, eventData.pressEventCamera, out offset);

        if (bringToFrontOnDrag)
            targetPanel.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragHandle != null && eventData.pointerDrag != gameObject)
            return;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)targetPanel.parent, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        targetPanel.localPosition = localPoint - offset;

        if (clampToCanvas)
            ClampToCanvas();
    }

    public void OnEndDrag(PointerEventData eventData) { }

    private void ClampToCanvas()
    {
        if (parentCanvas == null) return;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector3[] corners = new Vector3[4];
        targetPanel.GetWorldCorners(corners);

        Vector3 minCorner = corners[0];
        Vector3 maxCorner = corners[2];

        float canvasWidth = canvasRect.rect.width * parentCanvas.transform.localScale.x;
        float canvasHeight = canvasRect.rect.height * parentCanvas.transform.localScale.y;

        Vector3 pos = targetPanel.localPosition;
        float halfW = (maxCorner.x - minCorner.x) * 0.5f;
        float halfH = (maxCorner.y - minCorner.y) * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -canvasWidth * 0.5f + halfW, canvasWidth * 0.5f - halfW);
        pos.y = Mathf.Clamp(pos.y, -canvasHeight * 0.5f + halfH, canvasHeight * 0.5f - halfH);

        targetPanel.localPosition = pos;
    }
}
