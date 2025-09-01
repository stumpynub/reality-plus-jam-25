using System;
using System.Linq;
using Godot;
using NXR;
using NXRInteractable;

namespace NXRFirearm;

[Tool]
[GlobalClass]
public partial class FirearmBreakAction : FirearmPathInteractable
{

	[Export] private Node3D _bulletQueue;
	[Export] private Transform3D _relativeMoveXform;

 	private bool _moveable = false;


	public override void _Ready()
	{
		base._Ready();

		if (Firearm != null)
		{
			Firearm.OnGrabbed += Grabbed;
			Firearm.OnFire += Fired; 
		}
	}

	public override void _Process(double delta)
	{

		RunTool(); 

		if (Firearm == null) return;


		if (Firearm.PrimaryGrab.Interactor != null && Firearm.GetPrimaryController().ButtonOneShot("ax_button"))
		{
			Open();
		}

		if (!IsClosed() && GetCloseInput())
		{
			Close();
		}

		if (_moveable && Firearm.SecondaryGrab.Interactor != null)
		{
			Vector3 relLocal = ToLocal(_relativeMoveXform.Origin);
			Vector3 currentLocal = ToLocal(Firearm.SecondaryGrab.Interactor.GlobalPosition);
			float pull = Mathf.Clamp((currentLocal.Y - relLocal.Y) * 10.0f, 0f, 1.0f);
			InterpolateTransforms(pull, true);
		}

		if (_moveable && AtStart()) 
		{
			_moveable = false; 
		}

		HandleChamber(); 
	}


	private void HandleChamber()
	{
		if (_bulletQueue == null) return; 

		FirearmBulletZoneQueue queue = _bulletQueue as FirearmBulletZoneQueue;

		if (queue.GetSorted(true).Count > 0 && AtStart())
		{
			Firearm.Chambered = true; 
		}
		else
		{
			Firearm.Chambered = false; 
		}
	}


	private bool IsClosed()
	{
		return Target.Transform.IsEqualApprox(GetStartXform());
	}


	public async void Open()
	{
		_moveable = false; 

		await ToSignal(GoToEnd(ease: Tween.EaseType.In, trans: Tween.TransitionType.Cubic, time: 0.2f), "finished");
		
		_moveable = true;

		if (Firearm.SecondaryGrab.Interactor != null)
		{
			_relativeMoveXform = Firearm.SecondaryGrab.Interactor.GlobalTransform;
		}
		

		if (Util.NodeIs(_bulletQueue, typeof(FirearmBulletZoneQueue)))
		{
			FirearmBulletZoneQueue queue = (FirearmBulletZoneQueue)_bulletQueue;
			queue.EjectAll();
		}
	}

	public void Close()
	{
		GoToStart(time: 0.05f); 
		_moveable = false;
	}


	private bool GetCloseInput()
	{
		if (Firearm.PrimaryGrab.Interactor == null) return false;
		Vector3 dir = Firearm.GetPrimaryController().Transform.Basis.Y;

		return Firearm.GetPrimaryController().VelMatches(dir, 2f);
	}


	private void Grabbed(Interactable interactable, Interactor interactor)
	{
		if (interactor == interactable.SecondaryGrab.Interactor)
		{
			_relativeMoveXform = interactor.GlobalTransform;
		}
	}


	private void Fired()
	{
		FirearmBulletZoneQueue queue = _bulletQueue as FirearmBulletZoneQueue;
		queue.GetSorted(true).First().Bullet.Spent = true; 
	}
}
