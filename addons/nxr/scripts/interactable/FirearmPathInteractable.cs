using Godot;
using NXRFirearm;
using NXRInteractable;
using System;


namespace NXR;

[Tool]
public partial class FirearmPathInteractable : PathInteractable
{


	public Firearm Firearm; 

	public override void _Ready()
	{
		base._Ready();
		Firearm = this.GetParentOrOwnerOfType<Firearm>();
	}
}
