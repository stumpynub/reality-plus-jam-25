using Godot;
using NXR;
using System;

public partial class NetChildSpawner : MultiplayerSpawner
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (Node child in GetChildren()) { 
			Node3D inst = (Node3D)child.Duplicate(); 
			child.QueueFree(); 

			GetParent().AddChild(inst); 
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
