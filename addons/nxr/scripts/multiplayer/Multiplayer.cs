using Godot;
using NXRPlayer;
using System;
using System.Collections.Generic;
using System.Linq;


namespace NXRMultiplayer;

[GlobalClass]
public partial class Multiplayer : Node
{
	public int MaxClients = 5; 
	public bool IsServer = false;
	public bool IsHostMode = false; 
	public string Address { get; set; } = "127.0.0.1";
	public int Port { get; set; } = 8000;
	public List<int> Peers = new(); 
	public List<Player> Players { get; set; } = new(); 

	[Signal] public delegate void PeerConnectedEventHandler(); 
	[Signal] public delegate void ServerCreatedEventHandler();
	[Signal] public delegate void HostConnectedEventHandler(int id);
	[Signal] public delegate void PlayerConnectedEventHandler(long id);
	[Signal] public delegate void PlayerDisconnectedEventHandler(long id);


	public override void _Ready() { 
		Resource root = GD.Load("res://scenes/levels/level_towers.tscn"); 
		string[] args = OS.GetCmdlineArgs(); 
	
	
		if (args.Contains("-s")) { 
			GD.Print("is server build"); 
			CallDeferred("StartServer"); 
		} 
		
		Multiplayer.ConnectionFailed += ConnectionFailed; 
	}


    public void StartServer()
	{
		ENetMultiplayerPeer peer = new();
		peer.SetBindIP(Address); 
		Error error = peer.CreateServer(Port, maxClients: MaxClients);


		if (error != Error.Ok)
		{
			GD.Print("Error creating server:" + error.ToString());
			return;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.Fastlz);
		Multiplayer.MultiplayerPeer = peer;
		
		string[] args = OS.GetCmdlineArgs(); 

		if (!args.Contains("-s"))
		{
			CallDeferred("emit_signal", "HostConnected", peer.GetUniqueId());
			Peers.Add(1); 
			IsHostMode = true; 
		}
		
		EmitSignal("ServerCreated"); 
	}


	public void JoinServer()
	{
		ENetMultiplayerPeer peer = new();
		Error error = peer.CreateClient(Address, Port);


		if (error != Error.Ok)
		{
			GD.Print("Error joining server:" + error.ToString());
			return;
		}

		PeerConnected += ConnectedToServer; 

		peer.Host.Compress(ENetConnection.CompressionMode.Fastlz);
		Multiplayer.MultiplayerPeer = peer;
		Peers.Add(peer.GetUniqueId()); 
	}


	private void ConnectedToServer() { 
		GD.Print("Connection Sucess"); 
		EmitSignal("PeerConnected"); 
	}


	private void ConnectionFailed() { 
		GD.Print("connection failed"); 
		Multiplayer.MultiplayerPeer = null; 
	}
}
