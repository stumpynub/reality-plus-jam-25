using Godot;
using NXRPlayer;
using NXRMultiplayer;
using Godot.Collections;


public enum SpawnType { 
	Random, 
	RoundRobin, 
	
}

public partial class NetPlayerSpawn : Node3D
{
	[Export] NetRoot NetRoot { get; set; }
	[Export]SpawnType SpawnType = SpawnType.Random; 
	[Export] PackedScene PlayerScene { get; set; }
	[Export] Array<Node3D> SpawnPoints { get; set; }

	public int CurrentSpawn { get; set; } = 0; 

	Multiplayer NetManager;

	[Export]Array<Player> Players = new(); 


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		NetManager = GetTree().Root.GetNode<Multiplayer>("NetManager"); 

		if (Multiplayer.IsServer()) { 
			NetRoot.PeerLoaded += AddPlayer;
			NetRoot.ServerLoaded += ServerLoaded; 
			Multiplayer.PeerDisconnected += RemovePlayer; 
		}
	}	

	public void AddPlayer(long id)
	{
		if (!Multiplayer.IsServer()) return; 
		
		
		Player inst = (Player)PlayerScene.Instantiate();
		
		inst.Name = id.ToString();

		AddChild(inst, true);
	
		inst.Rpc("SetXform", new Transform3D(inst.GlobalBasis, GetSpawnPoint().GlobalPosition)); 
		Players.Add(inst); 

		CurrentSpawn += 1; 
	}

	public void RemovePlayer(long id) { 

		if (!Multiplayer.IsServer()) return; 

		foreach (Player player in Players) { 
			if (int.Parse(player.Name) == (int)id) { 
				Players.Remove(player); 
				player.QueueFree(); 
			}
		}
	}

	public Node3D GetSpawnPoint() { 
		if (SpawnPoints.Count <=  0) { 
			return this; 
		}

		switch (SpawnType) { 
			case SpawnType.Random: 
				return SpawnPoints.PickRandom();  
			case SpawnType.RoundRobin: 

				if (CurrentSpawn >= SpawnPoints.Count) { 
					CurrentSpawn = 0; 
				}
				return SpawnPoints[CurrentSpawn]; 
		}

		return this; 
	}


	public void Respawn(int id) { 
		if (!Multiplayer.IsServer()) return; 

		foreach (Player player in Players) { 
			if (int.Parse(player.Name) == id) {
				
				RemoveChild(player);  
				player.QueueFree(); 
				Players.Remove(player); 

				CallDeferred("AddPlayer", id); 
			}
		}
	}

	public void ServerLoaded() { 
		GD.Print("server loaded"); 
		if (NetManager.IsHostMode) { 
			AddPlayer(1); 
		}
	}
}
