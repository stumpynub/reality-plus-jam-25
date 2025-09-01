using Godot;
using NXR;
using NXRInteractable;
using System;

namespace NXRFirearm; 

[GlobalClass]
public partial class FirearmBulletZone : InteractableSnapZone
{
	
	public FirearmBullet Bullet = null; 

    private bool _enabled; 

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override void Snap(Interactable interactable)
    {
		if (Util.NodeIs((Node)GetParent(), typeof(FirearmBullet))) return; 
		Bullet = (FirearmBullet)interactable; 
        Bullet.Disabled = true; 
        base.Snap(Bullet);
    }

	public void Eject(Vector3 velocity, Vector3 torque, Node3D newParent=null) { 
		if (Bullet == null) return; 
        if (newParent!= null)
        {
            Bullet.PreviousParent = newParent; 
        }
		Unsnap(); 
        Bullet.Disabled = false; 
		Bullet.ApplyTorqueImpulse(torque * 1000); 
		Bullet.ApplyCentralImpulse(velocity); 
        Bullet = null; 
	}

    public void Disable() { 
        Monitorable = false; 
        Monitoring = false; 
    }

    public void Enable() { 
        Monitorable = true; 
        Monitoring = true; 
    }

    public bool IsLoaded()
    {
        return Bullet != null && !Bullet.Spent; 
    }

}
