using Godot;
using System;

[GlobalClass]
public partial class DeleteTimer : Timer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Timeout += OnTimeout; 
	}

    private void OnTimeout()
    {
		GetParent().QueueFree(); 
    }
}
