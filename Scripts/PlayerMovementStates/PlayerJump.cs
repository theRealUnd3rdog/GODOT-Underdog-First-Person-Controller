using Godot;
using System;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class PlayerJump : PlayerMovementState
{
    public override void Enter()
    {
        base.Enter();
    }

    public override void PhysicsUpdate(double delta)
    {
        
    }
}