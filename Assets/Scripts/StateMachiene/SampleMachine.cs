using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State_BasicMove : State<PlayerContext>
{
    private Direction curDirction;
    private ActionRuntime actionRuntime;
    public State_BasicMove(StateMachine<PlayerContext> machine, PlayerContext ctx) : base(machine, ctx)
    {
    }


    public override void Enter()
    {
        Debug.Log("Enter Move State");
        base.Enter();
        Ctx.PlayerController.canMove = true;
        Ctx.PlayerController.canInteract = true;

        curDirction = Ctx.PlayerController.PlayerFacingDir;
        PlayAction(GetCurAnima(curDirction));
    }
    public override void Update()
    {
        base.Update();

        if (actionRuntime != null) { actionRuntime.Tick(Time.deltaTime); }

        if (curDirction != Ctx.PlayerController.PlayerFacingDir)
        {
            curDirction = Ctx.PlayerController.PlayerFacingDir;
            PlayAction(GetCurAnima(curDirction));
        }


        if (Ctx.InputContext.MoveInput.sqrMagnitude < 0.0001f)
        {
            Machine.ChangeState(new State_Idle(Machine, Ctx));
            return;
        }
        else
        {
            Ctx.PlayerController.SetMoveInput(Ctx.InputContext.MoveInput);
        }
    }

    private string GetCurAnima(Direction dir)
    {
        string Action_MoveDown = "Player_Move_Down";
        string Action_MoveLeft = "Player_Move_Left";
        string Action_MoveRight = "Player_Move_Right";
        string Action_MoveUp = "Player_Move_Up";

        string curDirAnima;
        switch (Ctx.PlayerController.PlayerFacingDir)
        {
            case Direction.Up:
                curDirAnima = Action_MoveUp;
                break;
            case Direction.Left:
                curDirAnima = Action_MoveLeft;
                break;
            case Direction.Down:
                curDirAnima = Action_MoveDown;
                break;
            case Direction.Right:
                curDirAnima = Action_MoveRight;
                break;
            default:
                curDirAnima = Action_MoveDown;
                break;
        }
        return curDirAnima;
    }
    public void PlayAction(string anima)
    {
        Ctx.Animator.Play(anima);
        actionRuntime = new ActionRuntime(ActionRegistry.Get(ActionRegistry.PlayerAction, anima));

    }
}

public class State_Idle : State<PlayerContext>
{
    private Direction curDirction;
    private ActionRuntime actionRuntime;
    public State_Idle(StateMachine<PlayerContext> machine, PlayerContext ctx) : base(machine, ctx)
    {
    }
    public override void Enter()
    {
        Debug.Log("Enter Idel State");
        base.Enter();
        Ctx.PlayerController.canMove = true;
        Ctx.PlayerController.canInteract = true;
        Ctx.PlayerController.SetMoveInput(Ctx.InputContext.MoveInput);

        curDirction = Ctx.PlayerController.PlayerFacingDir;
        PlayAction(GetCurAnima(curDirction));
    }
    public override void Update()
    {
        base.Update();

        if(actionRuntime != null) { actionRuntime.Tick(Time.deltaTime) ; }



        Debug.Log($"静止状态移动输入：{Ctx.InputContext.MoveInput.sqrMagnitude}");
        if (curDirction != Ctx.PlayerController.PlayerFacingDir)
        {
            curDirction = Ctx.PlayerController.PlayerFacingDir;
            PlayAction(GetCurAnima(curDirction));
        }

        if (Ctx.InputContext.MoveInput.sqrMagnitude > 0.1f)
        {
            Machine.ChangeState(new State_BasicMove(Machine, Ctx));
            return;
        }
    }
    private string GetCurAnima(Direction dir)
    {
        string Action_IdelDown = "Player_Idel_Down";
        string Action_IdelLeft = "Player_Idel_Left";
        string Action_IdelRight = "Player_Idel_Right";
        string Action_IdelUp = "Player_Idel_Up";

        string curDirAnima;
        switch (dir)
        {
            case Direction.Up:
                curDirAnima = Action_IdelUp;
                break;
            case Direction.Left:
                curDirAnima = Action_IdelLeft;
                break;
            case Direction.Down:
                curDirAnima = Action_IdelDown;
                break;
            case Direction.Right:
                curDirAnima = Action_IdelRight;
                break;
            default:
                curDirAnima = Action_IdelDown;
                break;
        }
        return curDirAnima;
    }

    public void PlayAction(string anima)
    {
        Ctx.Animator.Play(anima);
        actionRuntime = new ActionRuntime(ActionRegistry.Get(ActionRegistry.PlayerAction, anima));

    }
}



public enum InteractPhase
{
    None,
    Harvest,
}
public class State_Interact : State<PlayerContext>
{
    public InteractPhase phase;
    public State<PlayerContext> previous;
    private ActionRuntime actionRuntime;
    public State_Interact(StateMachine<PlayerContext> machine, PlayerContext ctx) : base(machine, ctx)
    {
    }
    public override void Enter()
    {
        Debug.Log("Entering Interact State");
        base.Enter();
        Ctx.PlayerController.canMove = false;
        Ctx.PlayerController.canInteract = false;

        //储存原状态，交互结束后返回

        phase = WorldState.Instance.DetectInteract(Ctx.PlayerController.InteractTilePosition);
        string curDirAnima = string.Empty;
        switch (phase)
        {
            case InteractPhase.None:
                Machine.PopState();
                return;
                //交互无效直接返回原状态
            case InteractPhase.Harvest:
                //依据玩家朝向分配动画
                string Action_HarvestDown = "Player_Harvest_Down";
                string Action_HarvestLeft = "Player_Harvest_Left";
                string Action_HarvestRight = "Player_Harvest_Right";
                string Action_HarvestUp = "Player_Harvest_Up";
                switch (Ctx.PlayerController.PlayerFacingDir)
                {
                    case Direction.Up:
                        curDirAnima = Action_HarvestUp;
                        break;
                    case Direction.Left:
                        curDirAnima = Action_HarvestLeft;
                        break;
                    case Direction.Down:
                        curDirAnima = Action_HarvestDown;
                        break;
                    case Direction.Right:
                        curDirAnima = Action_HarvestRight;
                        break;
                    default:
                        curDirAnima = Action_HarvestUp;
                        break;
                }
                //收获动画
                break;
        }
        if(curDirAnima != string.Empty)
        {
            PlayAction(curDirAnima);
        }

    }
    public override void Update()
    {
        base.Update();

        //检测事件
        if (actionRuntime != null) { actionRuntime.Tick(Time.deltaTime); }

        if (actionRuntime.CanApplyEffect())
        {
            WorldState.Instance.InteractAt(Ctx.PlayerController.InteractTilePosition);
            actionRuntime.MarkEffectApplied();
        }

        if (actionRuntime.IsFinished())
        {
            Machine.PopState();
        }

    }
    public override void Exit()
    {
        base.Exit();
    }
    public void PlayAction(string anima)
    {
        Ctx.Animator.Play(anima);
        actionRuntime = new ActionRuntime(ActionRegistry.Get(ActionRegistry.PlayerAction, anima));

    }
}