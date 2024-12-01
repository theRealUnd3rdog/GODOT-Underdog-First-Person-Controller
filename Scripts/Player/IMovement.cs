using Godot;

public interface IMovement
{
    Vector3 Velocity { get; set; }

    void SetLocalVelocity(Vector3 vel);
    Vector3 GetLocalVelocity();
    Vector3 GetPlayerDirection();
    void SetPlayerDirection(Vector3 direction);
}