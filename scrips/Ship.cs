using Godot;
using System;

public partial class Ship : CharacterBody3D
{
	public const float Speed = 15.0f;
	public const float JumpVelocity = 4.5f;

	private Transform3D _rotXform = Transform3D.Identity;

	[Export] private InteractableJoystick _throttle;
	[Export] private InteractableJoystick _joystick;


	public override void _Ready()
	{
		_rotXform = GlobalTransform; 
	}


	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		Vector3 direction = -GlobalBasis.Z * (1.0f - _throttle.XRatio);
		float throttle = _throttle.XRatio; 
		velocity = direction * Speed;

		
		Velocity = Velocity.Lerp(velocity, (float)delta);
	

		MoveAndSlide();

		HandleRotation((float)delta);
	}

	private void HandleRotation(float delta)
	{
		_rotXform = _rotXform.RotatedLocal(Vector3.Right, _joystick.X * delta * 2);
		_rotXform = _rotXform.RotatedLocal(Vector3.Forward, -_joystick.Z * delta * 2); 
		
		GlobalBasis = GlobalBasis.Slerp(_rotXform.Basis.Orthonormalized(), delta * 5).Orthonormalized(); 
	}
}
