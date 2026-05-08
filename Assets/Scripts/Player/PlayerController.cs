using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FacingDirectionCompanion
{
    public Vector3Int Offset;
    public Sprite Sprite;

    public FacingDirectionCompanion(Vector3Int offset, Sprite sprite)
    {
        Offset = offset;
        Sprite = sprite;
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    #region Inspector

    [Header("场景引用信息")]
    [SerializeField] private Tilemap mainTile;
    [SerializeField] private List<Sprite> facingSprites = new();
    [SerializeField] private Animator animator;

    [Header("移动信息")]
    [SerializeField] private float maxDirectionChange = 10f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("运行动态引用")]
    [SerializeField] private PlayerInputContext playerInputContext = new();

    #endregion

    #region 组件引用

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    #endregion

    #region 状态

    private StateMachine<PlayerContext> playerStateMachine;
    private PlayerContext machineContext;

    public bool canMove;
    public bool canInteract;

    #endregion

    #region 动画
    [SerializeField] private List<ActionDefinition_SO> IdelAction = new();
    [SerializeField] private List<ActionDefinition_SO> MoveAction = new();
    [SerializeField] private List<ActionDefinition_SO> TillingAction = new();
    [SerializeField] private List<ActionDefinition_SO> WateringAction = new();
    [SerializeField] private List<ActionDefinition_SO> HarvestAction = new();
    private readonly Dictionary<Direction, FacingDirectionCompanion> facingDirectionMap = new();
    private Direction playerFacingDir = Direction.Down;
    private Vector3Int interactOffset = Vector3Int.up;

    #endregion

    #region 动态存储数据

    private Vector2 playerMoveDirCur;
    private Vector2 playerMoveDirExp;

    #endregion

    #region Properties

    public Rigidbody2D Rb => rb;
    public Tilemap MainTile => mainTile;
    public float MaxSpeed => maxSpeed;
    public float MaxDirectionChange => maxDirectionChange;
    public Direction PlayerFacingDir => playerFacingDir;

    private Vector3Int TilePosition => mainTile.WorldToCell(rb.position);
    public Vector3Int InteractTilePosition => mainTile.WorldToCell(rb.position) + interactOffset;

    #endregion



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        InitializeFacingDirection();
        InitializeStateMachine();
    }

    private void Update()
    {
        UpdateMoveDirection();
    }

    private void FixedUpdate()
    {
        UpdatePosition();
    }

    private void InitializeStateMachine()
    {
        machineContext = new PlayerContext
        {
            PlayerController = this,
            InputContext = playerInputContext,
            Player = transform,
            Animator = animator,
            Rb = rb
        };

        playerStateMachine = new StateMachine<PlayerContext>(machineContext);
        playerStateMachine.ChangeState(new State_Idle(playerStateMachine, machineContext));

        StateMachineBrain.Instance.RegistryMachine(playerStateMachine, transform);

        ActionRegistry.RegistryAction(ActionRegistry.PlayerAction, IdelAction);
        ActionRegistry.RegistryAction(ActionRegistry.PlayerAction, MoveAction);
        ActionRegistry.RegistryAction(ActionRegistry.PlayerAction, TillingAction);
        ActionRegistry.RegistryAction(ActionRegistry.PlayerAction, WateringAction);
        ActionRegistry.RegistryAction(ActionRegistry.PlayerAction, HarvestAction);
    }

    private void InitializeFacingDirection()
    {
        if (facingSprites == null || facingSprites.Count < 4)
        {
            Debug.LogError("FacingSprites 至少需要按顺序配置 4 张：Up / Left / Down / Right", this);
            return;
        }

        facingDirectionMap.Clear();

        facingDirectionMap.Add(Direction.Up, new FacingDirectionCompanion(
            new Vector3Int(0, 1, 0),
            facingSprites[0]
        ));

        facingDirectionMap.Add(Direction.Left, new FacingDirectionCompanion(
            new Vector3Int(-1, 0, 0),
            facingSprites[1]
        ));

        facingDirectionMap.Add(Direction.Down, new FacingDirectionCompanion(
            new Vector3Int(0, -1, 0),
            facingSprites[2]
        ));

        facingDirectionMap.Add(Direction.Right, new FacingDirectionCompanion(
            new Vector3Int(1, 0, 0),
            facingSprites[3]
        ));

        ApplyFacing(Direction.Down);
    }

    /// <summary>
    /// 由外部输入控制器传入移动输入
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        playerMoveDirExp = input.normalized;

        if (playerMoveDirExp.sqrMagnitude > 0.0001f)
        {
            Direction facing = GetFacingDirection(playerMoveDirExp);
            ApplyFacing(facing);
        }
    }

    public void ClearMoveInput()
    {
        playerMoveDirExp = Vector2.zero;
    }

    public void SetInputInfo(Vector2 moveInput)
    {
        playerInputContext.MoveInput = moveInput;
    }

    public bool SimpleInteract()
    {
        playerStateMachine.PushState(new State_Interact(playerStateMachine, machineContext));
        return true;
    }
    public bool ToolInteract(List<ToolType> tools)
    {
        playerStateMachine.PushState(new State_UseTool(playerStateMachine, machineContext, tools));
        return true;
    }

    private void ApplyFacing(Direction direction)
    {
        if (!canMove){ return;}
        playerFacingDir = direction;

        if (!facingDirectionMap.TryGetValue(direction, out var companion))
            return;

        interactOffset = companion.Offset;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = companion.Sprite;
        }
    }

    private void UpdateMoveDirection()
    {
        Vector2 delta = playerMoveDirExp - playerMoveDirCur;
        float dist = delta.magnitude;
        float thisChange = maxDirectionChange * Time.deltaTime;

        if (dist <= 0.0001f)
            return;

        if (dist < thisChange)
        {
            playerMoveDirCur = playerMoveDirExp;
        }
        else
        {
            Vector2 dir = delta / dist;
            playerMoveDirCur += thisChange * dir;
        }

        if (playerMoveDirCur.sqrMagnitude > 1f)
        {
            playerMoveDirCur = playerMoveDirCur.normalized;
        }
    }

    private void UpdatePosition()
    {
        if (!canMove) { return; }
        //Debug.Log("Moving Position");
        rb.MovePosition(rb.position + playerMoveDirCur * maxSpeed * Time.fixedDeltaTime);
    }

    private Direction GetFacingDirection(Vector2 moveDir)
    {
        if (Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y))
            return moveDir.x >= 0f ? Direction.Right : Direction.Left;
        else
            return moveDir.y >= 0f ? Direction.Up : Direction.Down;
    }

    private void OnDrawGizmos()
    {
        if (mainTile == null || rb == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(mainTile.GetCellCenterWorld(TilePosition), 0.25f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(mainTile.GetCellCenterWorld(InteractTilePosition), 0.25f);
    }
}