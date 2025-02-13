using Godot;
using System;
using System.Collections.Generic;
using MEC;

public partial class Movement : CharacterBody3D, IMovement
{
	// Finite state machine
	public GodotParadiseFiniteStateMachine FSM;
	[Export] public AnimationTree AnimationPlayer;

	// grabbables
	private Node3D _body;
	private Node3D _neck;

	// references
	private CollisionShape3D _collider;

	// current speeds
	private float _currentSpeed;
	private float _momentum;

	// directions
	private Vector3 _playerDirection; // Final direction calculated from stable direction
	private Vector3 _stableDirection; // Direction that is absolute after no input has been given
	private Vector3 _previousDirection; // Previous direction from the last frame

	// velocities
	private Vector3 _localVelocity;
	private Vector3 _lastVelocity;

	// position
	private Vector3 _lastPhysicsPos;

	// Delete later
	[Export] private Node3D _resetPosition;

	[ExportCategory("Movement")]
	[Export] public float maxSpeed {private set; get;} // metres per second
	private float _accelerationRate;
	private float _decelerationRate;

	// Value that is used to determine how long it takes to change the player direction
	private float _dirChangeTime = 0.3f;
	private Vector3 _delayedDirection; // Direction where the vector is scaled according to the change in direction time
	private float _directionBlendFactor = 0.0f;

	private float _directionControl = 1f;

	// Value that by default is set to default gravity but can be changed.
	public float gravity {set; get;} = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	[ExportCategory("Input")]
	[Export(PropertyHint.Range, "0.01, 10,")] private float _inputReponseTime;
	private float _currentInputFactor;
	private Vector2 _currentInput = Vector2.Zero;

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
	}

	public override void _Process(double delta)
	{
		if (_physicsInterpolate)
		{
			PhysicsInterpolation();
		}

		if (Input.IsKeyPressed(Key.R) && _resetPosition != null)
			GlobalPosition = _resetPosition.GlobalPosition;

		_currentInput = GetSmoothInputDirection();

		HandleLabels();
	}

	public override void _PhysicsProcess(double delta)
	{
		_localVelocity = Velocity;

		ChangeDirectionWithInput();
		ApplyDelayedDirection();

		DebugDraw3D.DrawArrow(Position, Position + (_playerDirection * 2f), Colors.Red, 0.2f);
		DebugDraw3D.DrawArrow(Position, Position + (_delayedDirection * 1f), Colors.Blue, 0.2f);
		
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

		AnimationNodeStateMachinePlayback node = (AnimationNodeStateMachinePlayback)AnimationPlayer.Get("parameters/Master/playback");
		_animationLabel.Text = "ANIMATION: " + node.GetCurrentNode();
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
	/// Acceleration Method
	/// </summary>
	public void Accelerate(float delta, float desiredSpeed, float accelerationTime)
	{
		desiredSpeed *= GetDelayedDirection().Length();
		
		_accelerationRate = desiredSpeed / accelerationTime;
		_currentSpeed += _accelerationRate * delta;

		// Clamp to ensure we don't overshoot the desired speed
		_currentSpeed = Mathf.Clamp(_currentSpeed, 0, desiredSpeed);
	}
	
	/// <summary>
	/// Decceleration method
	/// </summary>
	public void Deccelerate(float delta, float desiredSpeed, float decelerationTime)
	{
		// Deceleration logic
		_decelerationRate = desiredSpeed / decelerationTime;
		_currentSpeed -= _decelerationRate * delta;

		// Clamp to ensure we don't go below the desired speed
		_currentSpeed = Mathf.Clamp(_currentSpeed, 0f, 100f);
	}

	public void SetCurrentSpeed(float speed)
	{
		_currentSpeed = speed;
	}

	public float GetCurrentSpeed()
	{
		return _currentSpeed;
	}

	public Vector2 GetRawInputDirection()
	{
		return Input.GetVector("left", "right", "forward", "backward");
	}

	private Vector2 GetSmoothInputDirection()
	{
		Vector2 rawInput = GetRawInputDirection();
		Vector2 lerpedInput = Vector2.Zero;

		// Check if input vector is zero
		if (rawInput == Vector2.Zero)
		{
			// Reset responseTime
			_currentInputFactor = 0f;
		}
		else
		{
			// Calculate the lerp factor based on response time
			_currentInputFactor = Mathf.Min(_currentInputFactor + (float)GetProcessDeltaTime() / _inputReponseTime, 1.0f);
		}

		// Smoothly interpolate _currentInput towards rawInput
		lerpedInput = lerpedInput.Lerp(rawInput, _currentInputFactor);

		return lerpedInput;
	}

	public Vector2 GetCurrentSmoothInput()
	{
		return _currentInput;
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

	private void ChangeDirectionWithInput()
	{
		Vector2 inputDir = GetRawInputDirection();

		// Compute the desired direction based on input
		Vector3 desiredDirection = _neck.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

		// If input direction is equivalent to zero, keep the direction as is
		if (inputDir != Vector2.Zero)
		{
			_stableDirection = desiredDirection;
		}

		_playerDirection = _playerDirection.Lerp(_stableDirection, _directionControl);
	}

	private void ApplyDelayedDirection()
	{
		// If the direction changes, reset the blend value
        if (_previousDirection != _stableDirection)
        {
            _directionBlendFactor = 0.0f;
            _previousDirection = _stableDirection;
        }
		
		_directionBlendFactor = Mathf.Min(_directionBlendFactor + (float)GetPhysicsProcessDeltaTime() / _dirChangeTime, 1.0f);
		
        // Smoothly transition towards the player direction
        _delayedDirection = _delayedDirection.Lerp(_stableDirection, _directionBlendFactor);
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

	public Vector3 GetDelayedDirection()
	{
		return _delayedDirection;
	}

	public Vector2 GetXZVelocity()
	{
		return new Vector2(Velocity.X, Velocity.Z);
	}

	public float GetCurrentDirChangeTime()
	{
		return _dirChangeTime;
	}

	public void SetDirectionChangeTime(float time)
	{
		_dirChangeTime = time;
	}

	public void SetDirectionControl(float value)
	{
		_directionControl = value;
	}

	/// <summary>
	/// Method to check if player is mainly facing forward by input
	/// </summary>
	/// <param name="angle">Angle in degrees that determines how much is needed to consider it forward</param>
	public bool IsPlayerMainlyForward(float angle)
	{
		Vector2 inputDir = GetRawInputDirection();
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

	public static float CalculateT(float v, float k)
    {
        float t = v / k;  
        t = Math.Min(t, 1);
        return t;
    }
}
