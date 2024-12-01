using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Air : MovementState
{
    [Export(PropertyHint.Range, "1, 4,")] private float _gravityMultiplier = 1f;

    public override void Enter()
    {
        base.Enter();

        Camera.StartStanding();
    }

    public override void Update(double delta)
    {
        Camera.FollowMeshToNeck();
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

        // Handle landing
		if (Movement.IsOnFloor())
		{
            EmitSignal(SignalName.StateFinished, "Decceleration", new());
		}
    }
}
