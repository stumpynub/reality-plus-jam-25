using Godot;
using System;

public interface IShipTargetable
{
	public int Health { get; set; }
	public void Damage(int amount); 

}
