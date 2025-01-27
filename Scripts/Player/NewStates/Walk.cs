using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Walk : MovementState
{
    [Export] private float _walkingSpeed;
    [Export] private float _walkAccelerationTime = 0.5f;
    [Export] private float _headBobSpeed = 14.0f;
    [Export] private float _headBobIntensity = 0.1f; //in centimetres

    public override void Enter()
    {
        base.Enter();

        Camera.StartStanding();
    }

    public override void Update(double delta)
    {
        Camera.SetHeadBob(_headBobIntensity, _headBobSpeed);
        Camera.HeadBob();
    }

    public override void PhysicsUpdate(double delta)
    {
        Movement.Accelerate((float)delta, _walkingSpeed, _walkAccelerationTime);

        if (Movement.IsOnFloor() && Input.IsActionPressed("crouch"))
        {
            EmitSignal(SignalName.StateFinished, "Crouch", new());
        }

        if (Input.IsActionJustPressed("jump"))
        {
            EmitSignal(SignalName.StateFinished, "Jump", new());
        }

        if (Movement.IsOnFloor() && Input.IsActionPressed("sprint"))
        {
            EmitSignal(SignalName.StateFinished, "Sprint", new());
        }

        // Threshold velocity before it reaches idle
        if (Movement.GetRawInputDirection() == Vector2.Zero)
            EmitSignal(SignalName.StateFinished, "Idle", new());
        
        if (!Movement.IsOnFloor())
		{
			EmitSignal(SignalName.StateFinished, "Air", new());
		}

        /* if (Movement.CheckLadder())
        {
            EmitSignal(SignalName.StateFinished, "PlayerLadder", new());
        } */
    }
}
