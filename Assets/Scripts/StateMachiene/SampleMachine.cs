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