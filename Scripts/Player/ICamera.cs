using Godot;

public interface ICamera
{
    void SetHeadPosition(Vector3 position);
    Vector3 GetHeadPosition();

    float Fov { get; set; }  // Add FOV property
    float NearClipPlane { get; set; }  // Add Near Clip Plane property, for example
    float FarClipPlane { get; set; }   // Add Far Clip Plane property
}