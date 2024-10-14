using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class PlayerWalk : PlayerMovementState
{
    public override void Enter()
    {
        base.Enter();
    }

    public override void PhysicsUpdate(double delta)
    {
        Movement.Stand(delta);
        Movement.currentSpeed = Mathf.Lerp(Movement.currentSpeed, Movement.walkingSpeed, 
                            1.0f - Mathf.Pow(0.5f, (float)delta *  Movement.lerpSpeed));

        if (Input.IsActionPressed("crouch"))
        {
            EmitSignal(SignalName.StateFinished, "PlayerCrouch", new());
        }

        if (Movement.sprintAction)
        {
            EmitSignal(SignalName.StateFinished, "PlayerSprint", new());
        }

        if (Movement.inputDirection == Vector2.Zero)
            EmitSignal(SignalName.StateFinished, "PlayerIdle", new());
        
        if (!Movement.IsOnFloor() && Mathf.Abs(Movement.Velocity.Y) > 0.1f)
		{
			EmitSignal(SignalName.StateFinished, "PlayerAir", new());
		}

        if (Movement.CheckLadder())
        {
            EmitSignal(SignalName.StateFinished, "PlayerLadder", new());
        }
    }
}
