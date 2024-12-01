using Godot;
using Godot.Collections;

public partial class GodotParadiseState : Node
{
	[Signal]
	public delegate void StateEnteredEventHandler();


	[Signal]
	public delegate void StateFinishedEventHandler(string nextState, Dictionary parameters);

	public Array<GodotParadiseState> PreviousStates = new();
	public Dictionary parameters = new();

	public virtual void Awake()
	{

	}

	public virtual void Enter()
	{

	}

	public virtual void Exit()
	{

	}

	public virtual void HandleInput(InputEvent @event)
	{

	}

	public virtual void PhysicsUpdate(double delta)
	{

	}

	public virtual void Update(double delta)
	{

	}

	public virtual void OnAnimationPlayerFinished(string Name)
	{

	}

	public virtual void OnAnimationFinished()
	{

	}
}
