using Godot;

public partial class MovementState : GodotParadiseState
{
    protected Movement Movement;
    protected Camera Camera;

    public override void Enter()
    {
        Movement = GetOwner<Movement>();

        if (Movement != null)
            Camera = Movement.FindChild("Camera3D", true, false) as Camera;
        else
        {
            GD.PrintErr("Player Movement script does not exist on owner node");
        }
    }
}