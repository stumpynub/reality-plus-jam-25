using Godot;
using NXR;
using NXRInteractable;
using System;

public partial class InteractableNetSync : NetSync
{

	Interactable Interactable;

	public override void _Ready()
	{
		base._Ready();

		if (Util.NodeIs(GetParent(), typeof(Interactable)))
		{
			Interactable = (Interactable)GetParent();
			Interactable.OnDropped += Dropped;
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (IsMultiplayerAuthority())
		{
			SetOwnerXform(); 
		}
		else
		{
			Interpolate();
		}
	}

	public void Dropped(Interactable interactable, Interactor interactor)
	{
		Rpc("SetOwnerXform");
	}
}
