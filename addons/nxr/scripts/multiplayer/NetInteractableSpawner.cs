using Godot;
using NXRInteractable;
using System;

public partial class NetInteractableSpawner : Node3D
{

	[Export] PackedScene Interactable; 
	[Export] MultiplayerSpawner Spawner;


	public override void _Ready() { 
		if (!IsMultiplayerAuthority()) return; 

		Spawner.SpawnPath = GetPath(); 

		CallDeferred("Spawn"); 
	}


	public void Grabbed(Interactable interactable, Interactor interactor)
	{
	}

	public void Spawn() { 
		Interactable interactable = (Interactable)Interactable.Instantiate(); 

		AddChild(interactable); 
		
		interactable.GlobalTransform = GlobalTransform; 

		interactable.OnGrabbed += Grabbed;
	}
}
