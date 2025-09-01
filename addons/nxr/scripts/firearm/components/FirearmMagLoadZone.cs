using Godot;
using NXR;
using NXRFirearm;
using NXRInteractable;
using System;

[GlobalClass]
public partial class FirearmMagLoadZone : Area3D
{
	[Export] private String _ammoGroup;
	[Export] private FirearmMag _mag; 
	[Export] private AmmoStack _stack; 

	[Signal] public delegate void AmmoAddedEventHandler(); 


	public override void _Ready()
	{
		BodyEntered += Entered; 
	}

	
	private void Entered(Node3D body) { 
			
		if (body.IsInGroup(_ammoGroup)) { 
			
			if (_mag.CurrentAmmo >= _mag.Capacity) return; 

			FirearmBullet bullet = null; 

			if (body is FirearmBullet) { 
				bullet = (FirearmBullet)body; 
			}

			if (_mag != null && _mag.CurrentAmmo < _mag.Capacity) { 
				bullet?.FullDrop(); 
				body.QueueFree(); 
				_mag.AddBullet(1); 
				EmitSignal("AmmoAdded"); 
			}
		}
	}
}
