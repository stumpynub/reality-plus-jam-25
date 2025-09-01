using Godot;
using NXRFirearm;
using System;

namespace NXRFirearm;


[GlobalClass]
public partial class FirearmMuzzleFlash : Sprite3D
{
    [Export] Firearm _firearm;
    [Export] Godot.Collections.Array<Texture2D> _textures;

    private float t = 0;

    public override void _Ready()
    {
        if (_firearm != null)
        {
            _firearm.OnFire += OnFire;
        }

        Visible = false; 
    }

    private void OnFire()
    {
        Flash();
    }

    private async void Flash()
    {

        t = GD.Randf() * 100;

        if (t > 50) return; 

        Visible = true; 

        await ToSignal(GetTree().CreateTimer(0.02), "timeout");

        Visible = false;
        t = 0; 
    }
}
