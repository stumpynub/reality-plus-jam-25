using Godot;
using System;


public enum SpawnLocation { 
	Parent, 
	Owner, 
	OwnerParent, 
	OwnerOwner, 
}

[GlobalClass]
public partial class NetSpawner : MultiplayerSpawner
{

	[Export] public PackedScene Scene { get; set; }
	// Called when the node enters the scene tree for the first time.
	[Export] SpawnLocation SpawnLocation { get; set; }
	public override void _Ready()
	{
		AddSpawnableScene(Scene.ResourcePath); 
		SpawnPath = GetSpawnPath(); 
	}
	
	private NodePath GetSpawnPath() { 
		switch(SpawnLocation) { 
			case SpawnLocation.Parent: 
				return GetParent().GetPath(); 
			case SpawnLocation.Owner: 
				return Owner.GetPath();  
			case SpawnLocation.OwnerParent: 
				return Owner.GetParent().GetPath(); 
		}

		return GetParent().GetPath(); 
	}
}
