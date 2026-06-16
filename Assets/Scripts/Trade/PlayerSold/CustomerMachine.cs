using UnityEngine;

/// <summary>
/// Base class for all customer states.
/// Keeps common accessors and helper methods.
/// </summary>
public abstract class State_CustomerBase : State<CustomerContext>
{
    protected CustomerController Customer => Ctx.CustomerController;
    protected PlayerStore_Entity TargetStore => Ctx.TargetEntity;

    protected State_CustomerBase(StateMachine<CustomerContext> machine, CustomerContext ctx)
        : base(machine, ctx)
    {
    }

    protected void MoveToQueueTarget()
    {
        if (!Ctx.HasQueueTarget)
            return;

        Customer.MoveTo(Ctx.QueueTargetPos);
    }
}

/// <summary>
/// Customer idle state.
/// Customer wanders or waits until a target store is assigned.
/// </summary>
public class State_CustomerIdle : State_CustomerBase
{
    public State_CustomerIdle(StateMachine<CustomerContext> machine, CustomerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Customer.BeginWander();
    }

    public override void Update()
    {
        base.Update();

        if (TargetStore != null)
        {
            Customer.StopWander();
            Machine.ChangeState(new State_CustomerAttracting(Machine, Ctx));
            return;
        }

        // 闲逛逻辑
        Customer.UpdateWander();
    }

    public override void Exit()
    {
        base.Exit();

        Customer.StopWander();
    }
}

/// <summary>
/// Customer queueing state.
/// Customer joins target store queue, moves to assigned queue position,
/// waits, and either buys or leaves when patience is gone.
/// </summary>
public class State_CustomerAttracting : State_CustomerBase
{
    public State_CustomerAttracting(StateMachine<CustomerContext> machine, CustomerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log($"Customer Attracted", Customer);

        Customer.StopWander();

        if (TargetStore == null)
        {
            Debug.Log("Customer don't have target store", Customer);
            Machine.ChangeState(new State_CustomerIdle(Machine, Ctx));
            return;
        }

        TargetStore.JoinQueue(Customer);

        Customer.ResetAttractAttitude();
    }

    public override void Update()
    {
        base.Update();

        if (TargetStore == null)
        {
            Machine.ChangeState(new State_CustomerIdle(Machine, Ctx));
            return;
        }

        MoveToQueueTarget();

        // The first customer in one queue can start buying.
        if (TargetStore.IsQueueFront(Customer) && Customer.HasArrivedQueueTarget())
        {
            Machine.ChangeState(new State_CustomerBuying(Machine, Ctx));
            return;
        }

        // Non-front customers may leave when patience is gone.
        if (Customer.AttractAttitude <= 0f)
        {
            Machine.ChangeState(new State_CustomerQuit(Machine, Ctx));
            return;
        }
    }

    public override void HandleEvent(FsmEvent evt)
    {
        base.HandleEvent(evt);

        if (evt.Type == FsmEventType.MinutePassed)
        {
            Customer.AttractWill();
        }
    }

    public override void Exit()
    {
        base.Exit();

        // 注意：这里不要 LeaveQueue。
        // 因为 Attracting -> Buying 时，顾客仍然应该占据队首。
        // 真正离队放到 Quit / 买完逻辑里。
    }
}

/// <summary>
/// Customer buying state.
/// Customer is at queue front and tries to buy from target store containers.
/// Waiting here affects final tip / price.
/// </summary>
public class State_CustomerBuying : State_CustomerBase
{
    public State_CustomerBuying(StateMachine<CustomerContext> machine, CustomerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log("EnterBuying", Customer);
        Customer.StopMove();
        Customer.FaceUp();

        if (TargetStore == null || !TargetStore.IsQueueFront(Customer))
        {
            Machine.ChangeState(new State_CustomerQuit(Machine, Ctx));
            return;
        }

        Customer.ResetBuyAttitude();

        if (!Customer.TryPrepareBuyItem(out ItemStack item, out int price, out int count))
        {
            Debug.Log("Try Prepare Buy Item Failed", Customer);
            Machine.ChangeState(new State_CustomerQuit(Machine, Ctx));
            return;
        }

        Ctx.BuyItem = item;
        Ctx.Price = price;
        Ctx.Count = count;
        Debug.Log(item.itemId, Customer);
    }

    public override void Update()
    {
        base.Update();

        if (TargetStore == null)
        {
            Machine.ChangeState(new State_CustomerQuit(Machine, Ctx));
            return;
        }

        // 商品失效时重新选商品。
        if (!Customer.IsBuyItemValid(Ctx.BuyItem))
        {
            if (!Customer.TryPrepareBuyItem(out ItemStack item, out int price, out int count))
            {
                Debug.Log("Buying Failed", Customer);
                Machine.ChangeState(new State_CustomerQuit(Machine, Ctx));
                return;
            }

            Ctx.BuyItem = item;
            Ctx.Price = price;
            Ctx.Count = count;
            Debug.Log(item.itemId, Customer);
        }

        // todo: 这里之后接真正的交易完成条件。
        // 例如播放购买动画 / 等待几秒 / 扣库存 / 加钱。
        if (Customer.BuyFinished)
        {
            Machine.ChangeState(new State_CustomerQuit(Machine, Ctx));
        }
    }

    public override void HandleEvent(FsmEvent evt)
    {
        base.HandleEvent(evt);

        if (evt.Type == FsmEventType.MinutePassed)
        {
            Customer.BuyWill(Ctx.Price, Ctx.Count);
        }
    }
}

/// <summary>
/// Customer quit state.
/// Customer leaves queue and gets removed / recycled.
/// </summary>
public class State_CustomerQuit : State_CustomerBase
{
    public State_CustomerQuit(StateMachine<CustomerContext> machine, CustomerContext ctx)
        : base(machine, ctx)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (TargetStore != null)
        {
            TargetStore.LeaveQueue(Customer);
        }

        Ctx.HasQueueTarget = false;
        Ctx.TargetEntity = null;

        Customer.OnQuit();
    }
}