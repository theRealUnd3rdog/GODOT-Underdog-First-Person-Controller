using Godot;
using System;

public partial class SmoothFollow : Node3D
{
	private Node3D _parentNode;
	[Export] private float _smoothSpeed = 10.0f;
	private Vector3 _currentLocation;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TopLevel = true;

		_parentNode = GetParent<Node3D>();

		_currentLocation = Position;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector3 targetLoc = _parentNode.GlobalPosition;
		
		_currentLocation = _currentLocation.Lerp(targetLoc, 1.0f - Mathf.Pow(0.5f, (float)delta * _smoothSpeed));
		Position = _currentLocation;

		Transform3D newTransform = GlobalTransform;
		Quaternion curRot = newTransform.Basis.GetRotationQuaternion();
		Quaternion targetRot = _parentNode.GlobalTransform.Basis.GetRotationQuaternion();

		Quaternion newRot = curRot.Slerp(targetRot, 1.0f - Mathf.Pow(0.5f, (float)delta * _smoothSpeed));
		
		newTransform.Basis = new Basis(newRot);
		Transform = newTransform;
	}
}