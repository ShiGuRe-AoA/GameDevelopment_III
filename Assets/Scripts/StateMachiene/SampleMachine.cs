using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State_BasicMove : State<PlayerContext>
{

    public State_BasicMove(StateMachine<PlayerContext> machine, PlayerContext ctx) : base(machine, ctx)
    {
    }


    public override void Enter()
    {
        base.Enter();
        Ctx.PlayerController.canMove = true;
        Ctx.PlayerController.canInteract = true;

        //TODO: 收到美术的移动动画后进行替换
        string State_IdelUp = "Player_Idel_Up";
        string State_IdelLeft = "Player_Idel_Left";
        string State_IdelDown = "Player_Idel_Down";
        string State_IdelRight = "Player_Idel_Right";

        string curDirAnima;
        switch (Ctx.PlayerController.PlayerFacingDir)
        {
            case Direction.Up:
                curDirAnima = State_IdelUp;
                break;
            case Direction.Left:
                curDirAnima = State_IdelLeft;
                break;
            case Direction.Down:
                curDirAnima = State_IdelDown;
                break;
            case Direction.Right:
                curDirAnima = State_IdelRight;
                break;
            default:
                curDirAnima = State_IdelDown;
                break;
        }

        Ctx.Animator.Play(curDirAnima);
    }
    public override void Update()
    {
        base.Update();
        if (Ctx.InputContext.MoveInput.sqrMagnitude < 0.0001f)
        {
            Machine.ChangeState(new State_Idle(Machine, Ctx));
        }
        else
        {
            Ctx.PlayerController.SetMoveInput(Ctx.InputContext.MoveInput);
        }


    }
}

public class State_Idle : State<PlayerContext>
{
    public State_Idle(StateMachine<PlayerContext> machine, PlayerContext ctx) : base(machine, ctx)
    {
    }
    public override void Enter()
    {
        base.Enter();
        Ctx.PlayerController.canMove = true;
        Ctx.PlayerController.canInteract = true;

        string State_IdelUp = "Player_Idel_Up";
        string State_IdelLeft = "Player_Idel_Left";
        string State_IdelDown = "Player_Idel_Down";
        string State_IdelRight = "Player_Idel_Right";

        string curDirAnima;
        switch (Ctx.PlayerController.PlayerFacingDir)
        {
            case Direction.Up:
                curDirAnima = State_IdelUp;
                break;
            case Direction.Left:
                curDirAnima = State_IdelLeft;
                break;
            case Direction.Down:
                curDirAnima = State_IdelDown;
                break;
            case Direction.Right:
                curDirAnima = State_IdelRight;
                break;
            default:
                curDirAnima = State_IdelDown;
                break;
        }

        Ctx.Animator.Play(curDirAnima);
    }
    public override void Update()
    {
        base.Update();
        if (Ctx.InputContext.MoveInput.sqrMagnitude > 0.1f)
        {
            Machine.ChangeState(new State_BasicMove(Machine, Ctx));
        }
    }
}



public enum InteractPhase
{

}
public class State_Interact : State<PlayerContext>
{
    public State_Interact(StateMachine<PlayerContext> machine, PlayerContext ctx) : base(machine, ctx)
    {
    }
    //储存原状态，交互结束后返回
    public override void Enter()
    {
        base.Enter();
        Ctx.PlayerController.canMove = false;
        Ctx.PlayerController.canInteract = false;

        
    }
    public override void Exit()
    {
        base.Exit();
    }
}