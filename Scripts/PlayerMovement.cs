using Godot;
using Godot.Collections;
using MEC;
using System;
using System.Collections.Generic;
using System.Diagnostics;


public enum CameraState
{
	Normal,
	Freelooking,
	Wallrunning,
	Ladder,
}

public partial class PlayerMovement : CharacterBody3D
{
	public GodotParadiseFiniteStateMachine FSM;
	private Node3D _head;
	private Node3D _neck;
	private Node3D _eyes;
	private Node3D _mesh;

	private Vector3 _lastPhysicsPos;
	[Export] private Node3D _resetPosition;
	[Export] private CollisionShape3D _standingCollider;
	[Export] private AnimationTree _animator;
	[Export] private bool _physicsInterpolate = true;

	public CameraState camState;

	[ExportCategory("Movement")]
	[Export] public float walkingSpeed {private set; get;} = 5.0f;
	[Export] public float sprintingSpeed {private set; get;} = 8.0f;
	[Export] public float maxSpeed {private set; get;} = 12f;
	[Export] public float accelerationRate {private set; get;} = 1.0f;
	[Export] public float lerpSpeed {private set; get;} = 10.0f; // Gradually changes a value. (Adding smoothing to values)
	[Export] private float _airLerpSpeed = 3.0f;

	[ExportCategory("Jumping")]
	[Export] private float _jumpVelocity = 4.5f;
	[Export] private float _coyoteTime = 0.5f;
	[Export] private int _jumps = 1;
	private int _jumpsDone = 0;

	public float currentSpeed = 5.0f;
	public float momentum {set; get;} = 0.0f;
	public float airTime = 0.0f;
	public Vector3 direction = Vector3.Zero;
	public Vector2 inputDirection = Vector2.Zero;
	public Vector3 lastVelocity = Vector3.Zero;
	public Vector3 playerVelocity = Vector3.Zero;

	// private rotations
	private float _rotationX = 0f;
	private float _rotationZ = 0f;

	[ExportSubgroup("Z Tilt")]
	[Export] private float _zRotationLerp = 7f;
	[Export] private float _zClamp = 5f;

	// Events
	public static event Action<Vector3> VelocityChange; // Event that constantly fires when velocity changes


	[ExportSubgroup("Crouching")]
	[Export] private CollisionShape3D _crouchingCollider;
	[Export] public RayCast3D ceilingRay {private set; get;}
	[Export] public float crouchingSpeed {private set; get;} = 3.0f;
	[Export(PropertyHint.Range, "0.25f, 0.75f")] private float _crouchingDepth = 0.5f;
	private float _initialDepth;


	[ExportSubgroup("Sliding")]
	[Export] public float slideTimerMax {private set; get;} = 1.0f;
	[Export] public float slideSpeed  {private set; get;}= 10.0f;
	public float slideTimer = 0.0f;
	public Vector2 slideVector = Vector2.Zero;
	public Basis slideBasis;
	public float initialRotationY;

	[ExportSubgroup("Vaulting")]
	[Export] private RayCast3D _vaultRay;
	[Export] private RayCast3D _vaultCheck;
	[Export] private ShapeCast3D _vaultCast;
	[Export] public ShapeCast3D stepCast;
    [Export] public float vaultMomentum {private set; get;}
	[Export] public float vaultJumpVelocity {private set; get;}
	private Vector3 _vaultProjection = Vector3.Zero;
	private Vector3 _vaultPoint = Vector3.Zero;

	[ExportSubgroup("Wall running")]
	[Export] public float wallRunTime {private set; get;} = 3f;
	[Export] public float wallRunSpeed {private set; get;} = 15f;
	[Export] public float wallFrictionCoefficient {private set; get;} = 0.5f;
	[Export] public float wallJumpSpeed {private set; get;} = 4.5f;
	
	public float wallRunTimer = 0.0f;
	private PlayerWallrun _wallRunStateNode;


	[ExportSubgroup("Vertical wallrun")]
	[Export] public float verticalRunHeight {private set; get;} = 4f; // in metres

	[ExportSubgroup("Ladder")]
	[Export] public float ladderSpeed;
	public bool isLadder;
	public Ladder currentLadder;

	[ExportSubgroup("Head Bobbing")]
	[Export] private float _headBobSprintSpeed = 22.0f;
	[Export] private float _headBobWalkingSpeed = 14.0f;
	[Export] private float _headBobCrouchSpeed = 10.0f;
	[Export] private float _headBobWallrunSpeed = 48.0f;
	

	[Export] private float _headBobSprintIntensity = 0.2f; //in centimetres
	[Export] private float _headBobWalkIntensity = 0.1f;
	[Export] private float _headBobCrouchIntensity = 0.05f;
	[Export] private float _headBobWallrunIntensity = 0.4f;
	

	private Vector2 _headBobVector = Vector2.Zero; // Keep track of side to side and up and down of bob
	private float _headBobIndex = 0.0f; // Keep track of our head bob index along the sin wave
	private float _headBobCurrentIntensity = 0.0f;
	

	[ExportSubgroup("Sensitivity")]
	[Export(PropertyHint.Range, "0, 1,")] public float mouseSensitivityX = 0.4f;
	[Export(PropertyHint.Range, "0, 1,")] public float mouseSensitivityY = 0.4f;


	[ExportSubgroup("Free Looking")]
	[Export] private float _freeLookTilt = 0.3f;

	[ExportSubgroup("Field of View")]
	[Export] private Camera3D _camera;
	[Export] private float _maxFov = 98f;
	private float _minFov = 0f;
	[Export] private float _velocityExponent = 2.0f;
	[Export] private float _maxPlayerVelocity = 15f;
	[Export] private float _fovLerpSpeed = 2.0f;


	[ExportSubgroup("Interface")]
	[Export] private Label _speedLabel;
	[Export] private Label _momentumLabel;
	[Export] private Label _stateLabel;
	[Export] private Label _animationLabel;
	[Export] private Label _desiredSpeedLabel;
	[Export] private Label _previousStateLabel;

	// inputs
	public bool sprintAction {private set; get;} = false;
	private bool _previousSprintAction;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
		FSM = GetNode<GodotParadiseFiniteStateMachine>("FSM");
		_wallRunStateNode = (PlayerWallrun)FSM.GetStateByName("PlayerWallrun");

		_head = GetNode<Node3D>("Mesh/Neck/Head");
		_eyes = GetNode<Node3D>("Mesh/Neck/Head/Eyes");
		_neck = GetNode<Node3D>("Mesh/Neck");
		_mesh = GetNode<Node3D>("Mesh");

		_lastPhysicsPos = GlobalTransform.Origin;

		_initialDepth = _head.Position.Y;

		_minFov = _camera.Fov;

		// Make the mouse confined and within the center of the screen
        Input.MouseMode = Input.MouseModeEnum.Captured;

		// Events
		PlayerAir.PlayerLanded += ResetJumps;
    }

    public override void _ExitTree()
    {
        PlayerAir.PlayerLanded -= ResetJumps;
    }

    public override void _Input(InputEvent @event)
    {
		// Mouse movement on camera
		if (@event is InputEventMouseMotion eventMouseMotion)
		{

			switch (camState)
			{
				case CameraState.Freelooking:
					if (FSM.CurrentState is PlayerSlide)
					{
						RotatePlayer(eventMouseMotion.Relative.X, eventMouseMotion.Relative.Y);
					}
					else
					{
						FreeLookRotation(eventMouseMotion.Relative.X, -120f, 120f);
					}

					break;

				case CameraState.Wallrunning:
					break;
				
				case CameraState.Normal:
				
					RotatePlayer(eventMouseMotion.Relative.X, eventMouseMotion.Relative.Y);
					break;
			}
		}
    }

    public override void _Process(double delta)
    {
		HandleZRotation((float)delta);

		if (Input.IsKeyPressed(Key.R) && _resetPosition != null)
			GlobalPosition = _resetPosition.GlobalPosition;

		PhysicsInterpolation();

		HandleFOV(delta);

		HandleLabels();
    }

	private void RotatePlayer(float mouseX, float mouseY)
	{
		RotateY(Mathf.DegToRad(-mouseX * mouseSensitivityX));

		_rotationX += Mathf.DegToRad(-mouseY * mouseSensitivityY);
		_rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

		_rotationZ += Mathf.DegToRad(mouseX * mouseSensitivityX * inputDirection.Length());
		_rotationZ = Mathf.Clamp(_rotationZ, Mathf.DegToRad(-_zClamp), Mathf.DegToRad(_zClamp));
	}

	public void RotatePlayerByConstraint(float mouseX, float mouseY, float leftDeg, float rightDeg)
	{
		float rotationY = Mathf.DegToRad(-mouseX * mouseSensitivityX);
		rotationY = Mathf.Clamp(rotationY, Mathf.DegToRad(leftDeg), Mathf.DegToRad(rightDeg));

		RotateY(rotationY);

		_rotationX += Mathf.DegToRad(-mouseY * mouseSensitivityY);
		_rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

		_rotationZ += Mathf.DegToRad(mouseX * mouseSensitivityX * inputDirection.Length());
		_rotationZ = Mathf.Clamp(_rotationZ, Mathf.DegToRad(-_zClamp), Mathf.DegToRad(_zClamp));
	}

	public void RotatePlayerByConstraintUp(float mouseX, float mouseY, float leftDeg, float rightDeg, float upDeg, float downDeg)
	{
		float rotationY = Mathf.DegToRad(-mouseX * mouseSensitivityX);
		rotationY = Mathf.Clamp(rotationY, Mathf.DegToRad(leftDeg), Mathf.DegToRad(rightDeg));

		RotateY(rotationY);

		_rotationX += Mathf.DegToRad(-mouseY * mouseSensitivityY);
		_rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(downDeg), Mathf.DegToRad(upDeg));

		_rotationZ += Mathf.DegToRad(mouseX * mouseSensitivityX * inputDirection.Length());
		_rotationZ = Mathf.Clamp(_rotationZ, Mathf.DegToRad(-_zClamp), Mathf.DegToRad(_zClamp));
	}

	private void FreeLookRotation(float mouseX, float leftDeg, float rightDeg)
	{
		_neck.RotateY(Mathf.DegToRad(-mouseX * mouseSensitivityX));

		float neckClampedRotation = Mathf.Clamp(_neck.Rotation.Y, Mathf.DegToRad(leftDeg), Mathf.DegToRad(rightDeg));
		Vector3 neckRotation = new Vector3(_neck.Rotation.X, neckClampedRotation, _neck.Rotation.Z);
		_neck.Rotation = neckRotation;
	}

	public void ConstraintedRotation(float mouseX, float mouseY, float leftDeg, float rightDeg, float upDeg, float downDeg)
	{
		_neck.RotateY(Mathf.DegToRad(-mouseX * mouseSensitivityX));

		float neckClampedRotationY = Mathf.Clamp(_neck.Rotation.Y, Mathf.DegToRad(leftDeg), Mathf.DegToRad(rightDeg));
		Vector3 neckRotation = new Vector3(_neck.Rotation.X, neckClampedRotationY, _neck.Rotation.Z);
		_neck.Rotation = neckRotation;

		_rotationX += Mathf.DegToRad(-mouseY * mouseSensitivityY);
		_rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(downDeg), Mathf.DegToRad(upDeg));

		Vector3 eyeRot = new Vector3(_eyes.Rotation.X, _eyes.Rotation.Y, 0f);
		_eyes.Rotation = _eyes.Rotation.Lerp(eyeRot, 1.0f - Mathf.Pow(0.5f, (float)GetProcessDeltaTime() * lerpSpeed));
	}

	private void PhysicsInterpolation()
	{
		if (_physicsInterpolate)
		{
			double fraction = Engine.GetPhysicsInterpolationFraction();
		
			Transform3D modifiedTransform = _mesh.GlobalTransform;
			modifiedTransform.Origin = _lastPhysicsPos.Lerp(GlobalTransform.Origin, (float)fraction);

			_mesh.GlobalTransform = modifiedTransform;
		}
	}

	private void HandleFOV(double delta)
	{
		if (_camera != null)
		{
			Vector3 playerVelocity = Velocity;

			float velocityMagnitude = playerVelocity.Length() / _maxPlayerVelocity;
			float velocityScale = Mathf.Pow(velocityMagnitude, _velocityExponent);

			float desiredFOV = Mathf.Lerp(_minFov, _maxFov, velocityScale);

			desiredFOV = Mathf.Clamp(desiredFOV, _minFov, _maxFov);

			_camera.Fov = Mathf.Lerp(_camera.Fov, desiredFOV, _fovLerpSpeed * (float)delta);
		}

	}

	private void HandleZRotation(float delta)
	{
		_rotationZ = Mathf.Lerp(_rotationZ, 0f, delta * _zRotationLerp);

		Transform3D transform = _head.Transform;
		transform.Basis = Basis.Identity;
		_head.Transform = transform;

		_head.RotateObjectLocal(Vector3.Right, _rotationX);
		_head.RotateObjectLocal(Vector3.Forward, _rotationZ);
	}

	private void HandleLabels()
	{
		if (_speedLabel == null)
			return;

		_speedLabel.Text = $"VELOCITY: {Velocity.Length()}";
		_momentumLabel.Text = $"MOMENTUM: {Mathf.Round(momentum)}";
		_stateLabel.Text = $"STATE: {FSM.CurrentState.Name}";
		_desiredSpeedLabel.Text = $"DESIRED SPEED: {Mathf.Round(currentSpeed)}";
		_previousStateLabel.Text = $"PREVIOUS STATE: {FSM.PreviousState.Name}";
	}

	private void HandleAnimation()
	{
		if (_animator != null)
		{
			_animator.Set("parameters/moveState/conditions/idle", FSM.CurrentState is PlayerIdle 
						|| (inputDirection.Length() <= 0.1f 
						&& (FSM.CurrentState is not PlayerWallrun || FSM.CurrentState is not PlayerVerticalWallrun)));

			_animator.Set("parameters/moveState/conditions/moving", IsOnFloor() && (FSM.CurrentState is PlayerWalk 
						|| FSM.CurrentState is PlayerSprint || FSM.CurrentState is PlayerCrouch) && Velocity.Length() > 0.1f);
			
			_animator.Set("parameters/moveState/conditions/jump", (Input.IsActionJustPressed("jump") && airTime < _coyoteTime && 
						_jumpsDone < _jumps && !ceilingRay.IsColliding() && (!stepCast.IsColliding() 
						|| FSM.CurrentState is PlayerIdle)) || 
						(Input.IsActionJustPressed("jump") && (FSM.CurrentState is PlayerWallrun || FSM.CurrentState is PlayerLadder)));

			_animator.Set("parameters/moveState/conditions/inAir", FSM.CurrentState is PlayerAir);
			
			_animator.Set("parameters/moveState/conditions/vault", FSM.CurrentState is PlayerVault);

			_animator.Set("parameters/moveState/conditions/wallrun", FSM.CurrentState is PlayerWallrun 
						|| FSM.CurrentState is PlayerVerticalWallrun);

			_animator.Set("parameters/moveState/conditions/isLadder", FSM.CurrentState is PlayerLadder && inputDirection.Length() >= 0.9f);


			_animator.Set("parameters/moveState/wallrun/conditions/left_wallrun",
						_wallRunStateNode.wallDirection == "Right" ? true : false);

			_animator.Set("parameters/moveState/wallrun/conditions/right_wallrun",
						_wallRunStateNode.wallDirection == "Left" ? true : false);


			if (_animationLabel != null)
			{
				AnimationNodeStateMachinePlayback node = (AnimationNodeStateMachinePlayback)_animator.Get("parameters/moveState/playback");
				_animationLabel.Text = "ANIMATION: " + node.GetCurrentNode();
			}
		}
	}

	public void Crouch(double delta)
	{
		Vector3 depth;

		// Crouching
		_standingCollider.Disabled = true;
		_crouchingCollider.Disabled = false;

		depth = new Vector3(_head.Position.X, _initialDepth - _crouchingDepth, _head.Position.Z);
		_head.Position = _head.Position.Lerp(depth, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));
	}

	public void Stand(double delta)
	{
		Vector3 depth;

		_standingCollider.Disabled = false;
		_crouchingCollider.Disabled = true;

		depth = new Vector3(_head.Position.X, _initialDepth, _head.Position.Z);
		_head.Position = _head.Position.Lerp(depth, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));

		// reduce momentum here and place momentum on top of sprintingSpeed
		if (FSM.CurrentState is not PlayerSlide && momentum >= 0)
			momentum -= (float)delta * (slideSpeed / 2);
	}

	public bool CheckVault(double delta, out Vector3 vaultPoint)
	{
		vaultPoint = default(Vector3);

		// Get the raw input direction and smooth it out
		Vector3 rawProjectedXZ = new Vector3(inputDirection.X, 0f,inputDirection.Y);
		_vaultProjection = _vaultProjection.Lerp(rawProjectedXZ, 25f * (float)delta);

		// Get the projected point based on input
		Vector3 inputProjectionPoint = 3f * _vaultProjection;
		Vector3 vaultNormal = default(Vector3);

		float vaultElevation = 0f;
		float minElevation = 0.25f; // Value that decides how much elevation needed to vault

		_vaultRay.Position = new Vector3(inputProjectionPoint.X, _vaultRay.Position.Y, inputProjectionPoint.Z);

		Vector3 playerForward = this.GlobalBasis.Z;
		float angleToFloor = Mathf.RadToDeg(playerForward.AngleTo(GetFloorNormal()));

		if (_vaultRay.IsColliding())
		{
			_vaultPoint = _vaultRay.GetCollisionPoint();
			vaultNormal = _vaultRay.GetCollisionNormal();

			vaultElevation = Math.Abs((_vaultPoint - this.GlobalPosition).Y);

			_vaultCast.Enabled = true;
		}
		else
		{
			_vaultCast.Enabled = false;
		}

		_vaultCast.GlobalPosition = _vaultPoint;

		if (inputDirection.Y < 0f && !ceilingRay.IsColliding() && _vaultCast.IsColliding() && _vaultCheck.IsColliding()
				&& !stepCast.IsColliding() && (angleToFloor > 80f || Mathf.IsZeroApprox(angleToFloor)))
		{
			if (vaultElevation > minElevation || IsOnWall())
			{
				vaultPoint = _vaultPoint;
				DebugDraw3D.DrawSphere(_vaultPoint, 0.25f, Colors.Red);

				return true;
			}
			
		}

		return false;
	}

	public bool CheckLadder()
	{
		if (currentLadder == null)
			return false;

		Node3D area = currentLadder.GetNode<Node3D>("LadderArea/LadderCollider");

		if (area == null)
			return false;

		if (!isLadder)
			return false;

		// Check if player is facing ladder
		Vector3 wallDirection = default;
		Vector3 wallPoint = default;

		// Get forward ray
		bool forwardRay = SendRayInDirection(-GlobalBasis.Z, 0.5f, out Vector3 rayNormal, out Vector3 rayPoint, out GodotObject collider);

		float dotCollision = Mathf.Abs(rayNormal.Dot(Vector3.Up));

		// Threshold to how slanted the wall can be
		if (dotCollision < 0.3f)
		{
			Vector3 playerForward = -GlobalBasis.Z;
			
			float forwardAngle = Mathf.RadToDeg(playerForward.AngleTo(-rayNormal));

			wallDirection = GlobalBasis.X.Cross(rayNormal).Normalized();
			wallDirection = new Vector3(
				Mathf.Abs(wallDirection.X),
				Mathf.Abs(wallDirection.Y),
				Mathf.Abs(wallDirection.Z)
			);

			Vector3 ladderZOffset = -rayNormal * -0.5f;

			float nearestBarHeight = Mathf.Round(GlobalPosition.Y / currentLadder.BarSpacing) * currentLadder.BarSpacing + 1f;
			wallPoint = new Vector3(area.GlobalPosition.X, nearestBarHeight, area.GlobalPosition.Z);

			// Check if not facing completely side ways to the wall and in general direction
			if (forwardAngle > 0 && forwardAngle < 20f)
			{
				// Check if player velocity is more than or equal to 0
				if (inputDirection.Y < 0f)
				{
					DebugDraw3D.DrawArrow(wallPoint, wallPoint + (wallDirection * 3f), Colors.Red, 0.2f, false, 5f);

					
					GlobalPosition = wallPoint + ladderZOffset;
					return true;
				}
			}
		}

		return false;
	}

	private void ResetJumps() => _jumpsDone = 0;

	public bool IsRunningUpSlope()
	{
		float dot = GetFloorNormal().Dot(-Transform.Basis.Z);
		
		if (dot < 0f)
			return true;
		else 
			return false;
	}

	private void HandleJump(float jumpSpeed)
	{
		// Handle Jump (Must refactor)
		if (Input.IsActionJustPressed("jump") && ((IsOnFloor() && !ceilingRay.IsColliding()) || airTime < _coyoteTime)
			&& (!stepCast.IsColliding() || FSM.CurrentState is PlayerIdle))
		{
			// Jump
			if (_jumpsDone < _jumps)
			{
				Jump(jumpSpeed);
			}
		}
		else if (Input.IsActionJustPressed("jump") && _jumpsDone > 0 && _jumpsDone < _jumps)
		{
			// Jump
			Jump(jumpSpeed);
		}
	}

	public void Jump(float jumpSpeed)
	{
		if (FSM.CurrentState is PlayerVault)
		{
			Vector3 velocity = Velocity;

			velocity = jumpSpeed * Vector3.Up;
			
			Velocity = velocity;
		}
		else
		{
			playerVelocity.Y = jumpSpeed;
		}
		
		_jumpsDone++;
	}

	public void WallJump(Vector3 wallDir)
	{
		playerVelocity.Y = wallJumpSpeed / 2; 
		direction = ((wallDir.Normalized()) + (-_camera.GlobalBasis.Z.Normalized() / 4)) * (wallJumpSpeed / 16);
	}

	public void RollPlayer(Vector3 directionFromAir)
	{
		// Check if current direction from camera is close to the direction from air.
		
		// Check if there's some forward velocity

		// Then land and execute animation.
	}
	
	public bool CheckWall(out KinematicCollision3D collision, out String direction)
	{
		int count = GetSlideCollisionCount();
		direction = String.Empty;
		collision = default(KinematicCollision3D);

		if (count <= 0)
			return false;
		
		if (inputDirection.Y >= 0)
			return false;
		
		List<KinematicCollision3D> collisions = new List<KinematicCollision3D>();
		
		for (int i = 0; i < count; i++)
		{
			collisions.Add(GetSlideCollision(i));
		}

		foreach (KinematicCollision3D c in collisions)
		{
			Vector3 collisionNormal = c.GetNormal();
			Vector3 collisionPoint = c.GetPosition();

			float dotCollision = Mathf.Abs(collisionNormal.Dot(Vector3.Up)); // Get the dot product to see if the wall is side ways

			if (dotCollision < 0.1f)
			{
				Vector3 playerForward = this.GlobalBasis.Z;
				float angleToWall = Mathf.RadToDeg(playerForward.AngleTo(collisionNormal));
				float signedAngle = Mathf.RadToDeg(playerForward.SignedAngleTo(collisionNormal, Vector3.Up));			

				// Check if camera is facing somewhat in that direction
				if (angleToWall < 105f && angleToWall > 25f)
				{
					//DebugDraw3D.DrawSquare(c.GetPosition(), 0.1f, Colors.Blue);

					direction = Mathf.Sign(signedAngle) > 0 ? "Left" : "Right";
					collision = c;
					
					//GD.Print(direction);
					
					return true;
				}
			}
		}

		return false;
	}

	public bool CheckVerticalWall(out Vector3 wallDirection, out Vector3 wallPoint)
	{
		wallDirection = default;
		wallPoint = default;

		// Get forward ray
		bool forwardRay = SendRayInDirection(-GlobalBasis.Z, 0.5f, out Vector3 rayNormal, out Vector3 rayPoint);

		if (forwardRay)
		{
			float dotCollision = Mathf.Abs(rayNormal.Dot(Vector3.Up));

			// Threshold to how slanted the wall can be
			if (dotCollision < 0.3f)
			{
				Vector3 cameraUp = _camera.GlobalBasis.Y;
				Vector3 playerForward = GlobalBasis.Z;
				
				float upAngleDot = rayNormal.Dot(cameraUp);
				float forwardAngle = Mathf.RadToDeg(playerForward.AngleTo(rayNormal));

				wallDirection = GlobalBasis.X.Cross(rayNormal).Normalized();
				wallDirection = new Vector3(
					Mathf.Abs(wallDirection.X),
					Mathf.Abs(wallDirection.Y),
					Mathf.Abs(wallDirection.Z)
				);

				wallPoint = rayPoint;

				// Check if not facing completely side ways to the wall and in general direction
				if (forwardAngle > 0 && forwardAngle < 20f)
				{
					// Check if camera is facing somewhat upwards
					if (upAngleDot > 0.2f)
					{
						// Check if player velocity is more than or equal to 0
						if (Velocity.Y >= 0)
						{
							DebugDraw3D.DrawArrow(wallPoint, wallPoint + (wallDirection * 1f), Colors.Aqua, 0.2f);
							return true;
						}
					}
				}
			}
		}

		return false;
	}

	public bool SendRayInDirection(Vector3 direction, float range, out Vector3 rayNormal, out Vector3 rayPoint)
	{
		// Send ray in direction of wall
        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;

        Vector3 rayOrigin = _camera.GlobalPosition;
        Vector3 rayEnd = rayOrigin + (direction * range);

		rayNormal = default(Vector3);
		rayPoint = default(Vector3);

		uint layerMask = (1 << 1)  | (1 << 4);

        PhysicsRayQueryParameters3D parameters = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd, layerMask);
		parameters.HitBackFaces = false;
		parameters.HitFromInside = false;

        var rayArray = spaceState.IntersectRay(parameters);

        if (rayArray.ContainsKey("collider"))
        {
			rayArray.TryGetValue("normal", out Variant normal);
			rayArray.TryGetValue("position", out Variant position);
			rayArray.TryGetValue("collider", out Variant colliderVariant);

			DebugDraw3D.DrawArrow(rayOrigin, rayEnd, Colors.GreenYellow, 0.2f);

			rayNormal = normal.AsVector3();
			rayPoint = position.AsVector3();

            return true;
        }

		return false;
	}

	public bool SendRayInDirection(Vector3 direction, float range, out Vector3 rayNormal, out Vector3 rayPoint, out GodotObject collider)
	{
		// Send ray in direction of wall
        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;

        Vector3 rayOrigin = _camera.GlobalPosition;
        Vector3 rayEnd = rayOrigin + (direction * range);

		rayNormal = default(Vector3);
		rayPoint = default(Vector3);
		collider = default(GodotObject);

		uint layerMask = (1 << 1)  | (1 << 4);

        PhysicsRayQueryParameters3D parameters = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd, layerMask);
		parameters.HitBackFaces = false;
		parameters.HitFromInside = false;
		parameters.CollideWithAreas = true;
		parameters.CollideWithBodies = true;

        var rayArray = spaceState.IntersectRay(parameters);

        if (rayArray.ContainsKey("collider"))
        {
			rayArray.TryGetValue("normal", out Variant normal);
			rayArray.TryGetValue("position", out Variant position);
			rayArray.TryGetValue("collider", out Variant colliderVariant);

			DebugDraw3D.DrawArrow(rayOrigin, rayEnd, Colors.GreenYellow, 0.2f);

			rayNormal = normal.AsVector3();
			rayPoint = position.AsVector3();
			collider = colliderVariant.AsGodotObject();

            return true;
        }

		return false;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		inputDirection = inputDir;

		VelocityChange?.Invoke(Velocity); // Invoke change in velocity event

		HandleAnimation();

		sprintAction = _previousSprintAction;

		if (IsOnFloor())
			sprintAction = !Input.IsActionPressed("sprint");

		// Handle free looking
		if (Input.IsActionPressed("free_look") || FSM.CurrentState is PlayerSlide)
		{
			camState = CameraState.Freelooking;

			// Slide Tilt
			if (FSM.CurrentState is PlayerSlide)
			{
				Vector3 eyeRotation = _eyes.Rotation;
				eyeRotation.Z = -Mathf.DegToRad(7f);
				_eyes.Rotation = _eyes.Rotation.Lerp(eyeRotation, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));
			}
			else
			{
				Vector3 eyeRotation = _eyes.Rotation;
				eyeRotation.Z = -Mathf.DegToRad(_neck.Rotation.Y * _freeLookTilt);
				_eyes.Rotation = eyeRotation;
			}
		}
		// If not free looking return to normal camera state
		else
		{
			if (FSM.CurrentState is not PlayerWallrun && FSM.CurrentState is not PlayerLadder)
			{
				camState = CameraState.Normal;

				Vector3 neckRot = new Vector3(0f, 0f, _neck.Rotation.Z);
				_neck.Rotation = _neck.Rotation.Lerp(neckRot, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));

				Vector3 eyeRot = new Vector3(_eyes.Rotation.X, _eyes.Rotation.Y, 0f);
				_eyes.Rotation = _eyes.Rotation.Lerp(eyeRot, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));
			}
		}

		// Handle head bob
		switch (FSM.CurrentState)
		{
			case PlayerWalk:
				_headBobCurrentIntensity = _headBobWalkIntensity;
				_headBobIndex += _headBobWalkingSpeed * (float)delta;
				break;
			
			case PlayerSprint:
				_headBobCurrentIntensity = _headBobSprintIntensity;
				_headBobIndex += _headBobSprintSpeed * (float)delta;
				break;
			
			case PlayerCrouch:
				_headBobCurrentIntensity = _headBobCrouchIntensity;
				_headBobIndex += _headBobCrouchSpeed* (float)delta;
				break;

			case PlayerWallrun:
				_headBobCurrentIntensity = _headBobWallrunIntensity;
				_headBobIndex += _headBobWallrunSpeed * (float)delta;
				break;

			default:
				break;
		}

		if ((IsOnFloor() && FSM.CurrentState is not PlayerSlide && inputDir != Vector2.Zero))
		{
			Vector2 headBob;

			headBob.Y = Mathf.Sin(_headBobIndex);
			headBob.X = Mathf.Sin(_headBobIndex / 2) + 0.5f;

			_headBobVector = headBob;
			
			Vector3 eyes = _eyes.Position;
			
			eyes.Y = Mathf.Lerp(eyes.Y, _headBobVector.Y * (_headBobCurrentIntensity / 2.0f), 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));
			eyes.X = Mathf.Lerp(eyes.X, _headBobVector.X * _headBobCurrentIntensity, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));

			_eyes.Position = eyes;
		}
		else
		{
			Vector3 eyes = _eyes.Position;
			eyes.Y = Mathf.Lerp(eyes.Y, 0.0f, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));
			eyes.X = Mathf.Lerp(eyes.X, 0.0f, 1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));

			_eyes.Position = eyes;
		}

		playerVelocity = Velocity;


		if (IsOnFloor())
		{
			direction = direction.Lerp((Transform.Basis * new Vector3(inputDir.X, 0f, inputDir.Y)).Normalized(), 
				1.0f - Mathf.Pow(0.5f, (float)delta * lerpSpeed));
		}
		else
		{
			if (!FSM.CurrentStateNameIs("PlayerWallrun") && !FSM.CurrentStateNameIs("PlayerLadder"))
			{
				playerVelocity.Y -= gravity * (float)delta;

				if (inputDir != Vector2.Zero)
				{
					direction = direction.Lerp((Transform.Basis * new Vector3(inputDir.X, 0f, inputDir.Y)).Normalized(), 
						1.0f - Mathf.Pow(0.5f, (float)delta * _airLerpSpeed));
				}
			}
		}

		if (FSM.CurrentState is not PlayerVault || FSM.CurrentState is not PlayerWallrun
			|| FSM.CurrentState is not PlayerLadder || FSM.CurrentState is not PlayerVerticalWallrun)
			HandleJump(_jumpVelocity);

		if (direction != Vector3.Zero)
		{
			playerVelocity.X = direction.X * currentSpeed;
			playerVelocity.Z = direction.Z * currentSpeed;
		}
		else
		{
			playerVelocity.X = Mathf.MoveToward(Velocity.X, 0, currentSpeed);
			playerVelocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentSpeed);
		}

		Velocity = playerVelocity;

		_lastPhysicsPos = GlobalTransform.Origin;
		lastVelocity = playerVelocity;
		
		_previousSprintAction = sprintAction;
		MoveAndSlide();
	}

	private void OnCollisionCheckerAreaEntered(Area3D area)
	{
		string group = CollisionChecker.GetGroupFromBody(area);

		// Check if current group is ladder
		if (group == "Ladder")
		{
			isLadder = true;
			currentLadder = area.GetParent<Ladder>();
		}
	}

	private void OnCollisionCheckerAreaExited(Area3D area)
	{
		string group = CollisionChecker.GetGroupFromBody(area);

		if (group == "Ladder")
		{
			isLadder = false;
		}
	}
}
