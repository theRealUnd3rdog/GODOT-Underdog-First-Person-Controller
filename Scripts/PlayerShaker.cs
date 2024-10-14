using Godot;
using System;

public partial class PlayerShaker : Node
{
    [Export] private PlayerMovement _movement;
    private CamShakeInstance _idleShake;
    private CamShakeInstance _sprintShake;
    private CamShakeInstance _walkShake;
    private CamShakeInstance _inAirShake;
    private CamShakeInstance _slideShake;

    public override void _Ready()
    {
        if (_movement == null)
            return;

        _idleShake = CamShake.ShakePreset(CamShakePresets.Idle);
        _sprintShake = CamShake.ShakePreset(CamShakePresets.Sprinting);
        _walkShake = CamShake.ShakePreset(CamShakePresets.Walking);
        _inAirShake = CamShake.ShakePreset(CamShakePresets.InAir);
        _slideShake = CamShake.ShakePreset(CamShakePresets.Sliding);

        ResetAllInfluences();

        PlayerAir.LastVelocityChangeLanded += ShakeWhenLanded;
    }

    public override void _ExitTree()
    {
        PlayerAir.LastVelocityChangeLanded -= ShakeWhenLanded;
    }

    private void ShakeWhenLanded(Vector3 lastVelocity)
    {
        if (Mathf.Abs(lastVelocity.Y) > 10f || Mathf.Abs(lastVelocity.Y) > 7f)
            CamShake.ShakePreset(CamShakePresets.Roll);
    }

    public override void _Process(double delta)
    {
        if (_movement == null)
            return;

        switch (_movement.FSM.CurrentState)
        {
            case PlayerIdle:
                ChangeShakeInfluence(_idleShake);
                break;

            case PlayerAir:
                ChangeShakeInfluence(_inAirShake);
                break;

            case PlayerWalk:
                ChangeShakeInfluence(_walkShake);
                break;

            case PlayerCrouch:
                ChangeShakeInfluence(_walkShake);
                break;

            case PlayerSprint:
                ChangeShakeInfluence(_sprintShake);
                break;
            
            case PlayerSlide:
                ChangeShakeInfluence(_slideShake);
                break;

            case PlayerWallrun:
                ChangeShakeInfluence(_slideShake);
                break;

            case PlayerVerticalWallrun:
                ChangeShakeInfluence(CamShake.ShakePreset(new CamShakeInstance(0.2f, 0.5f, 25f, 0.5f, 0.5f)));
                break;

            case PlayerLadder:
                ChangeShakeInfluence(_idleShake);
                break;

            default:
                ChangeShakeInfluence(_idleShake);
                break;
        }
    }

    private void ChangeShakeInfluence(CamShakeInstance instance)
    {
        if (instance.influence >= 1f)
            return;

        ResetAllInfluences();

        instance.influence = 1f;
    }

    private void ResetAllInfluences()
    {
        _idleShake.influence = 0;
        _sprintShake.influence = 0;
        _walkShake.influence = 0;
        _inAirShake.influence = 0;
        _slideShake.influence = 0;
    }
}
