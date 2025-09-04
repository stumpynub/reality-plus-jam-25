using Godot;
using System;

public partial class RaceManager : Node3D
{
	[Export] private Ship _ship;
	[Export] private int _maxLaps = 3; 
	[Export] private Area3D _startFinishLine;
	int _lapCount = 0;

	Timer _startTimer = new();


	public override void _Ready()
	{
		_ship.ShipEnabled = false;
		AddChild(_startTimer);
		_startTimer.WaitTime = 3.0f;
		_startTimer.OneShot = true;
		_startTimer.Timeout += StartRace;
		_startTimer.Start();
		TimeManager.Instance.StartStopwatch(); 
	}


	private void StartRace()
	{
		_ship.ShipEnabled = true;
	}
}
