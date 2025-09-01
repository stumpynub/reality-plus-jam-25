using Godot;
using Godot.Collections;
using NXR;
using NXRInteractable;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;


[GlobalClass]
public partial class InteractableSnapZone : Area3D
{
	[Export] private SnapMode _snapMode = SnapMode.OnEnter;
	[Export] public bool Locked = false;
	[Export] public bool DropOnSnap = false;
	[Export] public String[] AllowedGroups;
	[Export] private float _snapWaitTime = 0.25f;

	[ExportGroup("Animation")]
	[Export] private AnimationPlayer _animPlayer = null;
	[Export] private float _tweenTime = 0.1f;
	[Export] private Tween.EaseType _easeType;
	[Export] private Tween.TransitionType _transType;

	[ExportGroup("Distance Settings")]
	[Export] private bool _requireDrop = false;
	[Export] private float _snapDistance = 0.08f;
	[Export] private float _breakDistance = 0.1f;


	[Signal] public delegate void OnSnapStartEventHandler(Interactable interactable);
	[Signal] public delegate void OnSnapEventHandler(Interactable interactable);
	[Signal] public delegate void OnSnapExitEventHandler(Interactable interactable);

	private Tween _snapTween;
	private readonly RemoteTransform3D _rt = new();

	public List<Interactable> HoveredInteractables { get; private set; } = new();
	public Interactable LastSnappedInteractable { get; private set; }
	public Interactable SnappedInteractable { get; private set; }
	public Interactable HoveredInteractable { get; private set; }


	public bool CanSnap { get; private set; } = true;
	public bool CanUnsnap { get; private set; } = true;

	public override void _Ready()
	{
		AddChild(_rt);
		BodyEntered += Entered;
		BodyExited += Exited;

		foreach (var child in GetChildren())
		{
			if (Util.NodeIs(child, typeof(Interactable)))
			{
				CallDeferred(nameof(ReadySnap), child);
				return;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(SnappedInteractable))
			SnappedInteractable = null;

		if (_snapMode == SnapMode.Distance)
		{
			if (SnappedInteractable == null && HoveredInteractable != null)
				DistanceSnap(HoveredInteractable);

			if (SnappedInteractable != null)
			{
				if (!_snapTween?.IsRunning() ?? true)
					SnappedInteractable.GlobalTransform = GlobalTransform;

				DistanceBreak();
			}
		}

		if (IsInstanceValid(LastSnappedInteractable) && GetOverlappingBodies().Contains(LastSnappedInteractable))
			Connect(LastSnappedInteractable);

		if (!IsInstanceValid(SnappedInteractable) && SnappedInteractable != null)
			Unsnap();

		if (HoveredInteractable != null && !IsInstanceValid(HoveredInteractable))
			HoveredInteractable = null;
	}

	public virtual async void Unsnap(bool force = false, bool disconnect = true)
	{
		if ((Locked && !force) || SnappedInteractable == null)
			return;

		_snapTween?.Kill();

		_rt.RemotePath = "";
		LastSnappedInteractable = SnappedInteractable;
		SnappedInteractable = null;

		EmitSignal("OnSnapExit", LastSnappedInteractable);
		CanSnap = false;

		if (_snapWaitTime > 0)
			await ToSignal(GetTree().CreateTimer(_snapWaitTime), "timeout");

		LastSnappedInteractable?.RemoveMeta("Snapped");
		CanSnap = true;
	}

	private void ReadySnap(Node3D child)
	{
		HoveredInteractable = (Interactable)child;
		Connect(HoveredInteractable);
		Snap(HoveredInteractable);
	}

	public virtual async void Snap(Interactable interactable)
	{
		if (!interactable.IsInsideTree() || (_snapTween?.IsRunning() ?? false))
			return;

		EmitSignal("OnSnapStart", interactable);

		interactable.SetMeta("Snapped", true);
		LastSnappedInteractable = interactable;
		SnappedInteractable = interactable;
		_rt.RemotePath = SnappedInteractable.GetPath(); 

		StartSnapTween();

		if (DropOnSnap)
			SnappedInteractable.FullDrop();

		if (_snapTween != null)
			await ToSignal(_snapTween, "finished");

		Connect(SnappedInteractable);
		EmitSignal("OnSnap", interactable);
	}

	private void StartSnapTween()
	{
		_rt.GlobalTransform = LastSnappedInteractable.GlobalTransform; 
		
		_snapTween?.Kill();
		_snapTween = GetTree().CreateTween();
		_snapTween.SetProcessMode(Tween.TweenProcessMode.Physics)
				  .SetParallel(true)
				  .SetEase(_easeType)
				  .SetTrans(_transType)
				  .TweenProperty(_rt, "position", Vector3.Zero, _tweenTime);

		_snapTween.TweenProperty(_rt, "rotation", Vector3.Zero, _tweenTime);
	}

	private void Entered(Node3D body)
	{
		if (!CanSnap) return;

		var interactable = Util.NodeIs(body, typeof(Interactable)) ? (Interactable)body : null;
		if (!IsValidInteractable(interactable)) return;

		if (HoveredInteractable != null && interactable != HoveredInteractable)
			Disconnect(HoveredInteractable);

		HoveredInteractable = interactable;
		Connect(HoveredInteractable);

		if (_snapMode == SnapMode.OnEnter)
		{
			HoveredInteractable.FullDrop();
			Snap(HoveredInteractable);
		}
	}

	private void Exited(Node3D body)
	{
		if (!CanSnap || body != HoveredInteractable || body == SnappedInteractable)
			return;

		Disconnect(HoveredInteractable);
	}

	private void DistanceBreak()
	{
		if (SnappedInteractable?.IsGrabbed() != true)
			return;

		var interactor = SnappedInteractable.PrimaryGrab.Interactor ?? SnappedInteractable.SecondaryGrab.Interactor;
		if (interactor == null) return;

		float distance = interactor.GlobalPosition.DistanceTo(SnappedInteractable.GlobalPosition);
		if (distance > _breakDistance)
			Unsnap();
	}

	private void DistanceSnap(Interactable interactable)
	{
		if (!CanSnap || !IsValidInteractable(interactable))
			return;

		var interactor = interactable.PrimaryGrab.Interactor ?? interactable.SecondaryGrab.Interactor;
		if (interactor == null) return;

		float distance = interactor.GlobalPosition.DistanceTo(GlobalPosition);
		if (distance < _snapDistance)
			Snap(interactable);
	}

	private void OnDropped(Interactable interactable, Interactor interactor)
	{
		if (_snapMode != SnapMode.OnDrop) return;

		bool isPrimary = interactor == interactable.PrimaryGrab.Interactor;
		bool isSecondary = interactor == interactable.SecondaryGrab.Interactor;

		if ((isPrimary && interactable.SecondaryGrab.Interactor != null) ||
			(isSecondary && interactable.PrimaryGrab.Interactor != null))
			return;

		CallDeferred(nameof(Snap), interactable);
	}

	private void OnGrabbed(Interactable interactable, Interactor interactor)
	{
		if (_snapTween?.IsRunning() == true && _snapMode != SnapMode.Distance)
			_snapTween.Stop();

		if (_snapMode is SnapMode.OnEnter or SnapMode.OnDrop)
			Unsnap();
	}

	private bool InGroup(Node3D node)
		=> AllowedGroups == null || AllowedGroups.Any(group => node.GetGroups().Contains(group));

	private bool IsValidInteractable(Interactable interactable)
		=> IsInstanceValid(interactable) &&
		   !interactable.HasMeta("Snapped") &&
		   InGroup(interactable) &&
		   interactable.IsGrabbed() &&
		   SnappedInteractable == null;

	private void Connect(Interactable interactable)
	{
		Action<Interactable, Interactor> dropAction = OnDropped;
		Action<Interactable, Interactor> grabAction = OnGrabbed;
		bool dropConnected = interactable.IsConnected("OnDropped", Callable.From(dropAction));
		bool grabConnected = interactable.IsConnected("OnGrabbed", Callable.From(grabAction));

		
		if (!dropConnected)
		{
			interactable.Connect("OnDropped", Callable.From(dropAction));
		}
		if (!grabConnected)
		{
			interactable.Connect("OnGrabbed", Callable.From(grabAction));
		}
	}


	private void Disconnect(Interactable interactable)
	{
		if (interactable == null) return;

		Action<Interactable, Interactor> dropAction = OnDropped;
		Action<Interactable, Interactor> grabAction = OnGrabbed;
		bool dropConnected = interactable.IsConnected("OnDropped", Callable.From(dropAction));
		bool grabConnected = interactable.IsConnected("OnGrabbed", Callable.From(grabAction));

		if (dropConnected)
		{
			interactable.Disconnect("OnDropped", Callable.From(dropAction));
		}
		if (grabConnected)
		{

			interactable.Disconnect("OnGrabbed", Callable.From(grabAction));
		}
	}

	public override void _ValidateProperty(Dictionary property)
	{
		// Reserved for property editor validation if needed
	}
}
