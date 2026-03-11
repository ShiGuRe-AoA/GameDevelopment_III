using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countTex;

    public bool Interactable;
    public Sprite NullImage;
    private ItemContainer container;
    private ContainerView view;
    private ItemContainer_Base controller;
    private int slotId;
    public void Bind(ItemContainer c, ContainerView v, int id, ItemContainer_Base ctrl)
    {
        container = c;
        view = v;
        slotId = id;
        controller = ctrl;
    }

    private void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
        if (countTex == null) countTex = GetComponentInChildren<TMP_Text>();
    }
    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    public void SetNum(string num)
    {
        countTex.text = num;
    }
    public void ShowSlot()
    {
    }
    public void HideSlot()
    {
        SetIcon(NullImage);
        SetNum(string.Empty);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Interactable) { return; }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            controller.OnRightClick(container, slotId);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount == 1)
            {
                controller.OnLeftClick(container, slotId);
            }
            else if (eventData.clickCount == 2)
            {
                controller.OnDoubleLeftClick(container, slotId);
            }
        }
    }
}