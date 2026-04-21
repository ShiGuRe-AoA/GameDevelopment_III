using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public enum FsmEventType
{
    None = 0,

    // 时间
    MinutePassed,
    HourPassed,
    DayChanged,

    // 移动
    MoveStarted,
    MoveArrived,
    MoveFailed,

    // 动画
    AnimationFinished,
    AnimationEventTriggered,

    // 交互
    InteractionStarted,
    InteractionFinished,
    InteractionCancelled,

    // 任务
    TaskAssigned,
    TaskCompleted,
    TaskFailed,
    TaskCancelled,

    // 目标
    TargetAcquired,
    TargetLost,
    TargetInvalid,

    // 属性
    StaminaLow,
    HungerHigh,
    HealthZero,

    // 外部控制
    DialogueStarted,
    DialogueEnded,
    CutsceneStarted,
    CutsceneEnded,

    // 自定义
    Custom,
}
public readonly struct FsmEvent
{
    public readonly FsmEventType Type;
    public readonly object Sender;
    public readonly object Payload;

    public FsmEvent(FsmEventType type, object sender = null, object payload = null)
    {
        Type = type;
        Sender = sender;
        Payload = payload;
    }

    public T GetPayload<T>()
    {
        return Payload is T value ? value : default;
    }
}
public interface IState
{
    bool CanEnter();
    bool CanExit();
    void Enter();
    void Exit();
    void Pause();
    void Resume();
    void Update();
    void FixedUpdate();
    void HandleEvent(FsmEvent evt);
}

public abstract class State<TContext> : IState
{
    protected readonly TContext Ctx;
    protected readonly StateMachine<TContext> Machine;

    protected State(StateMachine<TContext> machine, TContext ctx)
    {
        Machine = machine;
        Ctx = ctx;
    }
    public virtual bool CanEnter() => true;
    public virtual bool CanExit() => true;
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Pause() { }
    public virtual void Resume() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void HandleEvent(FsmEvent evt) { }
}



public class StateMachine<TContext>
{
    public IState CurrentState { get; private set; }
    public IState PreviousState { get; private set; }

    private readonly Stack<IState> _stack = new();
    private readonly TContext _context;

    public StateMachine(TContext context)
    {
        _context = context;
    }

    public bool ChangeState(IState next)
    {
        if (next == null) return false;
        if (CurrentState != null && !CurrentState.CanExit()) return false;
        if (!next.CanEnter()) return false;

        CurrentState?.Exit();
        PreviousState = CurrentState;
        CurrentState = next;
        CurrentState.Enter();
        return true;
    }

    public bool PushState(IState next)
    {
        if (next == null) return false;
        if (CurrentState != null && !CurrentState.CanExit()) return false;
        if (!next.CanEnter()) return false;

        if (CurrentState != null)
        {
            CurrentState.Pause();
            _stack.Push(CurrentState);
        }

        PreviousState = CurrentState;
        CurrentState = next;
        CurrentState.Enter();
        return true;
    }

    public bool PopState()
    {
        if (CurrentState != null)
            CurrentState.Exit();

        if (_stack.Count == 0)
        {
            CurrentState = null;
            return false;
        }

        CurrentState = _stack.Pop();
        CurrentState.Resume();
        return true;
    }

    public void Update() => CurrentState?.Update();
    public void FixedUpdate() => CurrentState?.FixedUpdate();
    public void SendEvent(FsmEvent evt) => CurrentState?.HandleEvent(evt);
}