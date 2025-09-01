using Godot;
using NXRInteractable;
using System;
using System.Dynamic;



public partial class NetGrabSpawn : Interactable
{

	[Export] public PackedScene Scene { get; set; }
	[Export] SpawnLocation SpawnLocation { get; set; }

	Interactable LastSpawned = null;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Initialize(); 
		OnGrabbed += Grabbed;
	}

	private void Grabbed(Interactable interactable, Interactor interactor)
	{

		interactor.Drop();

		if (!Multiplayer.IsServer())
		{
			RpcId(1, "Spawn");
			if (LastSpawned != null)
			{
				interactor.Grab(LastSpawned); 
			}
		}
		else
		{
			Spawn();
		}
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void Spawn()
	{
		Interactable node = (Interactable)Scene.Instantiate();

		GetNode(GetSpawnPath()).AddChild(node, true);
		node.GlobalPosition = GlobalPosition;
		LastSpawned = node;
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
}
