using System;
using Godot;
using NXR;
using NXRFirearm;


[Tool]
[GlobalClass]
public partial class FirearmHammer : FirearmPathInteractable
{

	[Export] private bool _singleAction = false;

	private bool _hammerReset = true;
	private bool _lockedBack = false;
	private float _threshold = 0.9f;
	
	public override void _Ready()
	{
		base._Ready();
	}

	public override void _Process(double delta)
	{
		RunTool();

		if (Firearm == null) return;


		if (_singleAction)
		{
			HandleSingleAction(); 
		}
		else
		{
			HandleDoubleAction();
		}
	}


	private void HandleSingleAction() { 
		if (Firearm.PrimaryGrab.Interactor != null)
		{
			
			if (!_lockedBack && _hammerReset)
			{
				InterpolateTransforms(GetJoyValue());
			}

			// locks hammer back 
			if (GetJoyValue() >= 1 && !_lockedBack && _hammerReset) { 
				GoToEnd(); 
				_lockedBack = true;
				_hammerReset = false;
			}

			// resets hammer 
			if (!AtEnd() && GetJoyValue() <= 0.5) { 
				_hammerReset = true; 
			}

			if (_lockedBack && Firearm.GetPrimaryController().ButtonOneShot("trigger")) { 
				Firearm.EmitSignal("TryFire");
				GoToStart();
				_lockedBack = false; 
			}
		}
	}
	
	
	private void HandleDoubleAction()
	{
		if (_hammerReset)
		{
			InterpolateTransforms(GetTriggerValue());
		}

		if (AtEnd() && _hammerReset)
		{
			GoToStart();
			_hammerReset = false;
			Firearm.EmitSignal("TryFire");

		}

		if (GetTriggerValue() <= 0)
		{
			_hammerReset = true;
		}
	}


	public float GetJoyValue()
	{
		float joyY = 0f;

		if (Firearm.PrimaryGrab.Interactor != null)
		{
			joyY = Firearm.GetPrimaryController().GetVector2("primary").Y;
		}
		joyY = Mathf.Clamp(joyY, -1f, 0.0f);
		joyY = Math.Abs(joyY);

		if (joyY >= _threshold) joyY = 1;
		if (_lockedBack) joyY = 1.0f; 
		if(AtStart()) joyY = 0.0f;

		return joyY;
	}


	private float GetTriggerValue()
	{
		return Firearm.GetTriggerPullValue();
	}
}
