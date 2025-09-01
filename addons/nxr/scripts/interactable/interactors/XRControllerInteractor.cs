using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using NXR;

namespace NXRInteractable;


[GlobalClass]
public partial class XRControllerInteractor : Interactor
{

	#region Exported: 
	[Export] public Controller Controller { get; private set; }
	[Export(PropertyHint.Range, "0.001, 1.0")] public float Smoothing { get; set; } = 0f;
	[Export] public bool UpdateTransform { get; set; } = true;
	#endregion


	#region Public: 
	public RigidBody3D InteractorBody = new RigidBody3D();
	public Interactable CurrentHoverable = null;
	#endregion


	#region Private: 
	private float _distancePointBias = 0.95f;
	private Interactable _distanceInteractable = null;
	private List<Interactable> _cachedHovered = new();
	private Interactable _currentHovered = null;

	#endregion


	[Signal] public delegate void GrabbedEventHandler(Interactable interactable);
	[Signal] public delegate void DroppedEventHandler(Interactable interactable);


	public override void _Ready()
	{
		Controller.ButtonPressed += Interact;
		Controller.ButtonReleased += InteractDrop;
		BodyExited += BodyExit;
		BodyEntered += BodyEnter;
		InteractorBody.FreezeMode = RigidBody3D.FreezeModeEnum.Static;
	}


	public override void _PhysicsProcess(double delta)
	{
		if (UpdateTransform)
		{
			GlobalTransform = GlobalTransform.InterpolateWith(Controller.GlobalTransform, Mathf.Clamp(1 - Smoothing, 0.001f, 1.0f));
		}
	}


	public override void _Process(double delta)
	{
		if (GrabbedInteractable != null)
		{
			HandleGrabBreak();
		}

		UpdateHovered();
	}


	private void Interact(String buttonName)
	{

		if (IsInstanceValid(GrabbedInteractable))
		{

			if (GrabbedInteractable.HoldMode == HoldMode.Toggle && GrabbedInteractable.DropAction == buttonName)
			{
				Drop();
			}

			return;
		}


		Interactable interactable = FindBestHoveredInteractable(_cachedHovered, GlobalPosition, buttonName);
		if (interactable != null)
		{
			Grab(interactable);
			return;

		}
	}


	public void UpdateHovered()
	{
		Interactable bestHoverable = FindBestHoveredInteractable(_cachedHovered, GlobalPosition);
	
		if (GrabbedInteractable != null)
		{
			if (CurrentHoverable != null)
			{
				CurrentHoverable.EmitSignal("OnHoveredOut", this);
				CurrentHoverable.HoveredInteractor = null;
				CurrentHoverable = null;
			}
			return; 
		}

		if (bestHoverable == null && CurrentHoverable != null)
		{
			CurrentHoverable.EmitSignal("OnHoveredOut", this);
			CurrentHoverable.HoveredInteractor = null;
			CurrentHoverable = null;
			return;
		}

		if (bestHoverable != CurrentHoverable && bestHoverable.HoveredInteractor == null)
		{
			if (_currentHovered != null)
				_currentHovered.HoveredInteractor = null;

			if (bestHoverable != null)
				bestHoverable.HoveredInteractor = this;
			bestHoverable.EmitSignal("OnHovered", this);

			CurrentHoverable = bestHoverable;
		}
	}


	private void InteractDrop(String buttonName)
	{

		if (GrabbedInteractable != null && buttonName == GrabbedInteractable.GrabAction && GrabbedInteractable.HoldMode == HoldMode.Hold)
		{
			Drop();
			return;
		}

		if (_distanceInteractable != null)
		{
			_distanceInteractable.EmitSignal("OnDistanceGrabbedCancel", this);
			_distanceInteractable = null;
		}
	}


	private Interactable FindBestHoveredInteractable(
		List<Interactable> candidates,
		Vector3 origin,
		string requiredAction = null
	)
	{
		Interactable best = null;
		float bestScore = float.MaxValue;

		for (int i = candidates.Count - 1; i >= 0; i--)
		{
			var x = candidates[i];

			if (!IsInstanceValid(x))
			{
				candidates.RemoveAt(i);
				continue;
			}

			if (x.Disabled)
				continue;
			if (requiredAction != null && x.GrabAction != requiredAction)
				continue;

			bool isPrimary = x.PrimaryGrab.Interactor == null;
			Node3D grabPoint = isPrimary ? x.PrimaryGrabPoint : x.SecondaryGrabPoint;
			if (grabPoint == null) continue;

			if (!isPrimary && !x.SecondaryGrabEnabled)
				continue;

			float priority = isPrimary ? x.PrimaryGrabPoint.Priority : x.SecondaryGrabPoint.Priority;
			if (priority <= 0f) priority = 1f;

			Vector3 dir = grabPoint.GlobalPosition - origin;
			float dist = dir.Length();

			if (dist > x.GrabBreakDistance) continue;
			//if (x.DistanceGrabEnabled && dist > x.DistanceGrabReach) continue; 
			if (dist < 0.0001f) continue;

			float score = dist / priority;

			if (score < bestScore)
			{
				bestScore = score;
				best = x;
			}
		}

		return best;
	}


	private void HandleGrabBreak()
	{
		if (GrabbedInteractable == null) return;
		Interactable grabPoint = this == GrabbedInteractable.PrimaryGrab.Interactor ? GrabbedInteractable.PrimaryGrabPoint : GrabbedInteractable.SecondaryGrabPoint;
		float dist = GlobalPosition.DistanceTo(grabPoint.GlobalPosition);

		if (dist >= grabPoint.GrabBreakDistance)
		{
			Drop();
		}
	}


	private void BodyExit(Node3D body)
	{
		if (body is Interactable interactable)
		{
			_cachedHovered.Add(interactable);
			EmitSignal(nameof(OnHoverExited), interactable);
		}
	}

	private void BodyEnter(Node3D body)
	{
		if (body is Interactable interactable)
		{
			_cachedHovered.Add(interactable);
			EmitSignal(nameof(OnHoverEntered), interactable);
		}
	}
}