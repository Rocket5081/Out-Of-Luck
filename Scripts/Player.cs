using Godot;
using System;

public partial class Player : CharacterBody3D, IDamageable
{
	public const float Speed = 20.0f;
	[Export] public float Acceleration {get; set;} = 20.0f;
	public const float JumpVelocity = 8.5f;
	[Export] public float rotationSpeed {get; set;} = 12.0f;
	[Export] public float MaxHealth = 100f;
	public float Health;

	[Export (PropertyHint.Range, "0,1")] public float MouseSensitivity {get; set;} = 0.25f;
	public float TiltUpperLimit {get; set;} = Mathf.Pi / 3.0f;
	public float TiltLowerLimit {get; set;} = -Mathf.Pi / 8.0f;

	[Export] public Node3D cameraPivot;
	[Export] public Camera3D camera;

	private Vector2 _cameraInputDirection = Vector2.Zero;
	private Vector3 _lastMovementDirection = Vector3.Back;

	[Export] public Node3D mesh;

	public override void _Ready(){
		Health = MaxHealth;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else if (@event.IsActionPressed("attack"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion &&
			Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_cameraInputDirection = motion.ScreenRelative * MouseSensitivity;
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		//Camera
		Vector3 pivotRot = cameraPivot.Rotation;
		pivotRot.X -= _cameraInputDirection.Y * (float)delta;
		pivotRot.X = Mathf.Clamp(pivotRot.X, TiltLowerLimit, TiltUpperLimit);
		pivotRot.Y -= _cameraInputDirection.X * (float)delta;
		cameraPivot.Rotation = pivotRot;

		_cameraInputDirection = Vector2.Zero;

		//Movement
		Vector2 rawInput = Input.GetVector("left", "right", "forward", "back");
		Vector3 forward = camera.GlobalBasis.Z;
		Vector3 right = camera.GlobalBasis.X;
		Vector3 moveDirection = forward * rawInput.Y + right * rawInput.X;
		moveDirection.Y = 0.0f;
		moveDirection = moveDirection.Normalized();

		Vector3 velocity = Velocity;
		float yVelocity = velocity.Y;
		velocity.Y = 0.0f;
		velocity = velocity.MoveToward(moveDirection * Speed, Acceleration * (float)delta);
		velocity.Y = yVelocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Velocity = velocity;
		MoveAndSlide();

		if (moveDirection.Length() > 0.2f)
		{
			_lastMovementDirection = moveDirection;
		}
		float targetAngle = Vector3.Forward.SignedAngleTo(_lastMovementDirection, Vector3.Up);
		Vector3 meshRot = mesh.GlobalRotation;
		meshRot.Y = Mathf.LerpAngle(mesh.Rotation.Y, targetAngle, rotationSpeed * (float)delta);
		mesh.GlobalRotation = meshRot;
	}

	public virtual void TakeDamage(float damage){
		if(Health-damage <= 0f){
			GD.Print("Your dead gang");
		}
		Health -= damage;
	}
}
