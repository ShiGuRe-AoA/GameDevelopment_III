using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class InventoryContainer : MonoBehaviour
{
    private ItemContainer backpackContainer;

    private int currentSlot;
    public List<ItemSlotUI> hotbarSlots = new();
    public Image ChoiceBox;
    public float Padding;
    private ContainerView hotbarView;
    private int slotCount;

    [Header("PlacementFeature")]

    private Vector3 _mousePos;
    private Vector3 MousePos
    {
        get { return _mousePos; }
        set
        {
            _mousePos = value;
            holdTickContext.MousePos = value;
            holdInteractContext.MousePos = value;
        }
    }
    public ItemStack CurrentStack => backpackContainer.Items[currentSlot];
    public ItemBase_SO CurrentItem
    {
        get
        {
            var stack = CurrentStack;
            return ItemRegistry.Get(stack.itemId);
        }
    }

    private HoldTickContext holdTickContext;
    [SerializeField] private Material PhantomMat_G;
    [SerializeField] private Material PhantomMat_R;
    private GameObject PlacementInstance;
    private List<GameObject> CellInstance;

    private HoldInteractContext holdInteractContext;
    private int placementItemID;
    private bool isValid;

    private ExitSelectContext exitSelectContext;

    private EnterSelectContext enterSelectContext;
    [SerializeField] private GameObject cellPrefab;



    private void Update()
    {
        //TODO:鼠标位置后续一定通过InputManger储存，并从中调用，严谨私自获取输入信息
        MousePos =  Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("MouseUp");
            //调用交互接口
            holdInteractContext.isValid = isValid;
            if (CurrentItem != null)
            {
                foreach (var feature in CurrentItem.Features)
                {
                    if (feature is IHoldInteract i)
                    {
                        holdInteractContext.ItemID = CurrentStack.itemId;
                        holdInteractContext.containerIndex = currentSlot;
                        i.OnHoldInteract(holdInteractContext);

                        if(CurrentStack.count <= 0)
                        {
                            if (feature is IExitSelect j)
                            {
                                j.ExitSelect(exitSelectContext, ref PlacementInstance, ref CellInstance);
                            }
                        }
                    }
                }
            }
        }

        //调用帧更新接口
        if(CurrentItem != null)
        {
            foreach (var feature in CurrentItem.Features)
            {
                if(feature is IHoldTick i)
                {
                    holdTickContext.PlacementInstance = PlacementInstance;
                    holdTickContext.CellInstance = CellInstance;
                    i.OnHoldTick(holdTickContext, out isValid);
                }
            }
        }
    }
    public void Init(ItemContainer backpackContainer)
    {
        this.backpackContainer = backpackContainer;
        hotbarView = new ContainerView(backpackContainer, hotbarSlots);
        backpackContainer.RegistryView(hotbarView);
        //hotbarView = new ContainerView(backpackContainer, hotbarSlots, uiIndex => uiIndex);
        slotCount = hotbarSlots.Count;
        SetCurrentSlot(0);

        holdTickContext = new HoldTickContext();
        holdTickContext.PhantomMat_G = PhantomMat_G;
        holdTickContext.PhantomMat_R = PhantomMat_R;


        holdInteractContext = new HoldInteractContext();
        holdInteractContext.backpackContainer = backpackContainer;

        exitSelectContext = new ExitSelectContext();

        enterSelectContext = new EnterSelectContext();
        enterSelectContext.cellPrefab = cellPrefab;
    }
    public void SetCurrentSlot(int incomIndex)
    {
        if (currentSlot == incomIndex) { return; }
        if (incomIndex >= slotCount)
        {
            incomIndex = 0;
        }
        if (incomIndex < 0)
        {
            incomIndex = slotCount - 1;
        }

        //调用退出选择接口
        if (CurrentItem != null)
            foreach (var feature in CurrentItem.Features)
        {
            if (feature is IExitSelect i)
            {
                i.ExitSelect(exitSelectContext, ref PlacementInstance, ref CellInstance);
            }
        }

        currentSlot = incomIndex;

        //调用进入选择接口
        if (CurrentItem != null)
            foreach (var feature in CurrentItem.Features)
        {
            if (feature is IEnterSelect i)
            {
                //设置contex参数
                if(feature is Feature_Placement placementFeature)
                {
                    enterSelectContext.placementPrefab = placementFeature.prefabObj;
                }
                i.EnterSelect(enterSelectContext, out PlacementInstance, out CellInstance);
            }
        }

        //同步UI
        AttachHighlightWithPadding(ChoiceBox.rectTransform, (RectTransform)hotbarSlots[currentSlot].transform, Padding);
    }
    public static void AttachHighlightWithPadding(RectTransform highlight, RectTransform slot, float pad)
    {
        highlight.SetParent(slot, worldPositionStays: false);

        // 以父物体中心为基准
        highlight.anchorMin = highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);

        highlight.anchoredPosition = Vector2.zero;

        // 让框比格子大一圈：父宽高 + 2*pad
        highlight.sizeDelta = new Vector2(pad * 2f, pad * 2f);
    }
    public void NextSlot()
    {
        SetCurrentSlot(currentSlot + 1);
    }
    public void PreviousSlot()
    {
        SetCurrentSlot(currentSlot - 1);
    }
}
