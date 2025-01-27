using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Jump : MovementState
{
    [Export] private float _jumpHeight = 3.0f;
    [Export] private float _timeToPeak = 1.0f;

    public override void Awake()
    {
        base.Enter();

        float gravity = (-2 * _jumpHeight) / Mathf.Pow(_timeToPeak, 2);
        Movement.gravity = -gravity;

        GD.Print(Movement.gravity);
    }

    public override void Enter()
    {
        base.Enter();

        Camera.StartStanding();
        JumpUp(_jumpHeight);

        EmitSignal(SignalName.StateFinished, "Air", new());
    }

    public override void PhysicsUpdate(double delta)
    {

    }

    private void JumpUp(float jumpHeight)
	{
        float jumpVelocity = Mathf.Sqrt(2 * Movement.gravity * jumpHeight);
        Movement.SetYVelocity(jumpVelocity);
	}
}
