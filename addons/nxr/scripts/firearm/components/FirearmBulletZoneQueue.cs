using Godot;
using NXR;
using System.Collections.Generic;
using System.Linq;

namespace NXRFirearm;


/// <summary>
/// This class is used when using Bullet.cs for queing the next bullet to be shot
/// </summary>



public enum EjectAxis
{
	X,
	Y,
	Z
}


[GlobalClass]
public partial class FirearmBulletZoneQueue : Node3D
{
	
	[Export] private Firearm _firearm;

	[ExportGroup("EjectSettings")]
	[Export] private EjectAxis _ejectAxis = EjectAxis.Z;
	[Export] private Vector3 _ejectAngularForce = Vector3.One;
	[Export] private float _ejectForce = 1.0f; 
	[Export] private float _timeBetween = 0.25f; 

	
	private List<FirearmBulletZone> _bulletZones = new();


	public override void _Ready()
	{
		foreach (Node3D child in GetChildren())
		{
			if (Util.NodeIs(child, typeof(FirearmBulletZone))) _bulletZones.Add((FirearmBulletZone)child);
		}

		_firearm = FirearmUtil.GetFirearmFromParentOrOwner(this);
	}



	public void FireIndex(int index)
	{
		if (_bulletZones[index] == null) return;

		_bulletZones[index].QueueFree();
	}


	public FirearmBulletZone GetZoneIndex(int index)
	{	
		if (index < 0 || index > _bulletZones.Count) return null; 
		if (_bulletZones[index] == null) return null;

		return _bulletZones[index];
	}



	public async void EjectAll(bool onlyEmpty = false)
	{
		foreach (FirearmBulletZone zone in GetSorted())
		{
			
			if (onlyEmpty && zone.Bullet != null && zone.Bullet.Spent)
			{
				zone.Eject(GetEjectAxis() * _ejectForce, _ejectAngularForce, _firearm.InitState.Parent);
			}

			if (!onlyEmpty)
			{
				zone.Eject(GetEjectAxis() * _ejectForce, _ejectAngularForce, _firearm.InitState.Parent);
			}

			await ToSignal(GetTree().CreateTimer(_timeBetween), "timeout"); 
		}
	}
	
	public void EnableAll() 
	{
		foreach (FirearmBulletZone zone in _bulletZones)
		{
			zone?.Enable(); ;
		}
	}

	public void DisableAll() 
	{
		foreach (FirearmBulletZone zone in _bulletZones)
		{
			zone?.Disable();
		}
	}


	public List<FirearmBulletZone> GetSorted(bool removeSpent = false)
	{
		if (_bulletZones.Count <= 0) return _bulletZones;

	
		if (removeSpent)
			return _bulletZones.Where(x => x.Bullet != null && x.Bullet.Spent == false).ToList();
		else
			return _bulletZones.OrderBy(x => x.Bullet != null && x.Bullet.Spent == false).ToList();
	}


	private Vector3 GetEjectAxis()
	{
        return _ejectAxis switch
        {
            EjectAxis.X => GlobalBasis.X,
            EjectAxis.Y => GlobalBasis.Y,
            EjectAxis.Z => GlobalBasis.Z,
            _ => Vector3.Back,
        };
    }
}
