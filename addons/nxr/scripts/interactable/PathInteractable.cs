using Godot;
using Godot.Collections;
using NXRInteractable;
using System;
using System.Security.Cryptography.X509Certificates;


namespace NXR;

[Tool]
[GlobalClass]
public partial class PathInteractable : Interactable
{
	[Export] public Node3D Target;
	[Export(PropertyHint.Range, "0.0, 1.0")]
	public float Progress
	{
		get
		{
			return _progress;
		}

		set
		{
			_progress = value;
			InterpolateTransforms(_progress);
		}
	}


	[Export] public Curve EasingCurve;  // Add this at the top of the class
	[Export] Array<Transform3D> Transforms = new();

	[ExportToolButton("Add Transform")] public Callable AddTransformButton => Callable.From(TAddTransform);
	[Export] bool _progressOnGrab = true;
	[Export(PropertyHint.Range, "0,1")]
	public float RotationWeight = 0.0f;  // 0 = only position, 1 = only rotation




	private float _progress = 0.0f;
	private bool _snapped = false;
	protected Tween _moveTween = null;
	private Vector3 _targetScale = Vector3.One;



	private float _startGrabRatio = 0.0f;
	private Vector3 _grabLocalStart = Vector3.Zero;
	private Node3D _cachedProgressNode;
	protected HapticTicker _hapticTicker = new HapticTicker();

	public override void _Ready()
	{
		base._Ready();

		OnGrabbed += _Grabbed;
		OnDropped += _Dropped;

		AddChild(_hapticTicker);
	}

	public override void _Process(double delta)
	{
		if (GetPrimaryController() != null)
		{
			_hapticTicker.Tick(GetPrimaryController(), Progress);
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		if (_progressOnGrab && PrimaryGrab.Interactor != null)
		{
			UpdateProgressFromNode();
		}

		Position = Position.Clamp(GetMinOrigin(), GetMaxOrigin());

		if (GetPrimaryController() != null)
		{
			_hapticTicker.Tick(GetPrimaryController(), Progress);
		}
	}

	public void RunTool()
	{

		if (Engine.IsEditorHint())
		{
			Target ??= this;

			if (Target == this)
			{
				_snapped = false;
			}

			if (Target != this)
			{

				if (GlobalPosition != Target.GlobalPosition && !_snapped)
				{
					GlobalPosition = Target.GlobalPosition;
					GlobalBasis = Target.GlobalBasis;

					_snapped = true;
				}
			}
		}

		Target.GlobalPosition = GlobalPosition;
		Target.GlobalRotation = GlobalRotation;
		Scale = Vector3.One;
	}


	public Transform3D GetStartXform()
	{
		if (Transforms.Count <= 0) return Transform;

		return Transforms[0];
	}

	public Transform3D GetEndXform()
	{
		if (Transforms.Count <= 0) return Transform;

		return Transforms[^1];
	}

	public Vector3 GetMinOrigin()
	{
		float x = GetStartXform().Origin.X < GetEndXform().Origin.X ? GetStartXform().Origin.X : GetEndXform().Origin.X;
		float y = GetStartXform().Origin.Y < GetEndXform().Origin.Y ? GetStartXform().Origin.Y : GetEndXform().Origin.Y;
		float z = GetStartXform().Origin.Z < GetEndXform().Origin.Z ? GetStartXform().Origin.Z : GetEndXform().Origin.Z;

		return new Vector3(x, y, z);
	}

	public Vector3 GetMaxOrigin()
	{
		float x = GetStartXform().Origin.X > GetEndXform().Origin.X ? GetStartXform().Origin.X : GetEndXform().Origin.X;
		float y = GetStartXform().Origin.Y > GetEndXform().Origin.Y ? GetStartXform().Origin.Y : GetEndXform().Origin.Y;
		float z = GetStartXform().Origin.Z > GetEndXform().Origin.Z ? GetStartXform().Origin.Z : GetEndXform().Origin.Z;

		return new Vector3(x, y, z);
	}

	public Vector3 GetMinRotation()
	{
		Vector3 startEuler = GetStartXform().Basis.GetEuler();
		Vector3 endEuler = GetEndXform().Basis.GetEuler();

		float x = startEuler.X < endEuler.X ? startEuler.X : endEuler.X;
		float y = startEuler.Y < endEuler.X ? startEuler.Y : endEuler.Y;
		float z = startEuler.Z < endEuler.Z ? startEuler.Z : endEuler.Z;
		return new Vector3(x, y, z);
	}

	public Vector3 GetMaxRotation()
	{
		Vector3 startEuler = GetStartXform().Basis.GetEuler();
		Vector3 endEuler = GetEndXform().Basis.GetEuler();

		float x = startEuler.X > endEuler.X ? startEuler.X : endEuler.X;
		float y = startEuler.Y > endEuler.X ? startEuler.Y : endEuler.Y;
		float z = startEuler.Z > endEuler.Z ? startEuler.Z : endEuler.Z;
		return new Vector3(x, y, z);
	}


	public bool AtStart()
	{
		return Mathf.IsEqualApprox(Progress, 0.0f);
	}

	public bool AtEnd()
	{
		return Mathf.IsEqualApprox(Progress, 1.0f);
	}

	public bool AtStartRot()
	{
		return GetStartXform().Basis.Orthonormalized().IsEqualApprox(Transform.Basis.Orthonormalized());
	}

	public bool AtEndRot()
	{
		return GetEndXform().Basis.Orthonormalized().IsEqualApprox(Transform.Basis.Orthonormalized());
	}

	public Tween GoToStart(float time = 0, Tween.EaseType ease = Tween.EaseType.InOut, Tween.TransitionType trans = Tween.TransitionType.Linear)
	{
		_moveTween?.Kill();
		_moveTween = this.CreateTween();
		_moveTween.TweenProperty(this, "Progress", 0.0f, time).SetEase(ease).SetTrans(trans);
		return _moveTween;
	}

	public Tween GoToEnd(float time = 0, Tween.EaseType ease = Tween.EaseType.InOut, Tween.TransitionType trans = Tween.TransitionType.Linear)
	{
		_moveTween?.Kill();
		_moveTween = this.CreateTween();
		_moveTween.TweenProperty(this, "Progress", 1.0f, time).SetEase(ease).SetTrans(trans);
		return _moveTween;
	}


	public void InterpolateTransforms(float t, bool reverse = false, bool loop = false)
	{
		if (Transforms.Count == 0)
		{
			// Fallback to GetStartXform() and GetEndXform()
			Transform3D from = reverse ? GetEndXform() : GetStartXform();
			Transform3D to = reverse ? GetStartXform() : GetEndXform();

			if (loop)
				t = t % 1f;
			else
				t = Mathf.Clamp(t, 0f, 1f);

			if (EasingCurve != null)
				t = EasingCurve.Sample(t);

			Transform = from.Orthonormalized().InterpolateWith(to.Orthonormalized(), t);
			return;
		}

		if (Transforms.Count == 1)
		{
			Transform = Transforms[0];
			return;
		}

		int segmentCount = Transforms.Count - 1;
		float segmentLength = 1f / segmentCount;

		if (loop)
			t = t % 1f;
		else
			t = Mathf.Clamp(t, 0f, 1f);

		if (EasingCurve != null)
			t = EasingCurve.Sample(t);

		if (reverse)
			t = 1f - t;

		int segmentIndex = Mathf.FloorToInt(t / segmentLength);
		segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);

		float segmentStartT = segmentIndex * segmentLength;
		float segmentEndT = (segmentIndex + 1) * segmentLength;
		float localT = (t - segmentStartT) / (segmentEndT - segmentStartT);

		Transform3D fromTransform = Transforms[segmentIndex].Orthonormalized();
		Transform3D toTransform = Transforms[segmentIndex + 1].Orthonormalized();

		if (reverse)
		{
			fromTransform = Transforms[segmentIndex + 1].Orthonormalized();
			toTransform = Transforms[segmentIndex].Orthonormalized();
		}

		Transform = fromTransform.InterpolateWith(toTransform, localT);
	}



	private void TAddTransform()
	{
		if (Transforms.Count >= 10) return; // Limit to 10 transforms

		Transforms.Add(Transform);
	}


	private void _Grabbed(Interactable interactable, Interactor interactor)
	{
		GD.Print(Progress);
		StartNodeProgress(interactor);
	}


	public void StartNodeProgress(Node3D node, float from = -1f, float animateTime = 0.0f)
	{
		if (from != -1)
		{
			_startGrabRatio = from;
		}
		else
		{
			_startGrabRatio = Progress; // Use current progress as the start ratio
		}

		Node3D parent = (Node3D)GetParent();
		_grabLocalStart = parent.ToLocal(node.GlobalPosition);

		_cachedProgressNode = node;
	}


	public void StopNodeProgress(bool resetStartRatio = true)
	{
		if (resetStartRatio)
			_startGrabRatio = 0.0f;

		_cachedProgressNode = null;
	}


	private void _Dropped(Interactable interactable, Interactor interactor)

	{

	}


	/// <summary>
	/// Updates the progress and animates based off the ratio of the node between the start and end transform
	/// Used in conjuction with Start StartNodeProgress 
	/// </summary>
	/// <param name="node"></param>
	public void UpdateProgressFromNode()
	{
		if (!IsInstanceValid(_cachedProgressNode) || Transforms.Count < 2)
			return;

		Node3D parent = (Node3D)GetParent();
		Transform3D startXform = Transforms[0];
		Transform3D endXform = Transforms[^1];

		// --- Positional Progress ---
		Vector3 grabLocalNow = parent.ToLocal(_cachedProgressNode.GlobalPosition);
		Vector3 axis = (endXform.Origin - startXform.Origin);
		float axisLength = axis.Length();
		float positionProgress = 0f;

		if (axisLength > 0.0001f)
		{
			Vector3 axisDir = axis.Normalized();
			Vector3 delta = grabLocalNow - _grabLocalStart;
			float projected = delta.Dot(axisDir);
			positionProgress = _startGrabRatio + projected / axisLength;
		}

		// --- Rotational Progress ---
		Basis startBasis = startXform.Basis;
		Basis endBasis = endXform.Basis;
		Basis grabBasis = _cachedProgressNode.GlobalTransform.Basis;

		Quaternion startQuat = new Quaternion(startBasis);
		Quaternion endQuat = new Quaternion(endBasis);
		Quaternion grabQuat = new Quaternion(grabBasis);

		float totalAngle = startQuat.AngleTo(endQuat);
		float rotationProgress = 0f;
		if (totalAngle > 0.0001f)
		{
			float grabAngle = startQuat.AngleTo(grabQuat);
			rotationProgress = grabAngle / totalAngle;
		}
	
		// --- Blended Progress ---
		float blendedProgress = Mathf.Lerp(positionProgress, rotationProgress, RotationWeight);
		Progress = Mathf.Clamp(blendedProgress, 0f, 1f);

		InterpolateTransforms(Progress);
	}
}
