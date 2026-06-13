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
        if (string.IsNullOrEmpty(actionName)) return;

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

    /// <summary>进入不可移动 / 不可交互的状态并播放动画（适用于钓鱼等待/咬钩等）</summary>
    protected void EnterImmobile(string actionName)
    {
        SetControl(canMove: false, canInteract: false);
        PlayAction(actionName);
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

//============================================================================================
// Idle / 待机
//============================================================================================
public class State_Idle : State_PlayerBase
{
    public State_Idle(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx) { }

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

        if (PlayerDirection != PreviousDirection)
        {
            PlayAction(GetIdleAction(PlayerDirection));
            PreviousDirection = PlayerDirection;
        }

        if (Ctx.InputContext.MoveInput.sqrMagnitude > 0.1f)
            Machine.ChangeState(new State_BasicMove(Machine, Ctx));
    }

    private string GetIdleAction(Direction dir) => GetDirectionAction(
        dir, "Player_Idle_Down", "Player_Idle_Left", "Player_Idle_Right", "Player_Idle_Up");
}

//============================================================================================
// BasicMove / 移动
//============================================================================================
public class State_BasicMove : State_PlayerBase
{
    public State_BasicMove(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx) { }

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

    private string GetMoveAction(Direction dir) => GetDirectionAction(
        dir, "Player_Move_Down", "Player_Move_Left", "Player_Move_Right", "Player_Move_Up");
}

//============================================================================================
// Interact / 交互
//============================================================================================
public enum InteractPhase
{
    None,
    Harvest,
    OpenDoor,
    Logging
}

public class State_Interact : State_PlayerBase
{
    private InteractPhase phase;

    public State_Interact(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx) { }

    public override void Enter()
    {
        base.Enter();
        SetControl(canMove: false, canInteract: false);

        phase = WorldState.Instance.DetectInteract(InteractTilePosition);
        if (phase == InteractPhase.None) { Machine.PopState(); return; }

        PlayAction(GetInteractAction(phase, PlayerDirection));
    }

    public override void Update()
    {
        base.Update();
        TickAction();
        if (actionRuntime == null) return;

        if (actionRuntime.CanApplyEffect())
        {
            WorldState.Instance.InteractAt(InteractTilePosition);
            actionRuntime.MarkEffectApplied();
        }

        if (actionRuntime.IsFinished())
            Machine.PopState();
    }

    private string GetInteractAction(InteractPhase phase, Direction dir)
    {
        return phase switch
        {
            InteractPhase.Harvest => GetDirectionAction(
                dir, "Player_Harvest_Down", "Player_Harvest_Left", "Player_Harvest_Right", "Player_Harvest_Up"),
            InteractPhase.OpenDoor => GetDirectionAction(
                dir, "Player_OpenDoor_Down", "Player_OpenDoor_Left", "Player_OpenDoor_Right", "Player_OpenDoor_Up"),
            _ => string.Empty
        };
    }
}

//============================================================================================
// UseTool / 使用工具
//============================================================================================
public class State_UseTool : State_PlayerBase
{
    private readonly List<ToolType> curTools;

    public State_UseTool(StateMachine<PlayerContext> machine, PlayerContext ctx, List<ToolType> toolTypes)
        : base(machine, ctx)
    {
        curTools = toolTypes;
    }

    public override void Enter()
    {
        base.Enter();
        SetControl(canMove: false, canInteract: false);
        PlayAction(GetToolAction(curTools[0], PlayerDirection));
    }

    public override void Update()
    {
        base.Update();
        TickAction();
        if (actionRuntime == null) return;

        if (actionRuntime.CanApplyEffect())
        {
            WorldState.Instance.ItemInteract(InteractTilePosition, curTools, Ctx);
            actionRuntime.MarkEffectApplied();
        }

        if (actionRuntime.IsFinished())
            Machine.PopState();
    }

    private string GetToolAction(ToolType tool, Direction dir)
    {
        return tool switch
        {
            ToolType.Hoe => GetDirectionAction(
                dir, "Player_Tilling_Down", "Player_Tilling_Left", "Player_Tilling_Right", "Player_Tilling_Up"),
            ToolType.WateringCan => GetDirectionAction(
                dir, "Player_Watering_Down", "Player_Watering_Left", "Player_Watering_Right", "Player_Watering_Up"),
            ToolType.FishingRod => GetDirectionAction(
                dir, "Player_Fishing_Down", "Player_Fishing_Left", "Player_Fishing_Right", "Player_Fishing_Up"),
            ToolType.Axe => GetDirectionAction(
                dir, "Player_Logging_Down", "Player_Logging_Left", "Player_Logging_Right", "Player_Logging_Up"),
            ToolType.Bell => GetDirectionAction(
                dir, "Player_RingBell_Down", "Player_RingBell_Left", "Player_RingBell_Right", "Player_RingBell_Up"),
            _ => string.Empty
        };
    }
}

//============================================================================================
// 钓鱼状态（合并三类：Wait / Bite / Game）
//============================================================================================
public class State_FishingWait : State_PlayerBase
{
    public State_FishingWait(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx) { }

    public override void Enter()
    {
        base.Enter();
        EnterImmobile(GetDirectionAction(PlayerDirection,
            "Player_FishingWait_Down", "Player_FishingWait_Left",
            "Player_FishingWait_Right", "Player_FishingWait_Up"));
    }

    public override void Update()
    {
        base.Update();
        TickAction();
    }
}

public class State_FishingBite : State_PlayerBase
{
    public State_FishingBite(StateMachine<PlayerContext> machine, PlayerContext ctx)
        : base(machine, ctx) { }

    public override void Enter()
    {
        base.Enter();
        EnterImmobile(GetDirectionAction(PlayerDirection,
            "Player_FishingBite_Down", "Player_FishingBite_Left",
            "Player_FishingBite_Right", "Player_FishingBite_Up"));
    }

    public override void Update()
    {
        base.Update();
        TickAction();
    }
}

public class State_FishingGame : State_PlayerBase
{
    private readonly FishingSession session; // 保留给钓鱼小游戏 UI 完成后使用

    public State_FishingGame(StateMachine<PlayerContext> machine, PlayerContext ctx, FishingSession session)
        : base(machine, ctx)
    {
        this.session = session;
    }

    public override void Enter()
    {
        base.Enter();
        EnterImmobile(GetDirectionAction(PlayerDirection,
            "Player_FishingGame_Down", "Player_FishingGame_Left",
            "Player_FishingGame_Right", "Player_FishingGame_Up"));
    }

    public override void Update()
    {
        base.Update();
        TickAction();
    }
}
