using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Decceleration : MovementState
{
    private float _previousSpeed;
    [Export] private float _deccelerationTime = 0.5f;

    public override void Enter()
    {
        base.Enter();
        _previousSpeed = Movement.GetCurrentSpeed();

        Movement.AnimationPlayer.Set("parameters/Master/conditions/moving", true);
    }

    public override void Update(double delta)
    {
        Camera.FollowMeshToNeck();
    }

    public override void Exit()
    {
        Movement.AnimationPlayer.Set("parameters/Master/conditions/moving", false);
    }

    public override void PhysicsUpdate(double delta)
    {
        Movement.Deccelerate((float)delta, _previousSpeed, _deccelerationTime);

        // Threshold velocity before it reaches idle
        if (Mathf.Round(Movement.GetCurrentSpeed()) <= 0f)
        {
            Movement.SetCurrentSpeed(0f);
            EmitSignal(SignalName.StateFinished, "Idle", new());
            
            Movement.AnimationPlayer.Set("parameters/Master/conditions/idle", true);
        }

        if (!Movement.IsOnFloor())
		{
			EmitSignal(SignalName.StateFinished, "Air", new());
		}

        if (Input.IsActionJustPressed("jump"))
        {
            EmitSignal(SignalName.StateFinished, "Jump", new());
        }

        if (Movement.GetRawInputDirection() != Vector2.Zero)
            EmitSignal(SignalName.StateFinished, "Sprint", new());
    }
}
