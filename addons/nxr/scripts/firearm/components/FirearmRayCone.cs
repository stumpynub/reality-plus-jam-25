using Godot;
using NXRFirearm;
using System;
using NXR;
using System.Collections.Generic;

[GlobalClass]
public partial class FirearmRayCone : Node3D
{
    [Export] public float MaxAngle { get; set; } = 15.0f;
    private List<FirearmRay> _rays = new();
    private Firearm _firearm;

    public override void _Ready()
    {

        for (int i = 0; i < GetChildCount(); i++)
        {
            Node3D child = GetChild<Node3D>(i);
            if (child is FirearmRay ray)
            {
                _rays.Add(ray);
            }
        }

        Rotate(); 

        _firearm = FirearmUtil.GetFirearmFromParentOrOwner(this);

        if (_firearm != null)
        {
            _firearm.OnFire += OnFire;
        }

    }

    private void OnFire()
    {
       Rotate(); 
    }

    private void Rotate()
    {
        foreach (var ray in _rays)
        {
            float rangeX = (float)GD.RandRange(-MaxAngle, MaxAngle);
            float rangeY = (float)GD.RandRange(-MaxAngle, MaxAngle);
            ray.Rotation = new Vector3(
                Mathf.DegToRad(rangeX),
                Mathf.DegToRad(rangeY),
                0
            );
        }
    }
}
