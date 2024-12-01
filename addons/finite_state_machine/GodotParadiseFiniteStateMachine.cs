/*
## Created by https://github.com/GodotParadise organization with LICENSE MIT
# There are no restrictions on modifying, sharing, or using this component commercially
# We greatly appreciate your support in the form of stars, as they motivate us to continue our journey of enhancing the Godot community
# ***************************************************************************************
# A finite state machine designed to cover 95% of use cases, providing essential functionality and a basic state node that can be extended now in C#.

# There is nothing wrong using the same process on the CharacterBody2D to handle all the movement but when the things start to grow and the player can perform a wide number of moves it is better to start thinking about a modular way to build this movement. 
# In this case, a state machine is a design pattern widely employed in the video game industry to manage this complexity
##

*/

using System.Runtime.Serialization.Formatters;
using Godot;
using Godot.Collections;

public partial class GodotParadiseFiniteStateMachine : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(GodotParadiseState fromState, GodotParadiseState state);
    [Signal]
    public delegate void StackPushedEventHandler(GodotParadiseState newState, Array<GodotParadiseState> stack);
    [Signal]
    public delegate void StackFlushedEventHandler(Array<GodotParadiseState> stack);

    [Export]
    public GodotParadiseState CurrentState;

    public GodotParadiseState PreviousState;

    [Export]
    public int StackCapacity = 3;
    [Export]
    public bool FlushStackWhenReachCapacity = false;
    [Export]
    public bool EnableStack { get; set; } = true;

    public Dictionary States = new();
    public Array<GodotParadiseState> StatesStack = new();
    public bool Locked = false;

    public override void _Ready()
    {
        InitializeStateNodes();

        foreach (GodotParadiseState state in States.Values)
        {
            state.StateFinished += OnFinishedState;
        }

        if (CurrentState is not null)
        {
            ChangeState(CurrentState, new(), true);
        }

        UnlockStateMachine();

        StackPushed += OnStackPushed;
    }


    public override void _Input(InputEvent @event)
    {
        CurrentState.HandleInput(@event);
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentState.PhysicsUpdate(delta);
    }

    public override void _Process(double delta)
    {
        CurrentState.Update(delta);
    }

    public void ChangeState(GodotParadiseState newState, Dictionary parameters, bool force = false)
    {
        if (!force && CurrentStateIs(newState))
        {
            return;
        }

        if (CurrentState is not null)
        {
            ExitState(CurrentState);
        }

        PushStateToStack(CurrentState);
        EmitSignal(SignalName.StateChanged, CurrentState, newState);

        PreviousState = CurrentState;

        CurrentState = newState;
        CurrentState.parameters = parameters;

        EnterState(newState);
    }

    public void ChangeStateByName(string name, Dictionary parameters, bool force = false)
    {
        GodotParadiseState state = GetStateByName(name);

        if (state is not null)
        {
            ChangeState(state, parameters, force);
        }

        GD.PushError($"GodotParadiseFSMPlugin: The state {name} does not exists on this FiniteStateMachine");
    }

    public void EnterState(GodotParadiseState state)
    {
        state.Enter();
        state.EmitSignal(GodotParadiseState.SignalName.StateEntered);
    }


    public void ExitState(GodotParadiseState state)
    {
        state.Exit();
    }


    public bool CurrentStateIs(GodotParadiseState state)
    {
        return state.Name.ToString().ToLower().Equals(CurrentState.Name.ToString().ToLower());
    }

    public bool CurrentStateNameIs(string name)
    {
        GodotParadiseState state = GetStateByName(name);

        if (state is not null)
        {
            return CurrentStateIs(state);
        }

        return false;
    }

    public GodotParadiseState GetStateByName(string name)
    {
        if (States.ContainsKey(name))
        {
            return (GodotParadiseState)States[name];
        }

        return null;
    }

    public void PushStateToStack(GodotParadiseState state)
    {
        if (EnableStack && StackCapacity > 0)
        {
            if (StatesStack.Count >= StackCapacity)
            {
                if (FlushStackWhenReachCapacity)
                {
                    EmitSignal(SignalName.StackFlushed, StatesStack);
                    StatesStack.Clear();
                }
                else
                {
                    StatesStack.RemoveAt(0);
                }
            }

            StatesStack.Add(state);
            EmitSignal(SignalName.StackPushed, state, StatesStack);
        }
    }

    public void LockStateMachine()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
        SetProcessInput(false);
        SetProcessUnhandledInput(false);
    }

    public void UnlockStateMachine()
    {
        SetProcess(true);
        SetPhysicsProcess(true);
        SetProcessInput(true);
        SetProcessUnhandledInput(true);
    }

    private void AddStateToDictionary(GodotParadiseState state)
    {
        if (state.IsInsideTree())
        {
            States.Add(state.Name, GetNode(state.GetPath()));
            state.Awake();
        }
    }


    private void InitializeStateNodes(Node node = null)
    {
        Array<Node> childrens = node != null ? node.GetChildren(true) : GetChildren(true);

        foreach (Node child in childrens)
        {
            if (child is GodotParadiseState state)
            {
                AddStateToDictionary(state);
            }
            else
            {
                InitializeStateNodes(child);
            }
        }

    }

    private void OnFinishedState(string nextState, Dictionary parameters)
    {
        GodotParadiseState state = GetStateByName(nextState);

        if (state is not null)
        {
            ChangeState(state, parameters);
        }
    }


    private void OnStackPushed(GodotParadiseState newState, Array<GodotParadiseState> stack)
    {
        foreach (GodotParadiseState state in States.Values)
        {
            state.PreviousStates = stack;
        }
    }


}