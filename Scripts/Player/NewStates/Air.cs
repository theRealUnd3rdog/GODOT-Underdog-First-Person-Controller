using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Air : MovementState
{
    [Export] private float _airSpeed;
    [Export] private float _airAccelerationTime = 2.0f;
    [Export(PropertyHint.Range, "0, 1")] private float _airControl = 0.05f;
    [Export(PropertyHint.Range, "0, 1,")] private float _airChangeFactor = 0.65f; 

    [Export(PropertyHint.Range, "0.01, 10,")] private float _airDirChangeTime;
    [Export(PropertyHint.Range, "1, 4,")] private float _gravityMultiplier = 1f;

    private float _airTime = 0.0f;
    private float _coyoteTimer = 0.0f;
    [Export] private float _coyoteTime = 0.2f;

    public override void Enter()
    {
        base.Enter();

        Camera.StartStanding();
        Movement.SetDirectionChangeTime(_airDirChangeTime);
        Movement.SetDirectionControl(_airControl);
    }

    public override void Exit()
    {
        
    }

    public override void Update(double delta)
    {
        _airTime += (float)GetProcessDeltaTime();
        _coyoteTimer += (float)GetProcessDeltaTime();

        if (Movement.GetRawInputDirection() == Vector2.Zero)
		{
			Camera.RotateBodyMeshDirection();
		}
		else
		{
			Camera.RotateBodyMeshInput();
		}
    }

    public override void PhysicsUpdate(double delta)
    {
        float yVelocity = Movement.Velocity.Y;

        if (yVelocity > 0)
        {
            yVelocity -= Movement.gravity * (float)delta;
        }
        else
        {
            yVelocity -= Movement.gravity * _gravityMultiplier * (float)delta;
        }

        Movement.SetYVelocity(yVelocity);

        float airSpeedChange = Movement.IsPlayerMainlyForward(45) ? _airSpeed : _airSpeed * _airChangeFactor;

        if (Movement.GetRawInputDirection() != Vector2.Zero && Movement.GetCurrentSpeed() < airSpeedChange)
        {
            Movement.Accelerate((float)delta, airSpeedChange, _airAccelerationTime);
        }
        else
        {
            Movement.Deccelerate((float)delta, airSpeedChange, _airAccelerationTime * 2f);
        }

        float airControlChange = Movement.IsPlayerMainlyForward(45) ? _airControl : _airControl * _airChangeFactor;
        Movement.SetDirectionControl(airControlChange);

        // Handle landing
		if (Movement.IsOnFloor())
		{
            _airTime = 0.0f;
            _coyoteTimer = 0.0f;

            if (Movement.GetRawInputDirection() != Vector2.Zero)
                EmitSignal(SignalName.StateFinished, "Sprint", new());
            else
                EmitSignal(SignalName.StateFinished, "Decceleration", new());
		}
        else
        {
            if (_coyoteTimer <= _coyoteTime)
            {
                if (Input.IsActionJustPressed("jump") && Movement.FSM.PreviousState is not Jump)
                {
                    EmitSignal(SignalName.StateFinished, "Jump", new());
                    _coyoteTimer = _coyoteTime + 1; // Prevents multiple jumps
                }
            }
        }
    }

    public float GetAirTime() => _airTime;
}
