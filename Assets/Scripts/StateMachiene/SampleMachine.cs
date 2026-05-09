using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家状态基类：统一处理玩家动画、动作运行时、移动/交互权限。
/// </summary>
public abstract class State_PlayerBase : State<PlayerContext>
{
    protected ActionRuntime actionRuntime;

    protected Direction PlayerDirection => Ctx.PlayerController.PlayerFacingDir;
    protected Vector3Int InteractTilePosition => Ctx.PlayerController.InteractTilePosition;
    protected Direction PreviousDirection = Direction.Down;

    protected State_PlayerBase(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();
        PreviousDirection = PlayerDirection;
    }

    protected void PlayAction(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return;

        Ctx.Animator.Play(actionName);
        actionRuntime = new ActionRuntime(
            ActionRegistry.Get(ActionRegistry.PlayerAction, actionName)
        );
    }

    protected void TickAction()
    {
        actionRuntime?.Tick(Time.deltaTime);
    }

    protected void SetControl(bool canMove, bool canInteract)
    {
        Ctx.PlayerController.canMove = canMove;
        Ctx.PlayerController.canInteract = canInteract;
    }

    protected string GetDirectionAction(
        Direction dir,
        string down,
        string left,
        string right,
        string up)
    {
        return dir switch
        {
            Direction.Up => up,
            Direction.Left => left,
            Direction.Right => right,
            Direction.Down => down,
            _ => down
        };
    }
}

/// <summary>
/// 玩家待机状态。
/// </summary>
public class State_Idle : State_PlayerBase
{
    public State_Idle(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();

        SetControl(canMove: true, canInteract: true);
        Ctx.PlayerController.SetMoveInput(Ctx.InputContext.MoveInput);

        PlayAction(GetIdleAction(PlayerDirection));
    }

    public override void Resume()
    {
        base.Resume();

        SetControl(canMove: true, canInteract: true);
        Ctx.PlayerController.SetMoveInput(Ctx.InputContext.MoveInput);

        PlayAction(GetIdleAction(PlayerDirection));
    }
    public override void Update()
    {
        base.Update();

        TickAction();

        // 朝向变化时刷新待机动画
        if (PlayerDirection != PreviousDirection)
        {
            PlayAction(GetIdleAction(PlayerDirection));
            PreviousDirection = PlayerDirection;
        }

        if (Ctx.InputContext.MoveInput.sqrMagnitude > 0.1f)
        {
            Machine.ChangeState(new State_BasicMove(Machine, Ctx));
        }
    }

    private string GetIdleAction(Direction dir)
    {
        return GetDirectionAction(
            dir,
            "Player_Idle_Down",
            "Player_Idle_Left",
            "Player_Idle_Right",
            "Player_Idle_Up"
        );
    }
}

/// <summary>
/// 玩家基础移动状态。
/// </summary>
public class State_BasicMove : State_PlayerBase
{
    public State_BasicMove(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();

        SetControl(canMove: true, canInteract: true);
        PlayAction(GetMoveAction(PlayerDirection));
    }
    public override void Resume()
    {
        base.Resume();

        SetControl(canMove: true, canInteract: true);
        PlayAction(GetMoveAction(PlayerDirection));
    }
    public override void Update()
    {
        base.Update();

        TickAction();

        // 朝向变化时刷新移动动画
        if (PlayerDirection != PreviousDirection)
        {
            PlayAction(GetMoveAction(PlayerDirection));
            PreviousDirection = PlayerDirection;
        }

        if (Ctx.InputContext.MoveInput.sqrMagnitude < 0.0001f)
        {
            Machine.ChangeState(new State_Idle(Machine, Ctx));
            return;
        }

        Ctx.PlayerController.SetMoveInput(Ctx.InputContext.MoveInput);
    }

    private string GetMoveAction(Direction dir)
    {
        return GetDirectionAction(
            dir,
            "Player_Move_Down",
            "Player_Move_Left",
            "Player_Move_Right",
            "Player_Move_Up"
        );
    }
}
public enum InteractPhase
{
    None,
    Harvest,
    OpenDoor,
}

/// <summary>
/// 玩家交互状态。
/// </summary>
public class State_Interact : State_PlayerBase
{
    private InteractPhase phase;

    public State_Interact(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();

        SetControl(canMove: false, canInteract: false);

        phase = WorldState.Instance.DetectInteract(InteractTilePosition);

        if (phase == InteractPhase.None)
        {
            Machine.PopState();
            return;
        }

        PlayAction(GetInteractAction(phase, PlayerDirection));
    }

    public override void Update()
    {
        base.Update();

        TickAction();

        if (actionRuntime == null)
            return;

        // 动画到达效果帧时执行一次交互逻辑
        if (actionRuntime.CanApplyEffect())
        {
            WorldState.Instance.InteractAt(InteractTilePosition);

            actionRuntime.MarkEffectApplied();
        }

        if (actionRuntime.IsFinished())
        {
            Machine.PopState();
        }
    }

    private string GetInteractAction(InteractPhase phase, Direction dir)
    {
        return phase switch
        {
            InteractPhase.Harvest => GetDirectionAction(
                dir,
                "Player_Harvest_Down",
                "Player_Harvest_Left",
                "Player_Harvest_Right",
                "Player_Harvest_Up"
            ),

            _ => string.Empty
        };
    }
}

public class State_UseTool : State_PlayerBase
{
    public State_UseTool(StateMachine<PlayerContext> machine, PlayerContext ctx, List<ToolType> toolTypes) : base(machine, ctx)
    {
        curTools = toolTypes;
    }

    private List<ToolType> curTools;

    public override void Enter()
    {
        base.Enter();

        SetControl(canMove: false, canInteract: false);

        PlayAction(GetToolAction(curTools[0],PlayerDirection));
    }

    public override void Update()
    {
        base.Update();

        TickAction();

        if (actionRuntime == null)
            return;

        // 动画到达效果帧时执行一次交互逻辑
        if (actionRuntime.CanApplyEffect())
        {
            WorldState.Instance.ItemInteract(InteractTilePosition, curTools);

            actionRuntime.MarkEffectApplied();
        }

        if (actionRuntime.IsFinished())
        {
            Machine.PopState();
        }
    }

    private string GetToolAction(ToolType tool, Direction dir)
    {
        return tool switch
        {
            ToolType.Hoe => GetDirectionAction(
                dir,
                "Player_Tilling_Down",
                "Player_Tilling_Left",
                "Player_Tilling_Right",
                "Player_Tilling_Up"
            ),
            ToolType.WateringCan => GetDirectionAction(
                dir,
                "Player_Watering_Down",
                "Player_Watering_Left",
                "Player_Watering_Right",
                "Player_Watering_Up"
            ),

            _ => string.Empty
        };
    }
}