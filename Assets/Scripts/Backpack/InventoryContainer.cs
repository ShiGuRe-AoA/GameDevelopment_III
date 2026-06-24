using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryContainer : MonoBehaviour
{
    private ItemContainer backpackContainer;

    [SerializeField] private PlayerController playerController;

    private int currentSlot;
    public List<ItemSlotUI> hotbarSlots = new();
    public Image ChoiceBox;
    public float Padding;
    private ContainerView hotbarView;

    [Header("PlacementFeature")]
    [SerializeField] private Material PhantomMat_G;
    [SerializeField] private Material PhantomMat_R;
    [SerializeField] private GameObject cellPrefab;

    private Vector3 mousePos;

    public ItemStack CurrentStack => backpackContainer?.Items != null ? backpackContainer.Items[currentSlot] : ItemStack.Empty;
    public ItemBase_SO CurrentItem => ItemRegistry.Get(CurrentStack.itemId);

    // Feature interaction contexts
    private HoldTickContext holdTickContext = new();
    private HoldInteractContext holdInteractContext = new();
    private ExitSelectContext exitSelectContext = new();
    private EnterSelectContext enterSelectContext = new();

    private GameObject PlacementInstance;
    private List<GameObject> CellInstance;
    private bool isValid;

    private void OnEnable()
    {
        InputManager.OnHotbarSlotSelected += OnInputHotbarSlot;
        InputManager.OnHotbarScroll += OnInputHotbarScroll;
        InputManager.OnItemInteract += OnInputItemInteract;
    }

    private void OnDisable()
    {
        InputManager.OnHotbarSlotSelected -= OnInputHotbarSlot;
        InputManager.OnHotbarScroll -= OnInputHotbarScroll;
        InputManager.OnItemInteract -= OnInputItemInteract;
    }

    private void Update()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        holdTickContext.MousePos = mousePos;
        holdInteractContext.MousePos = mousePos;

        if (CurrentItem == null) return;

        // 每帧 —— 调用 HoldTick
        ForEachFeature<IHoldTick>(feature =>
        {
            holdTickContext.PlacementInstance = PlacementInstance;
            holdTickContext.CellInstance = CellInstance;
            feature.OnHoldTick(holdTickContext, out isValid);
        });
    }

    private void OnInputItemInteract()
    {
        if (CurrentItem == null) return;

        holdInteractContext.isValid = isValid;
        ForEachFeature<IHoldInteract>(feature =>
        {
            holdInteractContext.ItemID = CurrentStack.itemId;
            holdInteractContext.containerIndex = currentSlot;
            holdInteractContext.InteractGrid = playerController.InteractTilePosition;
            feature.OnHoldInteract(holdInteractContext);

            if (CurrentStack.count <= 0)
            {
                ForEachFeature<IExitSelect>(f =>
                    f.ExitSelect(exitSelectContext, ref PlacementInstance, ref CellInstance));
            }
        });
    }

    private void OnInputHotbarSlot(int index)
    {
        SetCurrentSlot(index);
    }

    private void OnInputHotbarScroll(int direction)
    {
        if (direction > 0)
            NextSlot();
        else
            PreviousSlot();
    }

    public void Init(ItemContainer container)
    {
        backpackContainer = container;
        hotbarView = new ContainerView(container, hotbarSlots);
        container.RegistryView(hotbarView);
        SetCurrentSlot(0);

        holdTickContext.PhantomMat_G = PhantomMat_G;
        holdTickContext.PhantomMat_R = PhantomMat_R;

        holdInteractContext.backpackContainer = container;
        holdInteractContext.playerController = playerController;

        enterSelectContext.cellPrefab = cellPrefab;
    }

    public void SetCurrentSlot(int index)
    {
        int slotCount = hotbarSlots.Count;
        index = (index + slotCount) % slotCount;

        if (currentSlot == index) return;

        // 退出旧槽位
        ForEachFeature<IExitSelect>(f =>
            f.ExitSelect(exitSelectContext, ref PlacementInstance, ref CellInstance));

        currentSlot = index;

        // 进入新槽位
        ForEachFeature<IEnterSelect>(f =>
        {
            if (f is Feature_Placement placement)
                enterSelectContext.placementPrefab = placement.prefabObj;
            f.EnterSelect(enterSelectContext, out PlacementInstance, out CellInstance);
        });

        AttachHighlight(ChoiceBox.rectTransform, (RectTransform)hotbarSlots[currentSlot].transform, Padding);
    }

    public void NextSlot() => SetCurrentSlot(currentSlot + 1);
    public void PreviousSlot() => SetCurrentSlot(currentSlot - 1);

    //============================================================================================
    // Helpers
    //============================================================================================
    private void ForEachFeature<T>(System.Action<T> action) where T : class
    {
        if (CurrentItem == null) return;
        foreach (var feature in CurrentItem.Features)
        {
            if (feature is T typed)
                action(typed);
        }
    }

    private static void AttachHighlight(RectTransform highlight, RectTransform slot, float pad)
    {
        highlight.SetParent(slot, worldPositionStays: false);
        highlight.anchorMin = highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);
        highlight.anchoredPosition = Vector2.zero;
        highlight.sizeDelta = new Vector2(pad * 2f, pad * 2f);
    }
}
