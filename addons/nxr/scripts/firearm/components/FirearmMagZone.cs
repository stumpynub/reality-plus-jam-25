using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Godot;
using Godot.Collections;
using NXR;
using NXRFirearm;
using NXRInteractable;


namespace NXRFirearm;


[GlobalClass]
public partial class FirearmMagZone : InteractableSnapZone
{


    [Export] private bool _disableMag = false;

    [ExportGroup("Eject Settings")]
    [Export] public bool EjectEnabled = true; 
    [Export] private string _ejectAction = "ax_button";
    [Export] private float _ejectForce = 3f;
    [Export] public bool AddOnSnap = true; 


    public FirearmMag CurrentMag = null;
    public bool MagIn = false;

    private Firearm _firearm = null;


    [Signal] public delegate void TryEjectEventHandler();
    [Signal] public delegate void MagEnteredEventHandler(); 
    [Signal] public delegate void MagExitEventHandler(); 


    public override void _Ready()
    {

        OnSnap += OnSnapped;
        OnSnapExit += OnSnapExitped;
        TryEject += TriedEject; 

        base._Ready();

        _firearm = FirearmUtil.GetFirearmFromParentOrOwner(this);
        if (_firearm != null) { 
            _firearm.TryChamber += TryChamber;
            _firearm.OnFire += TryChamber;
        }
    }


    public override void _Process(double delta)
    {
        base._Process(delta);


        if (_firearm == null || _firearm.PrimaryGrab.Interactor == null) return; 
        
      
        if (_firearm.GetPrimaryController().ButtonOneShot(_ejectAction))
        {
            EmitSignal("TryEject");
        }
    }


    private void OnSnapped(Interactable mag)
    {
        if (!Util.NodeIs(mag, typeof(FirearmMag))) return;


        if (_disableMag)
        {
            mag.FullDrop();
            mag.Disabled = true;
        }

        if (AddOnSnap)
        {
            AddMag((FirearmMag)mag); 
        }
    }


    public void AddMag(FirearmMag mag)
    {
        CurrentMag = mag;
        mag.InitState.Parent = _firearm.InitState.Parent;
        mag.PreviousParent = _firearm.InitState.Parent;
        EmitSignal("MagEntered");
    }


    public void RemoveMag()
    {
        if (CurrentMag == null) return; 

        CurrentMag.Reparent(_firearm.InitState.Parent); 
    }

    private void OnSnapExitped(Interactable interactable)
    {
        if (CurrentMag != null)
        {
            CurrentMag.Disabled = false;
            CurrentMag = null;
            EmitSignal("MagExit");
        }
    }


    private void Eject(FirearmMag mag)
    {
        Unsnap();

        if (GetFirearm() != null && GetFirearm().PrimaryGrab.Interactor != null)
        {
            Vector3 linear = GetFirearm().GetPrimaryController().GetGlobalVelocity();
            float linearLength = GetFirearm().GetPrimaryController().GetLocalVelocity().Length();
   
            Vector3 anguler = GetFirearm().GetPrimaryController().GetAngularVelocity();
            float angLength = anguler.Normalized().Length();

            mag.LinearVelocity = (GetFirearm().GetPrimaryController().GetGlobalVelocity().Normalized() * linearLength) * angLength * _ejectForce;
            mag.AngularVelocity = anguler;
        }
    }


    private void TryChamber()
    {
        if (CurrentMag == null) return;
        if (!CurrentMag.CanChamber) return;

        if (CurrentMag.CurrentAmmo > 0)
        {
            _firearm.Chambered = true;
            CurrentMag.RemoveBullet(1);
            _firearm.EmitSignal("OnChambered");
        } 
    }
    

    private void TriedEject() { 
        if (CurrentMag == null || !EjectEnabled) return;

        Eject(CurrentMag); 
    }



    public Firearm GetFirearm()
    {
        return _firearm;
    }
}
