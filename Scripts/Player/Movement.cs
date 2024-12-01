using Godot;
using System;
using System.Collections.Generic;
using MEC;

public partial class Movement : CharacterBody3D, IMovement
{
	// Finite state machine
	public GodotParadiseFiniteStateMachine FSM;

	// grabbables
	private Node3D _body;
	private Node3D _neck;

	// references
	private CollisionShape3D _collider;
	private AnimationTree _animationTree;

	// current speeds
	private float _currentSpeed;
	private float _desiredSpeed;
	private float _momentum;

	// directions
	private Vector3 _playerDirection;
	private Vector3 _stableDirection = Vector3.Zero;

	// velocities
	private Vector3 _localVelocity;
	private Vector3 _lastVelocity;

	// position
	private Vector3 _lastPhysicsPos;

	// Delete later
	[Export] private Node3D _resetPosition;

	[ExportCategory("Movement")]
	[Export] public float maxSpeed {private set; get;} // metres per second

	[Export] public float accelerationTime {private set; get;} = 0.1f; // in seconds
	private float _accelerationRate;

	[Export] public float decelerationTime {private set; get;} = 0.5f; // in seconds
	private float _decelerationRate;

	// Value that is used to determine how long it takes to change the player direction
	[Export] public float directionChangeTime {private set; get;} = 0.3f; // in seconds

	// Value that by default is set to default gravity but can be changed.
	public float gravity {set; get;} = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	[ExportSubgroup("Interpolation")]
	[Export] private bool _physicsInterpolate;

	// For testing
	[ExportSubgroup("Interface")]
	[Export] private Label _speedLabel;
	[Export] private Label _momentumLabel;
	[Export] private Label _stateLabel;
	[Export] private Label _animationLabel;
	[Export] private Label _desiredSpeedLabel;
	[Export] private Label _previousStateLabel;
	

	public override void _Ready()
	{
		// Get Finite state machine
		FSM = GetNode<GodotParadiseFiniteStateMachine>("FSM");

		// Grab all node references
		_body = GetNode<Node3D>("Body");
		_neck = GetNode<Node3D>("Body/Neck");

		_collider = GetNode<CollisionShape3D>("Standing_collision_shape");

		// Set a default desiredSpeed in the beginning (value will change on other states automatically)
		_desiredSpeed = 5f;
	}

	public override void _Process(double delta)
	{
		if (_physicsInterpolate)
		{
			PhysicsInterpolation();
		}

		if (Input.IsKeyPressed(Key.R) && _resetPosition != null)
			GlobalPosition = _resetPosition.GlobalPosition;

		HandleLabels();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDir = GetInputDirection();
		_localVelocity = Velocity;

		// Compute the desired direction based on input
		Vector3 desiredDirection = _neck.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

		// Check if the input direction is stable (not changing significantly)
		if (inputDir != Vector2.Zero)
		{
			// Update the stable direction only when input is stable
			_stableDirection = desiredDirection;
		}

		// Smoothly transition towards the stable direction
		_playerDirection = _playerDirection.Lerp(
			_stableDirection,
			1.0f - Mathf.Exp(-(float)delta / directionChangeTime)
		);

		GD.Print(_playerDirection.Length());

		// Weight (0,1) that will control how much the velocity should go down to 0 when decellerating
		_localVelocity.X = _playerDirection.X * _currentSpeed;
		_localVelocity.Z = _playerDirection.Z * _currentSpeed;

		_lastPhysicsPos = GlobalTransform.Origin;
		Velocity = _localVelocity;

		MoveAndSlide();
	}

	private void PhysicsInterpolation()
	{
		double fraction = Engine.GetPhysicsInterpolationFraction();

		if (_body == null)
		{
			GD.PrintErr("Mesh object not found!");
			return;
		}
	
		Transform3D modifiedTransform = _body.GlobalTransform;
		modifiedTransform.Origin = _lastPhysicsPos.Lerp(GlobalTransform.Origin, (float)fraction);

		_body.GlobalTransform = modifiedTransform;
	}

	private void HandleLabels()
	{
		if (_speedLabel == null)
			return;

		_speedLabel.Text = $"VELOCITY: {Velocity.Length()}";
		_stateLabel.Text = $"STATE: {FSM.CurrentState.Name}";
		_desiredSpeedLabel.Text = $"DESIRED SPEED: {Mathf.Round(_currentSpeed)}";
		_previousStateLabel.Text = $"PREVIOUS STATE: {FSM.PreviousState.Name}";
	}

	public void SetLocalVelocity(Vector3 vel)
	{
		_localVelocity = vel;
		Velocity = _localVelocity;
	}

	public void SetYVelocity(float yVel)
	{
		Velocity = new Vector3(Velocity.X, yVel, Velocity.Z);
	}

	public Vector3 GetLocalVelocity()
	{
		return _localVelocity;
	}

	/// <summary>
	/// Sets the desired speed of the controller, value that should only change on 1 call
	/// </summary>
	public void SetDesiredSpeed(float desiredSpeed)
	{
		_desiredSpeed = desiredSpeed;
	}

	/// <summary>
	/// Update the current speed to it's accel/decel times.
	/// </summary>
	public void Accelerate(float delta, float desiredSpeed)
	{
		if (_currentSpeed < desiredSpeed)
		{
			// Acceleration logic
			_accelerationRate = (desiredSpeed - _currentSpeed) / accelerationTime;
			_currentSpeed += _accelerationRate * delta;

			// Clamp to ensure we don't overshoot the desired speed
			_currentSpeed = Mathf.Clamp(_currentSpeed, 0, desiredSpeed);
		}
	}

	/// <summary>
	/// Update the current speed to it's accel/decel times.
	/// </summary>
	public void Deccelerate(float delta, float desiredSpeed)
	{
		if (_currentSpeed > desiredSpeed)
		{
			// Deceleration logic
			_decelerationRate = (_currentSpeed - desiredSpeed) / decelerationTime;
			_currentSpeed -= _decelerationRate * delta;

			// Clamp to ensure we don't go below the desired speed
			_currentSpeed = Mathf.Clamp(_currentSpeed, desiredSpeed, _currentSpeed);
		}
	}

	public void SetCurrentSpeed(float speed)
	{
		_currentSpeed = speed;
	}

	public float GetCurrentSpeed()
	{
		return _currentSpeed;
	}

	public Vector2 GetInputDirection()
	{
		return Input.GetVector("left", "right", "forward", "backward");
	}

	/// <summary>
	/// Function that gets the filtered input for forward, forward left and forward right.
	/// If anything other than forward, forward left and forward right, it will default to forward.
	/// </summary>
	public Vector2 GetFilteredInputDirection()
	{
		// Get the input direction
		Vector2 inputDirection = Input.GetVector("left", "right", "forward", "backward");

		// Normalize the input direction
		inputDirection = inputDirection.Normalized();

		// Calculate the angle of the input direction in radians
		float angle = Mathf.Atan2(-inputDirection.X, -inputDirection.Y);

		// Normalize the angle to be between -π and π
		angle = Mathf.Wrap(angle, -Mathf.Pi, Mathf.Pi);

		float left = -Mathf.DegToRad(100);
		float right = Mathf.DegToRad(100);

		if (angle >= left && angle <= right)
		{
			return inputDirection;
		}

		// Define the allowed angle range for forward directions
		float forwardStart = Mathf.DegToRad(130); // -50 degrees (forward-left)
		float forwardEnd = Mathf.DegToRad(180);   // +50 degrees (forward-right)

		// Check if the angle falls within the allowed range
		if (Mathf.Abs(angle) >= forwardStart && Mathf.Abs(angle) <= forwardEnd)
		{
			// Return the filtered direction
			return -inputDirection;
		}

		// If the input is outside the allowed range, return the forward direction
		return Vector2.Up;
	}

	/// <summary>
	/// Method to get the un-lerped player direction
	/// </summary>
	public Vector3 GetStableDirection()
	{
		return _stableDirection;
	}

	/// <summary>
	/// Method to get the lerped player direction
	/// </summary>
	public Vector3 GetPlayerDirection()
	{
		return _playerDirection.Normalized();
	}

	/// <summary>
	/// Method to set the lerped player direction
	/// </summary>
	public void SetPlayerDirection(Vector3 direction)
	{
		_playerDirection = direction;
	}

	/// <summary>
	/// Method to check if player is mainly facing forward by input
	/// </summary>
	/// <param name="angle">Angle in degrees that determines how much is needed to consider it forward</param>
	public bool IsPlayerMainlyForward(float angle)
	{
		Vector2 inputDir = GetInputDirection();
        Vector3 inputDirVec3 = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        // Check if input is mainly forward (within 45 degrees of forward axis)
        float angleToForward = Vector3.Forward.AngleTo(inputDirVec3);
        bool isMainlyForward = angleToForward <= Mathf.DegToRad(angle);

		return isMainlyForward;
	}

	/// <summary>
	/// Set the standing collider state
	/// </summary>
	/// <param name="state">Boolean to change the collision state. True = Active, False = Inactive</param>
	public void SetColliderState(bool state)
	{
		_collider.Disabled = !state;
	}
}
