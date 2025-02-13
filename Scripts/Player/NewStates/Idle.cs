using Godot;
using System;
using System.Collections.Generic;
using MEC;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class Idle : MovementState
{
    public override void Enter()
    {
        base.Enter();

        Camera.StartStanding();
        Timing.RunCoroutine(AlignMeshBeforeAutoAlignment(), "AutoMeshAlignment");

        Movement.AnimationPlayer.Set("parameters/Master/conditions/idle", true);
    }

    public override void Exit()
    {
        StopAutoMeshAlignment();

        Movement.AnimationPlayer.Set("parameters/Master/conditions/idle", false);
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Input.IsActionPressed("crouch"))
        {
            EmitSignal(SignalName.StateFinished, "Crouch", new());
        }

        if (Input.IsActionJustPressed("jump"))
        {
            EmitSignal(SignalName.StateFinished, "Jump", new());
        }

        if (Movement.GetRawInputDirection() != Vector2.Zero)
        {
            EmitSignal(SignalName.StateFinished, "Sprint", new());
        }

		if (!Movement.IsOnFloor())
		{
			EmitSignal(SignalName.StateFinished, "Air", new());
		}
    }

    /// <summary>
	/// Coroutine that aligns the mesh with the neck
	/// </summary>
    private IEnumerator<double> AlignMeshBeforeAutoAlignment()
	{
        CoroutineHandle handle = Timing.RunCoroutine(Camera.AlignMeshWithDirectionConstant(), "AutoMeshAlignment");

        yield return Timing.WaitUntilDone(handle);
        RunAutoMeshAlignment();
	}

    private void RunAutoMeshAlignment() => Timing.RunCoroutine(Camera.RunBodyMeshRotation(), Segment.Process, "AutoMeshAlignment");
	private void StopAutoMeshAlignment() => Timing.KillCoroutines("AutoMeshAlignment");
}
