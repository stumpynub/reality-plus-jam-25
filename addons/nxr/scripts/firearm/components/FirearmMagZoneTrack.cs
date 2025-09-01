using System;
using System.Data.Common;
using Godot;
using NXR;
using NXRInteractable;

namespace NXRFirearm;

[Tool]
[GlobalClass]
public partial class FirearmMagZoneTrack : PathInteractable
{
	[Export] private float pullStrength = 2.0f;

	private FirearmMagZone _zone;
	private float _startGrabRatio = 0f;
	private Vector3 _grabLocalStart = Vector3.Zero;
	private bool _isGrabbing = false;
	private float _currentRatio = 0.0f;


	public override void _Ready()
	{
		if (GetChildCount() == 0 || GetChild(0) is not FirearmMagZone zone) return;

		_zone = zone;
		_zone.EjectEnabled = false;

		_zone.TryEject += TryEject;
		_zone.OnSnap += OnSnapped;
		_zone.OnSnapExit += OnSnapExit;
		_zone.AddOnSnap = false; 

		if (Engine.IsEditorHint())
			Transform = GetStartXform();

		Freeze = true;
	}

	public override void _Process(double delta)
	{
		RunTool();

		if (_zone == null) return;

		UpdateProgressFromNode();

		bool atStart =Progress < 0.05f;
		bool atEnd = Progress > 0.98f;

		if (AtStart() && _zone.CurrentMag == null && _zone.SnappedInteractable != null)
		{
			_zone.AddMag((FirearmMag)_zone.SnappedInteractable); 
		}

		if (Progress > 0.95)
				_zone.Unsnap(true);
			
		if (_zone.SnappedInteractable != null && _zone.SnappedInteractable is FirearmMag mag) 
			mag.CanChamber = _currentRatio < 0.1f;


		if (_zone.SnappedInteractable != null)
		{
			_hapticTicker.Tick(_zone.SnappedInteractable.GetPrimaryController(), Progress); 
		}
		
		if (atStart && _zone.SnappedInteractable?.IsGrabbed() == true)
			_zone.SnappedInteractable.FullDrop();
	}


	private void OnSnapped(Interactable interactable)
	{
		interactable.OnGrabbed += Grabbed;
		interactable.OnDropped += Dropped;

		// If it's already grabbed, initialize grabbing now
		if (interactable.IsGrabbed())
			StartNodeProgress(interactable.PrimaryGrab.Interactor, 0.9f); 
	}


	private void OnSnapExit(Interactable interactable)
	{
		interactable.OnGrabbed -= Grabbed;
		StopNodeProgress(); 
	}


	private void Grabbed(Interactable interactable, Interactor interactor)
	{
		if (Progress < 0.1f)
			StartNodeProgress(interactor, 0.1f, 0.2f);
		else
			StartNodeProgress(interactor);
		
	}	

	private void Dropped(Interactable interactable, Interactor interactor)
	{
		StopNodeProgress(false);
	}



	private async void TryEject()
	{
		Tween tween = GoToEnd(0.1f); 

		await ToSignal(tween, "finished");

		if (_zone.SnappedInteractable == null) return;

		if (_zone.GetFirearm()?.GetPrimaryController() is { } controller)
		{
			Vector3 ang = controller.GetAngularVelocity().LimitLength(3);
			_zone.CurrentMag.LinearVelocity = controller.GetGlobalVelocity() * ang.Length();
			_zone.CurrentMag.AngularVelocity = ang;
		}
		GD.Print("unsnapping mag");
		_zone.Unsnap(true);
	}


	private async void Exit(InteractableSnapZone zone)
	{
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(this, "transform", GetEndXform(), 0.2f);
		await ToSignal(tween, "finished");

		zone.Unsnap();
	}
}
