using Godot;
using NXR;
using NXRPlayer;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;


namespace NXRMultiplayer; 

[GlobalClass]
public partial class MultiplayerSpawn : Node3D
{
	[Export] Multiplayer MultiplayerInstance { get; set;}
	[Export] PackedScene Scene { get; set; }
 
    public override void _Ready()
    {

		if (!Multiplayer.IsServer()) return; 
		
		CallDeferred("Spawn", 1);  
		
		
		if (MultiplayerInstance == null) return; 
		MultiplayerInstance.HostConnected += HostConnected; 

    }
	
    private void PlayerConnected(long id)
    {
		if (id == 1) return; 
    }
	
	private void HostConnected(int id)
    {		
		Spawn(id); 
    }

	private void Spawn(long id) { 
		Node3D inst = (Node3D)Scene.Instantiate(); 

		GetParent().AddChild(inst, true); 
		inst.GlobalPosition = GlobalPosition; 
	}
}
