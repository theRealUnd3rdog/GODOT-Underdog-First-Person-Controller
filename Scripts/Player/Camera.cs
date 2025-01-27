using Godot;
using MEC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Camera : Camera3D, ICamera
{
	private Movement _movement;
	
	[Export] private Node3D _mesh;
	[Export] private Node3D _headMesh;
	
	[Export] private Node3D _head;
	[Export] private Node3D _neck;
	[Export] private Node3D _eyes;

	// private rotations in radians
	private float _rotationY = 0f;
	private float _rotationX = 0f;
	private float _rotationZ = 0f;

	private float _currentAngle = 0f; // Value that shows the angle between the mesh and the actual player rotation
	private float _previousAngle = 0f; // Previous angle between mesh and player


	[ExportSubgroup("Sensitivity")]
	[Export(PropertyHint.Range, "0, 1,")] public float mouseSensitivityX {private set; get;} = 0.25f;
	[Export(PropertyHint.Range, "0, 1,")] public float mouseSensitivityY {private set; get;} = 0.2f;

	[ExportCategory("Camera Settings")]
	[Export] public float standDuration {private set; get;} // in seconds

	// Head bobbing
	private Vector2 _headBobVector = Vector2.Zero; // Keep track of side to side and up and down of bob
	private float _headBobIndex = 0.0f; // Keep track of our head bob index along the sin wave
	private float _headBobCurrentIntensity = 0.0f;
	private float _headBobLerpSpeed = 5f;


	// Implementing the Camera3D properties directly through the interface
    public float Fov
    {
        get => this.Fov;
        set => this.Fov = value;
    }

    public float NearClipPlane
    {
        get => this.Near;
        set => this.Near = value;
    }

    public float FarClipPlane
    {
        get => this.Far;
        set => this.Far = value;
    }

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_movement = GetOwner<Movement>();
		if (_movement == null) GD.PushWarning("Movement script not assigned!");

		if (_mesh == null)
			GD.PushWarning("Mesh node not assigned!");

		// Assign correct node paths
		/* _eyes = GetNode<Node3D>("/root/Mesh/Neck/Head/Eyes");
		_head = GetNode<Node3D>("/root/Mesh/Neck/Head");
		_neck = GetNode<Node3D>("/root/Mesh/Neck");
 		*/
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			RotateCamera(eventMouseMotion.Relative.X, eventMouseMotion.Relative.Y);
		}
	}

	public override void _Process(double delta)
	{
		// modify current angle
		Node3D nodeA = _neck;
		Node3D nodeB = _mesh;

		_currentAngle = Mathf.RadToDeg(GetSignedYAngleBetween(nodeA, nodeB));

		// Debug arrow to visualize mesh direction
    	//DebugDraw3D.DrawArrow(_movement.Position, _movement.Position + (-nodeB.Basis.Z * 2f), Colors.Red, 0.2f);

		Vector2 filteredInput = _movement.GetFilteredInputDirection();
		Vector3 desiredDirection = nodeA.Transform.Basis * new Vector3(filteredInput.X, 0, filteredInput.Y).Normalized();

		// Debug arrow to visualize filtered input direction
		//DebugDraw3D.DrawArrow(_movement.Position, _movement.Position + (desiredDirection * 4f), Colors.Blue, 0.2f);
	}

	/// <summary>
	/// Master rotation of neck's Y axis and head's X axis
	/// </summary>
	private void RotateCamera(float mouseX, float mouseY)
	{
		// Apply Rotation Y
		_rotationY = Mathf.DegToRad(-mouseX * mouseSensitivityX);

		// Set the rotation of the player
		_neck.RotateY(_rotationY);

		// Apply Rotation X and apply the constraints
		_rotationX += Mathf.DegToRad(-mouseY * mouseSensitivityY);
		_rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

		Transform3D transform = _head.Transform;
		transform.Basis = Basis.Identity;
		_head.Transform = transform;

		// Rotate the X axis
		_head.RotateObjectLocal(Vector3.Right, _rotationX);

		_headMesh.Transform = GetLerpedTargetRotation(_headMesh.Transform, _head.GlobalTransform,
												 1.0f - Mathf.Pow(0.5f, (float)GetProcessDeltaTime() * 15f));
	}

	/// <summary>
	/// Method that rotates the body mesh to a filtered direction (forward, forward left, forward right) based on input
	/// </summary>
	public void RotateBodyMeshDirection()
	{
		Node3D nodeA = _neck;
		Node3D nodeB = _mesh;

		Vector3 desiredDirection = _movement.GetPlayerDirection();

		Transform3D target = GetLerpedTargetRotation(nodeB.Transform, desiredDirection,
												 1.0f - Mathf.Pow(0.5f, (float)GetProcessDeltaTime() * 15f));
		_mesh.Transform = target;
	}

	/// <summary>
	/// Method that rotates the body mesh to a filtered direction (forward, forward left, forward right) based on input
	/// </summary>
	public void RotateBodyMeshInput()
	{
		Node3D nodeA = _neck;
		Node3D nodeB = _mesh;

		Vector2 filteredInput = _movement.GetFilteredInputDirection();
		Vector3 desiredDirection = nodeA.Transform.Basis * new Vector3(filteredInput.X, 0, filteredInput.Y).Normalized();

		Transform3D target = GetLerpedTargetRotation(nodeB.Transform, desiredDirection,
												 1.0f - Mathf.Pow(0.5f, (float)GetProcessDeltaTime() * 15f));
		_mesh.Transform = target;
	}

	/// <summary>
	/// Coroutine that aligns the mesh Y rotation with the neck Y rotation with a 90 degree constraint.
	/// If angular velocity surpasses a particular threshold, it will align constantly.
	/// </summary>
	public IEnumerator<double> RunBodyMeshRotation()
	{
		float angleThreshold = 90f;
		float angularVelocityThreshold = 500f;

		float alignmentDuration = 0.3f;

		while (true)
		{
			float absoluteAngle = Mathf.Abs(_currentAngle);
			float rateOfChange = GetYAngleRateOfChange(absoluteAngle, ref _previousAngle);

			if (Mathf.Abs(absoluteAngle) > angleThreshold)
			{
				// Calculate the rate of change of the angle
				
				switch (Mathf.Sign(_currentAngle))
				{
					case -1:
						GD.Print("Turning left");
						break;
					
					case 1:
						GD.Print("Turning right");
						break;
				}

				// Decide which alignment method to use
				CoroutineHandle handle = rateOfChange < angularVelocityThreshold
					? Timing.RunCoroutine(AlignMeshWithDirection(alignmentDuration), Segment.Process, "AlignMeshRot")
					: Timing.RunCoroutine(AlignMeshWithDirectionConstant(), Segment.Process, "AlignMeshRotConstant");

				yield return Timing.WaitUntilDone(handle);
			}

			// Wait one frame
			yield return Timing.WaitForOneFrame;
		}
	}

	public void SetHeadPosition(Vector3 position)
	{
		_head.Position = position;
	}

	public Vector3 GetHeadPosition()
	{
		return _head.Position;
	}

	public float GetCurrentAngle()
	{
		return _currentAngle;
	}

	public void StartStanding() => Timing.RunCoroutine(Stand(standDuration), Segment.PhysicsProcess, "Stand");
	public void StopStanding() => Timing.KillCoroutines("Stand");

	private IEnumerator<double> Stand(float duration)
	{
		float timeElapsed = 0f;
		
		Vector3 depth;
		float initialDepth = GetHeadPosition().Y;

		_movement.SetColliderState(true);

        do
        {
            timeElapsed += (float)GetPhysicsProcessDeltaTime();
			float normalizedTime = timeElapsed / duration;

			Vector3 headPos = GetHeadPosition();
			depth = new Vector3(headPos.X, initialDepth, headPos.Z);

			SetHeadPosition(headPos.Lerp(depth, normalizedTime));

            yield return Timing.WaitForOneFrame;
        }
        while (timeElapsed < duration);
	}

	public void SetHeadBob(float headBobIntensity, float headBobSpeed)
	{
		_headBobCurrentIntensity = headBobIntensity;
		_headBobIndex += headBobSpeed * (float)GetProcessDeltaTime();
	}

	public void HeadBob()
	{
		double delta = GetProcessDeltaTime();

		Vector2 headBob;
		Vector3 eyes = _eyes.Position;

		if (_movement.GetRawInputDirection() != Vector2.Zero)
		{
			headBob.Y = Mathf.Sin(_headBobIndex);
			headBob.X = Mathf.Sin(_headBobIndex / 2) + 0.5f;

			_headBobVector = headBob;
			
			eyes.Y = Mathf.Lerp(eyes.Y, _headBobVector.Y * (_headBobCurrentIntensity / 2.0f), 1.0f - Mathf.Pow(0.5f, (float)delta * _headBobLerpSpeed));
			eyes.X = Mathf.Lerp(eyes.X, _headBobVector.X * _headBobCurrentIntensity, 1.0f - Mathf.Pow(0.5f, (float)delta * _headBobLerpSpeed));

			_eyes.Position = eyes;
		}
		else
		{
			eyes.Y = Mathf.Lerp(eyes.Y, 0.0f, 1.0f - Mathf.Pow(0.5f, (float)delta * _headBobLerpSpeed));
			eyes.X = Mathf.Lerp(eyes.X, 0.0f, 1.0f - Mathf.Pow(0.5f, (float)delta * _headBobLerpSpeed));

			_eyes.Position = eyes;
		}
	}

	private IEnumerator<double> AlignMeshWithDirection(float duration)
	{
		if (_mesh == null || _movement == null)
			yield break;

		float timeElapsed = 0f;

        do
        {
            timeElapsed += (float)GetProcessDeltaTime();
			float normalizedTime = timeElapsed / duration;

			Node3D nodeA = _neck;
			Node3D nodeB = _mesh;

			Transform3D target = GetLerpedTargetRotation(nodeB.Transform, nodeA.Transform, normalizedTime);
			_mesh.Transform = target;

            yield return Timing.WaitForOneFrame;
        }
        while (timeElapsed < duration);
	}

	public IEnumerator<double> AlignMeshWithDirectionConstant()
	{
		if (_mesh == null || _movement == null)
			yield break;

		float angleThreshold = 5f; // When angle(degrees) reaches this below this value, this coroutine will stop
		float smoothSpeed = 15f;

        do
        {
			Node3D nodeA = _neck;
			Node3D nodeB = _mesh;

			Transform3D target = GetLerpedTargetRotation(nodeB.Transform, nodeA.Transform,
												 1.0f - Mathf.Pow(0.5f, (float)GetProcessDeltaTime() * smoothSpeed));
			_mesh.Transform = target;

            yield return Timing.WaitForOneFrame;
        }
        while (Mathf.Abs(_currentAngle) > angleThreshold);
	}

	public void FollowMeshToNeck()
	{
		float smoothSpeed = 5f;
		
		Node3D nodeA = _neck;
		Node3D nodeB = _mesh;

		Transform3D target = GetLerpedTargetRotation(nodeB.Transform, nodeA.Transform,
												1.0f - Mathf.Pow(0.5f, (float)GetProcessDeltaTime() * smoothSpeed));
		_mesh.Transform = target;
	}

	/// <summary>
	/// Get signed Y angle between 2 nodes in radians
	/// </summary>
	/// <param name="nodeA">First node</param>
	/// <param name="nodeB">Second node</param>
	public static float GetSignedYAngleBetween(Node3D nodeA, Node3D nodeB)
	{
		// Get the forward directions (Z axis) of each node in the XZ plane, ignoring the Y component
		Vector3 forwardA = new Vector3(nodeA.GlobalTransform.Basis.Z.X, 0, nodeA.GlobalTransform.Basis.Z.Z).Normalized();
		Vector3 forwardB = new Vector3(nodeB.GlobalTransform.Basis.Z.X, 0, nodeB.GlobalTransform.Basis.Z.Z).Normalized();

		// Calculate the angle between the forward directions of nodeA and nodeB
		float angle = forwardA.AngleTo(forwardB);

		// Determine the sign of the angle for left/right distinction
		float sign = Mathf.Sign(forwardA.Cross(forwardB).Y);

		return angle * sign; // Signed angle in radians (positive for right, negative for left)
	}

	/// <summary>
	/// Get signed Y angle between 2 vectors
	/// </summary>
	public static float GetSignedYAngleBetween(Vector3 vectorA, Vector3 vectorB)
	{
		// Normalize both vectors in the XZ plane, ignoring the Y component
		Vector3 forwardA = new Vector3(vectorA.X, 0, vectorA.Z).Normalized();
		Vector3 forwardB = new Vector3(vectorB.X, 0, vectorB.Z).Normalized();

		// Calculate the unsigned angle between the two vectors
		float angle = Mathf.Acos(forwardA.Dot(forwardB)); // Dot product gives the cosine of the angle

		// Determine the sign of the angle for left/right distinction
		float sign = Mathf.Sign(forwardA.Cross(forwardB).Y);

		// Return the signed angle
		return angle * sign; // Signed angle in radians
	}


	/// <summary>
	/// Get rate of change of angle value as an angular velocity
	/// </summary>
	private float GetYAngleRateOfChange(float currentAngle, ref float previousAngle)
	{
		float delta = (float)GetProcessDeltaTime();

		// Calculate the rate of change of the angle (angular velocity)
		float angularVelocity = Mathf.Abs(currentAngle - previousAngle) / delta;

		// Update the previous angle for the next call
		previousAngle = currentAngle;

		return angularVelocity;
	}

	/// <summary>
	/// Get Lerped Transform from current rotation to target rotation. 
	/// Rotation is based on Godot Quaternion.
	/// </summary>
	public static Transform3D GetLerpedTargetRotation(Transform3D curTransform, Transform3D targetTransform, float duration)
	{
		Transform3D newTransform = curTransform;
		Vector3 originalScale = newTransform.Basis.Scale; // Get the original scale

		Quaternion curRot = newTransform.Basis.GetRotationQuaternion();
		Quaternion targetRot = targetTransform.Basis.GetRotationQuaternion();
		Quaternion newRot = curRot.Slerp(targetRot, duration);

		// Create a new Basis from the rotation, then scale it
		Basis rotatedBasis = new Basis(newRot);
		rotatedBasis = rotatedBasis.Scaled(originalScale); // Apply the original scale to the new Basis

		newTransform.Basis = rotatedBasis;
		return newTransform;
	}

	/// <summary>
	/// Get Lerped Transform from current rotation to target Y Vector Rotation.
	/// Rotate Transform to a particular Vector.
	/// </summary>
	public static Transform3D GetLerpedTargetRotation(Transform3D curTransform, Vector3 targetRotation, float duration)
	{
		Transform3D newTransform = curTransform;

		// Preserve the original scale
		Vector3 originalScale = newTransform.Basis.Scale;

		// Get the current rotation as a Quaternion
		Quaternion curRot = newTransform.Basis.GetRotationQuaternion();

		Vector3 normalizedDirection = targetRotation.Normalized();
    	float targetYRotation = Mathf.Atan2(-normalizedDirection.X, -normalizedDirection.Z);
		float curX = curTransform.Basis.GetRotationQuaternion().X;
		float curZ = curTransform.Basis.GetRotationQuaternion().Z;

		Vector3 newVector = new Vector3(curX, targetYRotation, curZ);

		// Convert the target rotation (Euler angles) to a Quaternion
		Quaternion targetRot = Quaternion.FromEuler(newVector); // Corrected line

		// Interpolate between the current and target rotation
		Quaternion newRot = curRot.Slerp(targetRot, duration);

		// Create a new Basis from the interpolated rotation and apply the original scale
		Basis rotatedBasis = new Basis(newRot);
		rotatedBasis = rotatedBasis.Scaled(originalScale);

		// Update the transform with the new Basis
		newTransform.Basis = rotatedBasis;

		return newTransform;
	}


}
