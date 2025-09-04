using Godot;
using System;


[GlobalClass]
public partial class EnemyBase : CharacterBody3D, IShipTargetable
{

	public int Health { get; set; } = 100;


	public void Damage(int amount)
	{
		Health -= amount;
		if (Health <= 0)
		{
			QueueFree();
		}
	}


	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}
}
