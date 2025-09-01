using Godot;
using NXRMultiplayer;
using System;

public partial class NetRoot : Node3D
{
	[Export] public MultiplayerSpawner RootSpawner; 

	Multiplayer NetManager;
	Stage Stage { get; set; }


	[Signal] public delegate void PeerLoadedEventHandler(long id); 
	[Signal] public delegate void ServerLoadedEventHandler(); 


	public override void _Ready()
	{
		if (RootSpawner != null) { 
			RootSpawner.SpawnPath = GetPath(); 
		}

		NetManager = GetTree().Root.GetNode<Multiplayer>("NetManager");
		Stage = (Stage)GetParent();

		if (Multiplayer.IsServer())
		{
			Stage.PeerLoaded += Loaded;
			Stage.ServerLoaded += LoadedServer; 
		}
	}

	private void Loaded(int id)
	{
		EmitSignal("PeerLoaded", (long)id); 
	}

	private void LoadedServer() { 
		EmitSignal("ServerLoaded"); 
	}	
}

