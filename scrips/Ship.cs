using Godot;
using NXR;
using NXRInteractable;
using System;

public partial class Ship : CharacterBody3D
{
	public const float Speed = 15.0f;
	public const float JumpVelocity = 4.5f;

	private Transform3D _rotXform = Transform3D.Identity;

	[Export] private InteractableJoystick _throttle;
	[Export] private InteractableJoystick _joystick;
	[Export] private ShapeCast3D _targetingShape;
	[Export] private Sprite3D _targetingSprite;
	[Export] private Line3D _rayLine; 

	private IShipTargetable _currentTarget;

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
		if (GetSlideCollisionCount() > 0)
		{
			GD.Print("Collided with " + GetSlideCollision(0).GetCollider());
		}


		if (GetJoystickInteractor() != null && GetJoystickInteractor().Controller.IsButtonPressed("trigger"))
		{
			Node3D target = _currentTarget as Node3D;
			_rayLine.UpdatePointPosition(1, _rayLine.ToLocal(target.GlobalPosition)); 
			GetJoystickInteractor().Controller.Pulse(0.5, 0.5, 0.1f);
		}
		else
		{
			_rayLine.UpdatePointPosition(1, Vector3.Zero); 
			_rayLine.Rebuild(); 
		}
	}


	public override void _Process(double delta)
	{
		if (_targetingShape.IsColliding())
		{
			Node3D collider = (Node3D)_targetingShape.GetCollider(0);
			if (collider is IShipTargetable)
			{
				_targetingSprite.Visible = true;
				_targetingSprite.GlobalPosition = _targetingShape.GetCollisionPoint(0);
				_currentTarget = collider as IShipTargetable;
			}

		}
		else
		{
			_targetingSprite.Visible = false;
			_currentTarget = null;
		}
	}

	private void HandleRotation(float delta)
	{
		_rotXform = _rotXform.RotatedLocal(Vector3.Right, _joystick.X * delta * 2);
		_rotXform = _rotXform.RotatedLocal(Vector3.Up, _joystick.Z * delta * 2);

		if (_joystick.PrimaryGrab.Interactor != null)
		{
			if (_joystick.PrimaryGrab.Interactor is XRControllerInteractor interactor)
			{
				_rotXform = _rotXform.RotatedLocal(Vector3.Forward, interactor.Controller.GetVector2("primary").X * delta * 2);
			}
		}

		GlobalBasis = GlobalBasis.Slerp(_rotXform.Basis.Orthonormalized(), delta * 5).Orthonormalized();
	}
	

	private XRControllerInteractor GetJoystickInteractor()
	{
		if (_joystick.PrimaryGrab.Interactor != null)
		{
			if (_joystick.PrimaryGrab.Interactor is XRControllerInteractor interactor)
			{
				return interactor;
			}
		}

		return null;
	}
}
