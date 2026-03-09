
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonClickListener : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private UnityEvent onLeftClick;
    [SerializeField] private UnityEvent onRightClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            onLeftClick?.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            onRightClick?.Invoke();
        }
    }
}