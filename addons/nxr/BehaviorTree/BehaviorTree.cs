using Godot;
using System;
using System.Collections.Generic;

public partial class BehaviorTree : Node

{

	List<Leaf> ActiveLeaves = new List<Leaf>(); 

	public override void _Process(double delta)
	{
		foreach (Leaf leaf in ActiveLeaves) { 
			leaf.Tick();
		}
	}
}
