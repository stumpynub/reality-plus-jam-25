using Godot;
using NXR;
using System;
using System.Runtime.Serialization;

public partial class level_loading : Node3D
{


	[Export] ProgressBar _progressBar;

	Stage Stage { get; set; }


	public override void _Ready()
	{
		if (Util.NodeIs(GetParent(), typeof(Stage))) { 
			Stage = (Stage)GetParent(); 

			Stage.ProgressUpdated += ProgressUpdated;  
		}
	}


	void ProgressUpdated(double progress) { 
		_progressBar.Value = (float)progress; 
	}
}
