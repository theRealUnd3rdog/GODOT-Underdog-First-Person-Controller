using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Sprint : MovementState
{
    [Export] private float _sprintingSpeed;
    private float _sprintSpeedChange; // Value that changes based on the input direction

    // Value that determines how much speed to reduce when moving to other directions except forward
    [Export(PropertyHint.Range, "0.5, 1,")] private float _sprintChangeFactor = 0.65f; 

    [Export] private float _headBobSpeed = 22.0f;
    [Export] private float _headBobIntensity = 0.2f; //in centimetres

    public override void Enter()
    {
        base.Enter();

        _sprintSpeedChange = _sprintingSpeed;

        Camera.StartStanding();
        Movement.SetDesiredSpeed(_sprintSpeedChange);
    }

    public override void Update(double delta)
    {
        Camera.SetHeadBob(_headBobIntensity, _headBobSpeed);
        Camera.HeadBob();

        Camera.RotateBodyMesh();
    }

    public override void PhysicsUpdate(double delta)
    {
        // Multiplication being the factor of how much you want to reduce the speed. 1 being full, 0 being nothing
        float _sprintSpeedChange = Movement.IsPlayerMainlyForward(45) ? _sprintingSpeed : _sprintingSpeed * _sprintChangeFactor;

        Movement.Accelerate((float)delta, _sprintSpeedChange);

        /* if (Input.IsActionPressed("crouch") && !Movement.IsOnWall() && !Movement.IsRunningUpSlope() && !Movement.stepCast.IsColliding()
                && Movement.Velocity.Length() >= (Movement.sprintingSpeed - 1) && Movement.inputDirection.Y < 0f)
        {
            EmitSignal(SignalName.StateFinished, "PlayerSlide", new());
        } */

        /* if (Movement.IsOnFloor() && !Input.IsActionPressed("sprint"))
        {
            EmitSignal(SignalName.StateFinished, "Walk", new());
        } */

        // Threshold velocity before it reaches idle
        if (Movement.GetInputDirection() == Vector2.Zero)
            EmitSignal(SignalName.StateFinished, "Decceleration", new());

        if (!Movement.IsOnFloor())
		{
			EmitSignal(SignalName.StateFinished, "Air", new());
		}

        if (Input.IsActionJustPressed("jump"))
        {
            EmitSignal(SignalName.StateFinished, "Jump", new());
        }

        /* if (Movement.CheckVault(delta, out Vector3 vaultPoint) && Input.IsActionJustPressed("jump"))
        {
            EmitSignal(SignalName.StateFinished, "PlayerVault", new());
        } */

        /* if (Movement.CheckLadder())
        {
            EmitSignal(SignalName.StateFinished, "PlayerLadder", new());
        } */
    }
}
