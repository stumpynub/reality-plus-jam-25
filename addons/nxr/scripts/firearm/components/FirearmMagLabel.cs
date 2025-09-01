using Godot;
using NXRInteractable;

namespace NXRFirearm; 


[GlobalClass]
public partial class FirearmMagLabel : Label3D
{
	[Export] private FirearmMag _mag = null;

    public override void _Process(double delta)
    {
        if (_mag == null)
		{
			return ; 
		}
		
		Text = _mag.CurrentAmmo.ToString(); 
    }
}
