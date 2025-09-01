using Godot;
using NXR;
using NXRFirearm;
using NXRInteractable;

public enum SlidePreset
{
    OpenBolt,
    ClosedBolt,
}


[Tool]
[GlobalClass]
public partial class FirearmSlide : FirearmPathInteractable
{
    #region Exported
    [Export] private bool _setBackOnFire = false;
    [Export] private bool _setBackOnEmpty = false;
    [Export] private string _releaseAction = "";
    #endregion


    #region Private
    protected bool back = false;
    private Transform3D _relativeGrabXform;
    protected Transform3D _relativeXform = new();
    private bool lockedBack = false;
    private Tween _returnTween;
    private HapticTicker _hapticTicker = new HapticTicker();
    #endregion


    #region Signals 
    [Signal] public delegate void SlideBackEventHandler();
    [Signal] public delegate void SlideForwardEventHandler();
    #endregion



    public override void _Ready()
    {
        base._Ready();

        if (Firearm == null) return;

        Firearm.OnFire += OnFire;
        Firearm.OnChambered += Chambered;
        Firearm.TryChamber += TryChambered;

        OnDropped += OnDrop;
        OnGrabbed += Grabbed;

        AddChild(_hapticTicker); 
    }


    public override void _Process(double delta)
    {
        RunTool();

        if (Firearm == null) return;


        if (AtEnd() && !back && IsGrabbed())
        {

            if (Firearm.Chambered)
            {
                Firearm.EmitSignal("TryEject");
                Firearm.Chambered = false;
            }

            back = true;
            EmitSignal("SlideBack");
        }

        if (!AtEnd() && back)
        {
            Firearm.EmitSignal("TryChamber");
            back = false;
            EmitSignal("SlideForward");
        }

        if (GetReleaseInput() == true && IsBack())
        {
            Firearm.EmitSignal("TryChamber");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }


    public bool IsBack()
    {
        return Position.IsEqualApprox(GetEndXform().Origin);
    }


    public bool IsForward()
    {
        return Position.IsEqualApprox(GetStartXform().Origin);
    }


    public void OnFire()
    {
        if (_setBackOnFire)
        {
            _moveTween.Kill();
            Progress = 1.0f; 
        }
    }

    public void OnDrop(Interactable interactable, Interactor interactor)
    {
        ReturnTween();
    }


    private void Grabbed(Interactable interactable, Interactor interactor)
    {


    }



    private void TryChambered()
    {
        if (IsBack() && !Firearm.Chambered)
        {
            ReturnTween();
        }
    }


    private void Chambered()
    {
        ReturnTween();
    }


    private void ReturnTween()
    {
        if (lockedBack) return;

        GoToStart(time: 0.1f);
    }


    private bool GetReleaseInput()
    {
        if (Firearm.PrimaryGrab.Interactor == null) return false;
        return Firearm.GetPrimaryController().ButtonOneShot(_releaseAction);
    }
}
