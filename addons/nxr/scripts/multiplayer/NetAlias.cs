using Godot;
using System;

public partial class NetAlias : Node3D
{

	[Export] bool HideLocal = false;
	[Export(PropertyHint.Range, "0.1, 1")] float Smoothing = 0.5f; 
	[Export] Node3D Target { get; set; }   
	
	private Transform3D _syncXform; 
	private float _currentSmoothing = 1;


    public override void _Ready()
    {

        if (IsMultiplayerAuthority()) {
			if (HideLocal) Visible = false; 
		} 

		CallDeferred("SetSmoothing"); 

    }
    public override void _Process(double delta)
	{
		if (IsMultiplayerAuthority()) { 

			GlobalTransform = Target.GlobalTransform; 
			_syncXform = GlobalTransform.Orthonormalized(); 

		} else { 

			GlobalPosition = GlobalTransform.Origin.Lerp(_syncXform.Origin, _currentSmoothing); 

			Quaternion q1 = GlobalBasis.Orthonormalized().GetRotationQuaternion(); 
			Quaternion q2 = _syncXform.Basis.Orthonormalized().GetRotationQuaternion(); 
			Quaternion q3 = q1.Normalized().Slerp(q2.Normalized(), _currentSmoothing); 

			GlobalBasis = new Basis(q3); 
		}
	}

	public async void SetSmoothing() { 
		await ToSignal(GetTree().CreateTimer(0.2), "timeout"); 

		_currentSmoothing = Smoothing; 
	} 
}
