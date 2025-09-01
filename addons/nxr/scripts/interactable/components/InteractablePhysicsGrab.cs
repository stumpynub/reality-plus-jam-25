using System.Security.Cryptography;
using Godot;
using NXR;
using NXRInteractable;


[GlobalClass]
public partial class InteractablePhysicsGrab : Node
{

	#region Exported
	[Export] private bool _percise = false;
	[Export] private bool _parentToInteractor = false;


	[ExportGroup("SecondaryGrabBehavior")]
	[Export] private bool _twoHanded = false;
	[Export] private LookUpVector _lookUpVector = LookUpVector.PrimaryGrab;
	[Export] private bool invert = false;


	[ExportGroup("Physics Grab Settings")]
	private float _initLinearDamp = 0.0f;
	private float _initAngularDamp = 0.0f;
	#endregion


	[ExportGroup("Ease Settings")]
	[Export] private float _rotationEaseTime = 0.5f;
	[Export] private float _positionEaseTime = 0.25f;



	#region Public     
	public Interactable Interactable { get; set; }
	#endregion


	#region Private  
	private Interactor _distanceGrabber;
	private Vector3 _perciseOffset = new();
	private Vector3 lVelocity = Vector3.Zero;
	private Vector3 aVelocity = Vector3.Zero;
	private bool _initFreezeState = false;
	private float _positionEase = 0.0f;
	private float _distanceEase = 0.0f;
	private float _rotationEase = 0.0f;
	private float _secondaryRotationEase = 0.0f;
	private Tween _grabTween;
	private Tween _secondaryRotTween;
	private Tween _posTween;
	private Vector3 _secondaryGrabPointOffset;
	#endregion


	public override void _Ready()
	{
		if (Util.GetParentOrOwnerOfType<Interactable>(this) != null)
		{
			Interactable = (Interactable)GetParent();
			Interactable.OnGrabbed += OnGrab;
			Interactable.OnFullDropped += OnFullDrop;
			Interactable.OnDropped += OnDrop;
			Interactable.OnDropped += CleanupEase;
			_initLinearDamp = Interactable.LinearDamp;
			_initAngularDamp = Interactable.AngularDamp;
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		Interactable.ForceUpdateTransform();
		Grab(delta);
		CalculateSecondaryOffset();
	}


	public void OnGrab(Interactable interactable, Interactor interactor)
	{
		HandleEase(interactor);

		if (interactor == Interactable.PrimaryGrab.Interactor)
		{

			if (_parentToInteractor)
			{
				Interactable.Reparent(interactor, true);
				Interactable.PreviousParent = (Node3D)interactor;
			}
		}
	}


	public void OnDrop(Interactable interactable, Interactor interactor)
	{
		_perciseOffset = Vector3.Zero;
	}


	public void OnFullDrop()
	{
		Interactable prev = Interactable; 

		if (_parentToInteractor && Util.NodeIs(Interactable.GetParent(), typeof(Interactor)))
		{
			Interactable.Reparent(Interactable.InitState.Parent, true);
		}

		prev.Freeze = prev.InitState.FreezeMode;
		//prev.LinearVelocity = interactor.Controller.GetGlobalVelocity();
		//prev.AngularVelocity = interactor.Controller.GetAngularVelocity();
	}


	private void Grab(double delta)
	{
		if (Interactable.PrimaryGrab.Interactor != null)
		{
			Interactor primary = Interactable.PrimaryGrab.Interactor;
			Quaternion currentRotation = Interactable.GlobalTransform.Basis.GetRotationQuaternion();
			Quaternion previousRotation = GetGrabXform(primary).Basis.GetRotationQuaternion();
			Quaternion rotationChange = currentRotation * previousRotation.Inverse();

			Vector3 prevVel = Vector3.Zero;


			lVelocity = (GetGrabXform(primary).Origin - Interactable.GlobalPosition);
			aVelocity = rotationChange.Inverse().GetEuler();

			if (_percise)
			{
				lVelocity = Interactable.GetPrimaryRelativeXform().Origin - Interactable.GlobalPosition;
			}


			if (Interactable.IsTwoHanded())
			{
				Quaternion current = GetTwoHandXform().Basis.GetRotationQuaternion();
				Quaternion prev = Interactable.GlobalTransform.Basis.GetRotationQuaternion();
				Quaternion change = current * prev.Inverse();
				aVelocity = change.GetEuler();
			}


			lVelocity /= (float)delta * Interactable.Mass;
			aVelocity /= (float)delta * Interactable.Mass;

			Interactable.LinearVelocity = lVelocity;
			Interactable.AngularVelocity = aVelocity;
		}
	}


	private void CalculateSecondaryOffset()
	{
		if (Interactable.SecondaryGrab.Interactor != null)
		{
			Transform3D grabPointXform = Interactable.SecondaryGrabPoint.GlobalTransform;

			Vector3 controllerOffset = Interactable.SecondaryGrab.Interactor.GlobalTransform.Origin - GetGrabXform(Interactable.PrimaryGrab.Interactor).Origin;
			Vector3 offset = (grabPointXform.Origin - Interactable.GlobalTransform.Origin);
			float dot = controllerOffset.Normalized().Dot(Interactable.GlobalBasis.Z);
			_secondaryGrabPointOffset = offset.Reflect(Interactable.GlobalTransform.Basis.Z.Normalized()) * Mathf.Abs(dot); // Allows for off-axis grab-points 
		}
	}


	private void HandleEase(Interactor interactor)
	{
		_grabTween?.Kill();

		if (interactor == Interactable.PrimaryGrab.Interactor)
		{
			_grabTween = GetTree().CreateTween();
			_grabTween.SetParallel(true);
			_rotationEase = 0.0f;
			_grabTween.TweenProperty(this, "_rotationEase", 1.0, _rotationEaseTime);
			_grabTween.TweenProperty(this, "_positionEase", 1.0, _positionEaseTime);
		}
		else
		{
			_grabTween.SetParallel(true);
			_grabTween = GetTree().CreateTween();
			_secondaryRotationEase = 0.0f;
			_grabTween.TweenProperty(this, "_secondaryRotationEase", 1, _rotationEaseTime);
		}
	}


	private void CleanupEase(Interactable interactable, Interactor interactor)
	{
		_grabTween?.Kill();

		if (interactor == Interactable.SecondaryGrab.Interactor)
		{
			_grabTween.SetParallel(true);
			_grabTween = GetTree().CreateTween();
			_rotationEase = 0.0f;
			_grabTween.TweenProperty(this, "_rotationEase", 1.0, _rotationEaseTime);
		}

		if (interactor == Interactable.PrimaryGrab.Interactor)
		{
			_positionEase = 0.0f;
		}
	}


	private Transform3D GetGrabXform(Interactor interactor)
	{

		if (interactor == null) return Transform3D.Identity;

		if(_percise) { 
			return Interactable.GetPrimaryRelativeXform(); 
		}

		Transform3D xform = new();
		Node3D grabPoint = Interactable.PrimaryGrabPoint;
		Vector3 posOffset = Interactable.GlobalPosition - Interactable.PrimaryGrabPoint.GlobalPosition;
		Vector3 newPos = interactor.GlobalPosition + posOffset;
		Basis rotOffset = (Interactable.GlobalTransform.Basis.Inverse() * grabPoint.GlobalTransform.Basis).Orthonormalized();

		xform.Origin = Interactable.GlobalTransform.Origin.Lerp(newPos, _positionEase);
		xform.Basis = Interactable.GlobalTransform.Basis.Orthonormalized().Slerp(
			(interactor.GlobalTransform.Basis * rotOffset.Inverse()) * Interactable.GetOffsetXform().Basis,
			_rotationEase
		).Orthonormalized();


		return xform;
	}


	public Transform3D GetTwoHandXform()
	{
		Interactor primary = Interactable.PrimaryGrab.Interactor;
		Transform3D secondaryXform = Interactable.SecondaryGrab.Interactor.GlobalTransform;
		Transform3D interactableXform = Interactable.GlobalTransform;
		Transform3D lookXform = GetGrabXform(primary);
		Vector3 up = Interactable.GlobalTransform.Basis.Y + GetUpVector();
		Vector3 lookDir = secondaryXform.Origin - GetGrabXform(primary).Origin + _secondaryGrabPointOffset;

		lookXform.Origin = GetGrabXform(primary).Origin;
		lookXform.Basis = interactableXform.Basis.Slerp(Basis.LookingAt(
			lookDir.Normalized(),
			up.Normalized()).Orthonormalized() * Interactable.GetOffsetXform().Basis,
			_secondaryRotationEase
		);


		return lookXform;
	}


	public Vector3 GetUpVector()
	{
		switch (_lookUpVector)
		{
			case LookUpVector.PrimaryGrab:
				return Interactable.PrimaryGrab.Interactor.GlobalTransform.Basis.Y;
			case LookUpVector.SecondaryGrab:
				return Interactable.SecondaryGrab.Interactor.GlobalTransform.Basis.Y;
			case LookUpVector.Combined:
				return (Interactable.PrimaryGrab.Interactor.GlobalTransform.Basis.Y + Interactable.SecondaryGrab.Interactor.GlobalTransform.Basis.Y).Normalized();
		}

		return Vector3.Up;
	}
}


