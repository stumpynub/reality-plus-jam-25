using Godot;
using NXR;
using NXRInteractable;
using System;

public partial class Ship : CharacterBody3D
{
	public const float Speed = 200.0f;
	public const float JumpVelocity = 4.5f;

	private Transform3D _rotXform = Transform3D.Identity;

	[Export] private InteractableJoystick _throttle;
	[Export] private InteractableJoystick _joystick;
	[Export] private ShapeCast3D _targetingShape;
	[Export] private Sprite3D _targetingSprite;
	[Export] private Line3D _rayLine;
	[Export] private Controller _leftController;
	[Export] private Controller _rightController;
	[Export] public bool ShipEnabled = true;


	private IShipTargetable _currentTarget;

	public override void _Ready()
	{
		_rotXform = GlobalTransform;
		_leftController.ButtonPressed += LeftButtonPressed;
		_rightController.ButtonPressed += RightButtonPressed;
		CallDeferred("Recenter");

	}


	public override void _PhysicsProcess(double delta)
	{
		HandleMovement((float)delta);
		HandleRotation((float)delta);

	}


	public override void _Process(double delta)
	{
		HandleTargeting();

	}


	public void Recenter()
	{
		XRServer.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, false);
	}


	private void HandleMovement(float delta)
	{
		if (!ShipEnabled) return;

		Vector3 velocity = Velocity;
		Vector3 direction = -GlobalBasis.Z * (1.0f - _throttle.XRatio);
		float throttle = _throttle.XRatio;
		velocity = direction * Speed;



		if (GetSlideCollisionCount() > 0)
		{
			KinematicCollision3D coll = GetSlideCollision(0);
			velocity = velocity.Lerp(Vector3.Zero, 0.8f); 
			
		}

		Velocity = Velocity.Lerp(velocity, (float)delta * 2.0f);

		MoveAndSlide();
	}
	

	private void HandleTargeting()
	{


		float scale = 1.0f;
		scale = _targetingSprite.GlobalPosition.DistanceTo(GlobalPosition);
		scale = Mathf.Clamp(scale, 1.0f, 100.0f);
		_targetingSprite.Scale = Vector3.One;

		if (_targetingShape.IsColliding())
		{
			Node3D collider = (Node3D)_targetingShape.GetCollider(0);
			if (collider is IShipTargetable)
			{
				_targetingSprite.Visible = true;
				_targetingSprite.GlobalPosition = collider.GlobalPosition;
				_currentTarget = collider as IShipTargetable;
			}

		}

		else
		{
			_targetingSprite.Visible = false;
			_currentTarget = null;
		}


		if (GetJoystickInteractor() != null && GetJoystickInteractor().Controller.IsButtonPressed("trigger") && _currentTarget != null)
		{
			Node3D target = _currentTarget as Node3D;
			_rayLine.UpdatePointPosition(1, _rayLine.ToLocal(target.GlobalPosition));
			GetJoystickInteractor().Controller.Pulse(0.6, 1, 0.1f);
			_currentTarget.Damage(1);
		}
		else
		{
			_rayLine.UpdatePointPosition(1, Vector3.Zero);
			_rayLine.Rebuild();
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

	private void LeftButtonPressed(string button)
	{
		if (button == "primary_click")
		{
			Recenter();
		}
	}

	private void RightButtonPressed(string button)
	{
		if (button == "primary_click")
		{
			Recenter();
		}
	}
}
