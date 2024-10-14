using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using MEC;

[GlobalClass, Icon("res://addons/finite_state_machine/state_icon.png")]
public partial class PlayerLadder : PlayerMovementState
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    private float _inputThreshold = 0.1f;

    [Export] private AudioStreamPlayer3D _ladderStart;
	[Export] private AudioStreamPlayer3D _ladderMove;

    public override void Enter()
    {
        base.Enter();

        CamShake.ShakePreset(CamShakePresets.Vault);

        _ladderStart.PitchScale = _rng.RandfRange(0.9f, 1.1f);
        _ladderStart.Play();

        // Enter a new camera state and constrain the angles
        Movement.currentSpeed = 0f;
        Movement.camState = CameraState.Ladder;

        Timing.RunCoroutine(RunLadder(), Segment.PhysicsProcess, "ladderCor");
    }

    public override void Exit()
    {
        Movement.camState = CameraState.Normal;
        Timing.KillCoroutines("ladderCor");
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            Movement.ConstraintedRotation(eventMouseMotion.Relative.X, eventMouseMotion.Relative.Y, -60f, 60f, 89f, -89f);
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // Crouch to slide down the ladder
        if (Input.IsActionPressed("crouch")) 
        {
            // Implement sliding down logic
        }

        if (Movement.IsOnFloor())
        {
            EmitSignal(SignalName.StateFinished, "PlayerIdle", new());
        }

        // Jump cancels the ladder state only if it's facing away from the ladder
        if (Input.IsActionJustPressed("jump") || !Movement.isLadder)
        {
		    Movement.Jump(Movement.vaultJumpVelocity);
            EmitSignal(SignalName.StateFinished, "PlayerAir", new());
        }

        /* if (Movement.CheckVault(delta, out Vector3 vaultPoint))
        {
            Movement.wallRunTimer = 0f; // Reset the timer
            EmitSignal(SignalName.StateFinished, "PlayerVault", new());
        } */
    }

    private IEnumerator<double> RunLadder()
    {
        while(true)
        {
            Movement.playerVelocity = Vector3.Zero;
            Movement.Velocity = Movement.playerVelocity;

            if (Mathf.Abs(Movement.inputDirection.Y) > _inputThreshold)
            {
                Movement.direction = Movement.direction.Lerp((Movement.Transform.Basis 
                                * new Vector3(0f, -Movement.inputDirection.Y, 0f)).Normalized(), 
                                1.0f - Mathf.Pow(0.5f, (float)GetPhysicsProcessDeltaTime() * Movement.lerpSpeed * 4));

                if (Mathf.Abs(Movement.direction.Y) >= 0.95f)
                {
                    CoroutineHandle handle = Timing.RunCoroutine(PerformRungClimb(Movement.direction.Y), Segment.PhysicsProcess, "ladderCor");
                    Timing.WaitUntilDone(handle);
                }
            }
            
            yield return Timing.WaitForOneFrame;
        }
    }

    private IEnumerator<double> PerformRungClimb(float lastYDirection)
    {
        float timeElapsed = 0f;
        float distanceTravelled = 0f;

        _ladderMove.PitchScale = _rng.RandfRange(0.9f, 1.1f);
        _ladderMove.Play();

        do
        {
            timeElapsed += (float)GetPhysicsProcessDeltaTime();

            GD.Print("Time taken for 1 bar: " + timeElapsed);

            // Let player move upward and downward
            Movement.currentSpeed = Mathf.Lerp(Movement.currentSpeed, Movement.ladderSpeed, 
                                1.0f - Mathf.Pow(0.5f, (float)GetPhysicsProcessDeltaTime() *  Movement.lerpSpeed * 4));

            Movement.playerVelocity.Y = Movement.currentSpeed * lastYDirection;

            distanceTravelled = Movement.Velocity.Length() * timeElapsed;

            Movement.Velocity = Movement.playerVelocity;

            yield return Timing.WaitForOneFrame;
        }
        while (distanceTravelled < Movement.currentLadder.BarSpacing);
    }
}
