using Godot;
using System;


namespace NXR;

public enum BillboardMode { 
    Disabled, 
    Enabled, 
    YBillboard, 
}


[GlobalClass]
public partial class Billboard3D : Node3D
{

    [Export] public Node3D Target; 
    [Export] public BillboardMode Mode;
    [Export(PropertyHint.Range, "0.0, 1.0")] public float Smoothing = 0.95f;


	private Vector3 _initScale; 


    public override void _Ready()
    {
		if (Target == null) return; 
		
		_initScale = Target.Scale; 
    }

    public override void _Process(double delta)
    {
		if (Engine.IsEditorHint()) return; 
        ManageBillboard(); 
    }


	private void ManageBillboard()
	{
		if (GetViewport().GetCamera3D == null) return; 

        Target ??= this;


		Camera3D cam = GetViewport().GetCamera3D();
		Vector3 look = Vector3.Zero;
		look = cam.GlobalPosition - Target.GlobalPosition;

		switch (Mode)
		{
			case BillboardMode.Disabled:
				return;
			case BillboardMode.Enabled: 
				BasisLook(look); 
				break; 
			case BillboardMode.YBillboard: 
				look.Y = 0; 
				BasisLook(look); 
				break; 
				
		}
	}

    private void BasisLook(Vector3 look) { 

        Smoothing = Mathf.Clamp(Smoothing, 0.05f, 1.0f); 
        Target.GlobalBasis = Basis.LookingAt(look.Normalized(), Vector3.Up, true).Scaled(_initScale); 
    }
}
