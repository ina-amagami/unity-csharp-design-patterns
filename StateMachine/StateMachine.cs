using System.Collections.Generic;

/// <summary>
/// ステートマシン
/// </summary>
public class StateMachine<TOwner>
{
    /// <summary>
    /// ステートを表すクラス
    /// </summary>
    public abstract class State
    {
        /// <summary>
        /// このステートを管理しているステートマシン
        /// </summary>
        protected StateMachine<TOwner> StateMachine => stateMachine;
        internal StateMachine<TOwner> stateMachine;
        /// <summary>
        /// 遷移の一覧
        /// </summary>
        internal Dictionary<int, State> transitions = new Dictionary<int, State>();
        /// <summary>
        /// このステートのオーナー
        /// </summary>
        protected TOwner Owner => stateMachine.Owner;

        /// <summary>
        /// ステート開始
        /// </summary>
        internal void Enter(State prevState)
        {
            OnEnter(prevState);
        }
        /// <summary>
        /// ステートを開始した時に呼ばれる
        /// </summary>
        protected virtual void OnEnter(State prevState) { }

        /// <summary>
        /// ステート更新
        /// </summary>
        internal void Update()
        {
            OnUpdate();
        }
        /// <summary>
        /// 毎フレーム呼ばれる
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// ステート終了
        /// </summary>
        internal void Exit(State nextState)
        {
            OnExit(nextState);
        }
        /// <summary>
        /// ステートを終了した時に呼ばれる
        /// </summary>
        protected virtual void OnExit(State nextState) { }
    }

    /// <summary>
    /// どのステートからでも特定のステートへ遷移できるようにするための仮想ステート
    /// </summary>
    public sealed class AnyState : State { }

    /// <summary>
    /// このステートマシンのオーナー
    /// </summary>
    public TOwner Owner { get; }
    /// <summary>
    /// 現在のステート
    /// </summary>
    public State CurrentState { get; private set; }

    // ステートリスト
    private LinkedList<State> states = new LinkedList<State>();

    /// <summary>
    /// ステートマシンを初期化する
    /// </summary>
    /// <param name="owner">ステートマシンのオーナー</param>
    public StateMachine(TOwner owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// ステートを追加する（ジェネリック版）
    /// </summary>
    public T Add<T>() where T : State, new()
    {
        var state = new T();
        state.stateMachine = this;
        states.AddLast(state);
        return state;
    }

    /// <summary>
    /// 特定のステートを取得、なければ生成する
    /// </summary>
    public T GetOrAddState<T>() where T : State, new()
    {
        foreach (var state in states)
        {
            if (state is T result)
            {
                return result;
            }
        }
        return Add<T>();
    }

    /// <summary>
    /// 遷移を定義する
    /// </summary>
    /// <param name="eventId">イベントID</param>
    public void AddTransition<TFrom, TTo>(int eventId)
        where TFrom : State, new()
        where TTo : State, new()
    {
        var from = GetOrAddState<TFrom>();
        if (from.transitions.ContainsKey(eventId))
        {
            // 同じイベントIDの遷移を定義済
            throw new System.ArgumentException(
                $"ステート'{nameof(TFrom)}'に対してイベントID'{eventId.ToString()}'の遷移は定義済です");
        }

        var to = GetOrAddState<TTo>();
        from.transitions.Add(eventId, to);
    }

    /// <summary>
    /// どのステートからでも特定のステートへ遷移できるイベントを追加する
    /// </summary>
    /// <param name="eventId">イベントID</param>
    public void AddAnyTransition<TTo>(int eventId) where TTo : State, new()
    {
        AddTransition<AnyState, TTo>(eventId);
    }

    /// <summary>
    /// ステートマシンの実行を開始する（ジェネリック版）
    /// </summary>
    public void Start<TFirst>() where TFirst : State, new()
    {
        Start(GetOrAddState<TFirst>());
    }

    /// <summary>
    /// ステートマシンの実行を開始する
    /// </summary>
    /// <param name="firstState">起動時のステート</param>
    /// <param name="param">パラメータ</param>
    public void Start(State firstState)
    {
        CurrentState = firstState;
        CurrentState.Enter(null);
    }

    /// <summary>
    /// ステートを更新する
    /// </summary>
    public void Update()
    {
        CurrentState.Update();
    }

    /// <summary>
    /// イベントを発行する
    /// </summary>
    /// <param name="eventId">イベントID</param>
    public void Dispatch(int eventId)
    {
        State to;
        if (!CurrentState.transitions.TryGetValue(eventId, out to))
        {
            if (!GetOrAddState<AnyState>().transitions.TryGetValue(eventId, out to))
            {
                // イベントに対応する遷移が見つからなかった
                return;
            }
        }
        Change(to);
    }

    /// <summary>
    /// ステートを変更する
    /// </summary>
    /// <param name="nextState">遷移先のステート</param>
    private void Change(State nextState)
    {
        CurrentState.Exit(nextState);
        nextState.Enter(CurrentState);
        CurrentState = nextState;
    }
}
