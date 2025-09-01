using Godot;
using Godot.Collections;
using System;

public interface ISignalTool { 
	
	Node Node { get; set; }
    string Signal { get; set; }

	public void OnSignal()
	{
		Action();
	}


	public void OnSignalParams(params int[] p)
	{
		Action();
	}


	public void Action(); 

}
