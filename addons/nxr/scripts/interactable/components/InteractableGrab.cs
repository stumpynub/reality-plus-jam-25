using System.Security.Cryptography;
using Godot;
using NXR;
using NXRInteractable;


[GlobalClass]
public partial class InteractableGrab : Node
{

	public enum LookUpVector
	{
		PrimaryGrab,
		LookProxy,
		Combined,
	}


	public enum RotationPivotMode
	{
		PrimaryGrabPoint,
		SecondaryGrabPoint,
		Interactable,
		Custom
	}


	#region Exported
	[Export] private bool _primaryGrabEnabled = true;
	[Export] private bool _percise = false;
	[Export] private bool _updatePostion = true;
	[Export] private bool _updateRotation = true;


	[ExportGroup("SecondaryGrabBehavior")]
	[Export] private bool _twoHanded = false;
	[Export(PropertyHint.Range, "0.0, 1.0")] private float _secondaryInfuence = 1.0f;

	[ExportSubgroup("Pivot Settings")]
	[Export] public RotationPivotMode PivotMode = RotationPivotMode.PrimaryGrabPoint;
	[Export] Node3D _customPivot = null;

	[ExportSubgroup("Look Settings")]
	[Export] private LookUpVector _lookUpVector = LookUpVector.PrimaryGrab;
	[Export] private Vector3 UpHint = Vector3.Zero;
	[Export] private Vector3 ForwardHint = Vector3.Zero;
	#endregion


	[ExportGroup("Ease Settings")]
	[Export] private Tween.EaseType _rotationInEase = Tween.EaseType.In;
	[Export] private float _rotationInEaseTime = 0.5f;
	[Export] private float _secondaryRotationInEaseTime = 0.5f;

	[Export] private Tween.EaseType _rotationOutEase = Tween.EaseType.In;
	[Export] private float _rotationOutEaseTime = 1.0f;


	[Export] private Tween.EaseType _positionInEase = Tween.EaseType.In;
	[Export] private float _positionInEaseTime = 0.25f;



	#region Public     
	public Node3D LookProxy { get; set; }
	public Interactable Interactable { get; set; }
	#endregion


	#region Private  
	private Vector3 _throwLinearVelocity = Vector3.Zero;
	private Vector3 _throwAngularVelocity = Vector3.Zero;
	private Transform3D _lookRelativeXform = new();
	private Interactor _distanceGrabber;
	private Vector3 _perciseOffset = new();
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
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		HandleGrab((float)delta);
	}


	public void StartLook(Node3D node)
	{
		if (LookProxy != null) return;
		_lookRelativeXform = node.GlobalTransform.AffineInverse() * Interactable.GlobalTransform;

		LookProxy = node;
		LookEase();
	}


	public void StopLook()
	{
		LookProxy = null;
		LookCleanup();
	}


	public void OnFullDrop()
	{
		Interactable.LinearVelocity = _throwLinearVelocity;
		Interactable.AngularVelocity = _throwAngularVelocity;
	}


	private void PrimaryCleanup()
	{
		if (!Interactable.IsGrabbed())
			_positionEase = 0.0f;
	}


	private void HandleGrab(float delta)
	{
		if (IsInstanceValid(Interactable.PrimaryGrab.Interactor))
		{
			Interactable.GlobalTransform = GetFinalXform().Orthonormalized();
			Interactable.GlobalTransform *= GetOffsetXform().Orthonormalized();
			Interactable.ForceUpdateTransform();
		}

		if (IsInstanceValid(Interactable.SecondaryGrab.Interactor) && !IsInstanceValid(Interactable.PrimaryGrab.Interactor))
		{
			Interactable.GlobalTransform = Interactable.GetSecondaryRelativeXform();
			_rotationEase = 1.0f;
			_positionEase = 1.0f;
		}
	}

	private void CalculateSecondaryOffset()
	{
		if (LookProxy != null)
		{
			Transform3D grabPointXform = Interactable.SecondaryGrabPoint.GlobalTransform;

			Vector3 controllerOffset = LookProxy.GlobalPosition - GetGrabXform().Origin;
			Vector3 offset = grabPointXform.Origin - Interactable.GlobalTransform.Origin;
			float dot = controllerOffset.Normalized().Dot(Interactable.GlobalBasis.Z);
			_secondaryGrabPointOffset = offset.Reflect(Interactable.GlobalTransform.Basis.Z.Normalized()) * Mathf.Abs(dot); // Allows for off-axis grab-points 
		}
	}


	public virtual Transform3D GetGrabXform()
	{
		if (!_primaryGrabEnabled)
			return Interactable.GlobalTransform;


		if (Interactable.PrimaryGrab.Interactor == null)
			return Transform3D.Identity;


		Interactor interactor = Interactable.PrimaryGrab.Interactor;
		if (Interactable.PrimaryGrabPoint != Interactable)
		{
			Transform3D grabPointLocal = Interactable.PrimaryGrabPoint.Transform;
			Transform3D grabXform = interactor.GlobalTransform * grabPointLocal.AffineInverse();
			Basis basis = Interactable.GlobalBasis.Slerp(grabXform.Basis, _rotationEase);
			Vector3 pos = Interactable.GlobalPosition.Lerp(grabXform.Origin, _positionEase);

			if (_percise)
			{
				pos = Interactable.GetPrimaryRelativeXform().Origin;
				basis = Interactable.GetPrimaryRelativeXform().Basis;
			}


			if (!_updatePostion)
				pos = Interactable.GlobalTransform.Origin;
			if (!_updateRotation)
				basis = Interactable.GlobalTransform.Basis;


			return new Transform3D(basis, pos);
		}
		else
		{
			return interactor.GlobalTransform;
		}
	}


	public virtual Transform3D GetFinalXform()
	{
		if (!_twoHanded || LookProxy == null)
			return GetGrabXform();

		Transform3D primaryHand = GetGrabXform();
		Transform3D secondaryHand = LookProxy.GlobalTransform;


		// Configurable forward and up hints
		Vector3 forwardHint = ForwardHint != Vector3.Zero
			? ForwardHint.Normalized()
			: (-Interactable.ToLocal(Interactable.SecondaryGrabPoint.GlobalPosition)).Normalized();

		Vector3 upHint = GetUpVector();


		// Compute local and world bases
		Basis localBasis = ComputeLocalFirearmBasis(forwardHint, upHint);
		Basis worldBasis = ComputeTargetGlobalBasis(primaryHand.Origin, secondaryHand.Origin, upHint);

		// Compute rotation delta
		Basis rotationDelta = worldBasis * localBasis;


		Transform3D firearmTransform = new Transform3D(rotationDelta, primaryHand.Origin);

		// Optional: preserve original rotation/position if desired
		if (!_updatePostion)
			firearmTransform.Origin = Interactable.GlobalTransform.Origin;
		if (!_updateRotation)
			firearmTransform.Basis = Interactable.GlobalTransform.Basis;

		// Blend
		return GetGrabXform().InterpolateWith(firearmTransform, _secondaryInfuence * _secondaryRotationEase);
	}


	private Basis ComputeLocalFirearmBasis(Vector3 forwardHint, Vector3 upHint)
	{
		Vector3 p = Interactable.PrimaryGrabPoint.Position;
		Vector3 s = Interactable.SecondaryGrabPoint.Position;
		Vector3 reflect = -Vector3.Forward.Reflect(forwardHint); 

		Vector3 forward = forwardHint;
		Vector3 control = (p - s).Normalized();

		return Util.CreateOrthonormalBasis(-forwardHint.Reflect(Vector3.Forward), forwardHint.Reflect(-Vector3.Forward), -Vector3.Up);
	}


	private Basis ComputeTargetGlobalBasis(Vector3 primary, Vector3 secondary, Vector3 upHint)
	{
		Vector3 forward = ((primary - secondary)).Normalized();

		return Util.CreateOrthonormalBasis(-forward, forward, -upHint);
	}


	public Transform3D GetOffsetXform()
	{
		Vector3 rotOffset = (Interactable.RotationOffset * (Vector3.One * (Mathf.Pi / 180))) * _rotationEase;
		Transform3D offsetTransform = Interactable.GlobalTransform * Interactable.GlobalTransform.AffineInverse();
		offsetTransform = offsetTransform.TranslatedLocal(Interactable.PositionOffset * _positionEase);
		offsetTransform.Basis *= Basis.FromEuler(rotOffset);
		return offsetTransform.Orthonormalized();
	}


	public Vector3 GetUpVector()
	{
		return _lookUpVector switch
		{
			LookUpVector.PrimaryGrab => Interactable.PrimaryGrab.Interactor.GlobalTransform.Basis.Y,
			LookUpVector.LookProxy => LookProxy.GlobalTransform.Basis.Y,
			LookUpVector.Combined => (Interactable.PrimaryGrab.Interactor.GlobalTransform.Basis.Y + LookProxy.GlobalTransform.Basis.Y).Normalized(),
			_ => Vector3.Up,
		};
	}

	private Vector3 GetLocalPivotPoint()
	{
		switch (PivotMode)
		{
			case RotationPivotMode.PrimaryGrabPoint:
				return Interactable.PrimaryGrabPoint.Position;

			case RotationPivotMode.SecondaryGrabPoint:
				return Interactable.SecondaryGrabPoint.Position;

			case RotationPivotMode.Interactable:
				return Interactable.Position;

			case RotationPivotMode.Custom:
				if (_customPivot != null && Interactable.IsAncestorOf(_customPivot))
					return Interactable.ToLocal(_customPivot.GlobalPosition);
				break;
		}

		// Default fallback
		return Interactable.PrimaryGrabPoint.Position;
	}

	#region Event Handlers

	public void OnGrab(Interactable interactable, Interactor interactor)
	{

		if (interactor == Interactable.PrimaryGrab.Interactor)
		{
			PrimaryEase();

			Interactable.LinearVelocity = Vector3.Zero;
		}
		else
		{
			StartLook(interactor);
		}
	}
	#endregion


	public void OnDrop(Interactable interactable, Interactor interactor)
	{
		if (interactor == interactable.PrimaryGrab.Interactor)
		{
			PrimaryCleanup();

		}
		else
		{
			if (interactor == LookProxy) StopLook();
		}


		if (interactor is XRControllerInteractor xrInteractor)
		{
			_throwLinearVelocity = xrInteractor.Controller.GetGlobalVelocity();
			_throwAngularVelocity = xrInteractor.Controller.GetAngularVelocity();
		}

		_perciseOffset = Vector3.Zero;
	}


	#region Easings
	private void PrimaryEase()
	{
		_grabTween = GetTree().CreateTween();
		_grabTween.SetParallel(true);
		_rotationEase = 0.0f;
		_grabTween.TweenProperty(this, "_rotationEase", 1.0, _rotationInEaseTime).SetEase(_rotationInEase);
		_grabTween.TweenProperty(this, "_positionEase", 1.0, _positionInEaseTime).SetEase(_positionInEase);
	}

	private void LookEase()
	{
		_grabTween?.Kill();
		_grabTween?.SetParallel(true);
		_grabTween = GetTree().CreateTween();
		_secondaryRotationEase = 0.0f;
		_grabTween.TweenProperty(this, "_secondaryRotationEase", 1, _secondaryRotationInEaseTime).SetEase(_rotationInEase);
	}

	private void LookCleanup()
	{
		_grabTween?.Kill();
		_grabTween.SetParallel(true);
		_grabTween = GetTree().CreateTween();
		_rotationEase = 0.0f;
		_grabTween.TweenProperty(this, "_rotationEase", 1.0, _rotationOutEaseTime).SetEase(_rotationOutEase);
	}

	#endregion
}


