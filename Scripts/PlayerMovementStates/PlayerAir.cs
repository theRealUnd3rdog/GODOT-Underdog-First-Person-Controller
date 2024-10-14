using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class PlayerAir : PlayerMovementState
{
    public static event Action<Vector3> LastVelocityChangeLanded; // Event that keeps track of the last velocity when landed
    public static event Action PlayerLanded;

    private Vector3 _initialDirection;

    public override void Enter()
    {
        base.Enter();

        if (Movement.IsOnFloor() && Movement.FSM.PreviousState is PlayerWallrun)
        {
            Movement.airTime = 0f;
            Movement.wallRunTimer = 0f; // Reset the timer
            
            EmitSignal(SignalName.StateFinished, "PlayerIdle", new());
        }

    }

    public override void Exit()
    {
        LastVelocityChangeLanded?.Invoke(Movement.lastVelocity);
        PlayerLanded?.Invoke();
    }

    public override void PhysicsUpdate(double delta)
    {
        Movement.Stand(delta);
        Movement.airTime += (float)delta;

        // Handle current speed when in air based on given speed and momentum

        if (Movement.momentum > 0.1f)
        {
            Movement.currentSpeed = Mathf.Lerp(Movement.currentSpeed, Movement.sprintingSpeed + (Movement.momentum), 
                            1.0f - Mathf.Pow(0.5f, (float)delta * Movement.lerpSpeed));
        }

        // Handle landing
		if (Movement.IsOnFloor())
		{
			Movement.airTime = 0f;
            Movement.wallRunTimer = 0f; // Reset the timer

            EmitSignal(SignalName.StateFinished, "PlayerIdle", new());
		}

        // Wallrun
        if (Movement.CheckWall(out KinematicCollision3D collision, out String direction) 
        && Movement.wallRunTimer <= Movement.wallRunTime
        && Movement.airTime < 2f
        && !Movement.SendRayInDirection(-Movement.GlobalBasis.Z, 0.5f, out Vector3 normal, out Vector3 point)
        && Movement.FSM.PreviousState is not PlayerVerticalWallrun)
        {
            Movement.airTime = 0f;
            EmitSignal(SignalName.StateFinished, "PlayerWallrun", new());
        }

        // Vertical wallrun
        if (Movement.CheckVerticalWall(out Vector3 verticalWallDir, out Vector3 verticalWallPoint)
            && !Movement.IsOnFloor()
            && Movement.FSM.PreviousState is not PlayerWallrun
            && Movement.FSM.PreviousState is not PlayerVerticalWallrun && !Movement.isLadder)
        {
            Movement.airTime = 0f;
            EmitSignal(SignalName.StateFinished, "PlayerVerticalWallrun", new());
        }

        if (Movement.CheckVault(delta, out Vector3 vaultPoint))
        {
            EmitSignal(SignalName.StateFinished, "PlayerVault", new());
        }

        if (Movement.CheckLadder())
        {
            EmitSignal(SignalName.StateFinished, "PlayerLadder", new());
        }
    }
}
