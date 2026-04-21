using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FacingDirectionCompanion
{
    public FacingDirectionCompanion(Vector3Int offset, Sprite sprite)
    {
        this.offset = offset;
        this.sprite = sprite;
    }

    public Vector3Int offset;
    public Sprite sprite;
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Dictionary<Direction, FacingDirectionCompanion> facingDirection = new();
    private Vector3Int interactOffset = Vector3Int.up;

    private Vector3Int TilePosition => MainTile.WorldToCell(rb.position);
    public Vector3Int InteractTilePosition => MainTile.WorldToCell(rb.position) + interactOffset;

    public List<Sprite> FacingSprites = new List<Sprite>();
    public float MaxDirectionChange;
    public float MaxSpeed;
    public Tilemap MainTile;

    private Vector2 playerMoveDir_cur;
    private Vector2 playerMoveDir_exp;
    private Direction playerFacingDir = Direction.Down;

    public Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerInputContext playerInputContext;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        PlayerContext machineContext = new PlayerContext();

        machineContext.PlayerController = this;
        machineContext.InputContext = playerInputContext;
        machineContext.Player = transform;
        //context.Animator
        machineContext.Rb = rb;
        machineContext.MoveSpeed = MaxSpeed;

        StateMachine<PlayerContext> playerStateMachine = new(machineContext);
        playerStateMachine.ChangeState(new State_Idle(playerStateMachine, machineContext));
        StateMachineBrain.Instance.RegistryMachine(playerStateMachine, transform);



    InitialFacingDirection();
    }

    private void Start()
    {
    }

    private void Update()
    {
        UpdateMoveDirection(playerMoveDir_exp);
    }

    private void FixedUpdate()
    {
        UpdatePosition(playerMoveDir_cur);
    }

    private void InitialFacingDirection()
    {
        FacingDirectionCompanion upCompanion = new(
            new Vector3Int(0, 1),
            FacingSprites[0]
        );

        FacingDirectionCompanion leftCompanion = new(
            new Vector3Int(-1, 0),
            FacingSprites[1]
        );

        FacingDirectionCompanion downCompanion = new(
            new Vector3Int(0, -1),
            FacingSprites[2]
        );

        FacingDirectionCompanion rightCompanion = new(
            new Vector3Int(1, 0),
            FacingSprites[3]
        );

        facingDirection.Add(Direction.Up, upCompanion);
        facingDirection.Add(Direction.Left, leftCompanion);
        facingDirection.Add(Direction.Down, downCompanion);
        facingDirection.Add(Direction.Right, rightCompanion);
    }

    /// <summary>
    /// 由外部输入控制器传入移动输入
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        input = input.normalized;
        playerMoveDir_exp = input;

        // 只有在有输入时才更新朝向
        if (input != Vector2.zero)
        {
            Direction curFacingDir = GetFacingDirection();
            playerFacingDir = curFacingDir;
            interactOffset = facingDirection[curFacingDir].offset;
            spriteRenderer.sprite = facingDirection[curFacingDir].sprite;
        }
    }

    private void UpdateMoveDirection(Vector2 input)
    {
        Vector2 dir = (playerMoveDir_exp - playerMoveDir_cur).normalized;
        float dist = (playerMoveDir_exp - playerMoveDir_cur).magnitude;

        float thisChange = MaxDirectionChange * Time.deltaTime;

        if (dist < thisChange)
        {
            playerMoveDir_cur = playerMoveDir_exp;
        }
        else
        {
            playerMoveDir_cur += thisChange * dir;
        }

        if (playerMoveDir_cur.magnitude > 1)
        {
            playerMoveDir_cur = playerMoveDir_cur.normalized;
        }
    }

    private void UpdatePosition(Vector2 dir)
    {
        rb.MovePosition(rb.position + dir * MaxSpeed * Time.fixedDeltaTime);
    }

    private Direction GetFacingDirection()
    {
        if (Mathf.Abs(playerMoveDir_exp.x) >= Mathf.Abs(playerMoveDir_exp.y))
            return playerMoveDir_exp.x >= 0f ? Direction.Right : Direction.Left;
        else
            return playerMoveDir_exp.y >= 0f ? Direction.Up : Direction.Down;
    }

    private void OnDrawGizmos()
    {
        if (MainTile == null || rb == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(MainTile.GetCellCenterWorld(TilePosition), 0.25f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(MainTile.GetCellCenterWorld(InteractTilePosition), 0.25f);
    }
}