using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Decceleration : MovementState
{
    public override void Enter()
    {
        base.Enter();
    }

    public override void Update(double delta)
    {
        Camera.FollowMeshToNeck();
    }

    public override void PhysicsUpdate(double delta)
    {
        Movement.Deccelerate((float)delta, 0f);

        // Threshold velocity before it reaches idle
        if (Mathf.Round(Movement.GetCurrentSpeed()) <= 0f)
        {
            Movement.SetCurrentSpeed(0f);
            EmitSignal(SignalName.StateFinished, "Idle", new());
        }

        if (!Movement.IsOnFloor())
		{
			EmitSignal(SignalName.StateFinished, "Air", new());
		}

        if (Input.IsActionJustPressed("jump"))
        {
            EmitSignal(SignalName.StateFinished, "Jump", new());
        }

        if (Movement.GetInputDirection() != Vector2.Zero)
            EmitSignal(SignalName.StateFinished, "Sprint", new());
    }
}
