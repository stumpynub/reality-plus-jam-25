using Godot;
using Godot.Collections;
using NXRInteractable;
using System;
using System.Collections.Generic;


[GlobalClass]
public partial class NetGrabSpawner : MultiplayerSpawner
{

	[Export] public PackedScene Scene { get; set; }
	// Called when the node enters the scene tree for the first time.
	[Export] SpawnLocation SpawnLocation { get; set; }

	[Export] Interactable Interactable { get; set; }

	[Export] Interactor GrabbedInteractor { get; set; }
	public override void _Ready()
	{
		SpawnPath = GetSpawnPath();

		Func<Variant, Node> spawnAction = SpawnItem;

		SpawnFunction = Callable.From(spawnAction);
		Interactable.OnGrabbed += Grabbed;
	}

	private NodePath GetSpawnPath()
	{
		switch (SpawnLocation)
		{
			case SpawnLocation.Parent:
				return GetParent().GetPath();
			case SpawnLocation.Owner:
				return Owner.GetPath();
			case SpawnLocation.OwnerParent:
				return Owner.GetParent().GetPath();
			case SpawnLocation.OwnerOwner:
				return Owner.GetParent().Owner.GetPath();
		}

		return GetParent().GetPath();
	}

	private Node SpawnItem(Variant d)
	{
		Interactable interactable = (Interactable)Scene.Instantiate();
		
		Godot.Collections.Array data = (Godot.Collections.Array)d;
	
		if (data != null)
		{
			interactable.GlobalPosition = (Vector3)data[0];
		}

		return (Node)interactable;
	}


	private void Grabbed(Interactable interactable, Interactor interactor)
	{
		Interactable.Drop(interactor);

		Godot.Collections.Array data = new();
		data.Add(interactor.GlobalPosition);

		Node spawned = Spawn(data);

		if (interactor.IsMultiplayerAuthority())
		{
			Interactable inter = (Interactable)spawned; 
			interactor.Grab(inter); 
		}
	}
}
