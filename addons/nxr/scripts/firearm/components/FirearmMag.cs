using Godot;
using System;
using NXRInteractable; 
using NXR;
using NXRFirearm;

namespace NXRFirearm; 

[GlobalClass]
public partial class FirearmMag : Interactable
{

    #region Exported
    [Export] private bool _internal = false; 
    [Export] private bool _infinite = false; 
    [Export] public int Capacity = 30;
    [Export] public int CurrentAmmo = 30;
    #endregion
    
    public bool CanChamber = true; 

    private Firearm _firearm; 


    [Signal] public delegate void AmmoTakenEventHandler(int ammoIndex); 


    public override void _Ready()
    {
        base._Ready();

        _firearm = this.GetParentOrOwnerOfType<Firearm>(); 
        if (_internal && _firearm != null)
        {
            _firearm.TryChamber += TryInternalChamber;
        }
    }


    private void TryInternalChamber() { 
        if (CurrentAmmo <= 0) return;
        
        CurrentAmmo -= 1; 
        _firearm.Chambered = true;
    }


    public void RemoveBullet(int amount) { 
        CurrentAmmo -= amount; 
        EmitSignal("AmmoTaken", CurrentAmmo); 
        CurrentAmmo = Mathf.Clamp(CurrentAmmo, 0, Capacity); 
    }


    public void AddBullet(int amount) { 
        CurrentAmmo += amount; 
        CurrentAmmo = Mathf.Clamp(CurrentAmmo, 0, Capacity); 
    }
}
