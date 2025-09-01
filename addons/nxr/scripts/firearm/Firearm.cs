
using Godot;
using NXR;
using NXRInteractable;

namespace NXRFirearm;

[GlobalClass]
public partial class Firearm : Interactable
{

    #region Exported: 
    [Export] public FireMode FireMode { get; set; } = FireMode.Single;
    [Export] private int _roundPerMinute = 600;
    [Export] private bool _autoChamber = false;
    [Export] private bool _startChambered = false;
    [Export] private bool _chamberOnFire = true;


    [ExportGroup("Burst Settings")]
    [Export] private int _burstAmount = 3;
    [Export] private float _burstTime = 0.5f;


    [ExportGroup("TwoHanded Settings")]
    [Export] private float _recoilMultiplier = 0.3f;


    [ExportGroup("Recoil Settings")]
    [Export] private Vector3 _recoilKick = new Vector3(0, 0, 0.15f);
    [Export] private Vector3 _recoilRise = new Vector3(15, 0, 0);
    [Export] private float _recoilTimeToPeak = 0.01f;
    [Export] private float _kickRecoverSpeed = .25f;
    [Export] private float _riseRecoverSpeed = .25f;
    [Export] private Curve _yCurve;


    [ExportGroup("Trigger Settings")]
    [Export] private float _triggerPressThreshold = 0.4f; 
    [Export] private float _triggerResetThreshold = 0.1f; 



    [ExportGroup("Haptic Settings")]
    [Export] private float _hapticStrength = 0.3f;
    #endregion



    #region Public: 
    public bool Chambered { get; set; } = false;
    public FirearmBullet ChamberedBullet { get; set; }
    public bool BlockFire { get; set; } = false;
    #endregion


    #region Private: 
    private bool _burstQueued = false;
    private Vector3 _initPositionOffset;
    private Vector3 _initRotationOffset;
    private Timer _fireTimer = new();
    private int _shotCount = 0;
    private Tween _recoilTween;
    private bool _triggerReset = true; 
    #endregion


    #region Signals: 
    [Signal] public delegate void OnFireEventHandler();
    [Signal] public delegate void OnEjectEventHandler();
    [Signal] public delegate void OnChamberedEventHandler();
    [Signal] public delegate void OnChamberFailedEventHandler(); 
    [Signal] public delegate void OnBulletFiredEventHandler(FirearmBullet bullet); 
    [Signal] public delegate void OnFiredLastBulletEventHandler(); 
    [Signal] public delegate void TryFireEventHandler();
    [Signal] public delegate void TryChamberEventHandler();
    [Signal] public delegate void TryEjectEventHandler();
    [Signal] public delegate void TryEjectEmptyEventHandler();
    [Signal] public delegate void TryEjectSpentEventHandler();
    #endregion


    public override void _Ready()
    {
        base._Ready();

        _initPositionOffset = PositionOffset;
        _initRotationOffset = RotationOffset;

        // timer setup 
        AddChild(_fireTimer);
        _fireTimer.WaitTime =  60.0 / _roundPerMinute;
        _fireTimer.OneShot = true;
        _fireTimer.ProcessCallback = Timer.TimerProcessCallback.Physics;

        if (_startChambered)
        {
            Chambered = true;
        }   

        TryFire += Fire; 
        OnEject += EjectChambered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTriggerPullValue() >= _triggerPressThreshold && _triggerReset)
        {
            Fire();
        }
        if (GetTriggerPullValue() <= _triggerResetThreshold)
        {
            _triggerReset = true;
        }
    }

    public void Fire()
    {
        if (!Chambered || !CanFire()) return;


        switch (FireMode)
        {
            case FireMode.Single:
                FireAction();
                _triggerReset = false; 
                break;
            case FireMode.Burst:
                FireActionBurst();
                _triggerReset = false; 
                break;
            case FireMode.Auto:
                FireAction();
                _triggerReset = true; 
                break; 
        }
    }

    private void FireAction() { 
        
        if (Util.NodeIs(GetParent(), typeof(InteractableSnapZone)) ) return;

        _shotCount += 1;
        Chambered = false;
        _fireTimer.Start();

        Recoil();

        GetPrimaryController()?.Pulse(_hapticStrength, 1.0, 0.1);
        GetSecondaryController()?.Pulse(_hapticStrength, 1.0, 0.1);

        EmitSignal("OnFire");


        if (_autoChamber) {
            Chambered = true; 
        }

        if (ChamberedBullet != null) EmitSignal("OnBulletFired", ChamberedBullet);
    }


     public async void FireActionBurst()
    {

        _burstQueued = true;

        for (int i = 0; i < _burstAmount; i++)
        {
            FireAction(); 
            await ToSignal(GetTree().CreateTimer(60.0 / _roundPerMinute), "timeout");
        }

        _burstQueued = false;
    }

    private bool CanFire()
    {
        return _fireTimer.IsStopped() && IsInstanceValid(PrimaryGrab.Interactor);
    }

    private bool GetFireInput()
    {
        switch (FireMode)
        {
            case FireMode.Single:
                return GetPrimaryController().ButtonOneShot("trigger_click");
            case FireMode.Burst:
                return GetPrimaryController().ButtonOneShot("trigger_click") && !_burstQueued;
            case FireMode.Auto:
                return GetPrimaryController().GetFloat("trigger_click") > 0.5;
        }
        return false;
    }
    
    public float GetTriggerValue()
    {
        if (PrimaryGrab.Interactor != null)
        {
            return GetPrimaryController().GetFloat("trigger");
        }
        return 0.0f;
    }

    public float GetTriggerPullValue()
    {
        if (PrimaryGrab.Interactor != null)
        {
            float value = Mathf.InverseLerp(0, _triggerPressThreshold, GetPrimaryController().GetFloat("trigger")); 
            value = Mathf.Clamp(value, 0.0f, 1.0f); 

            return value;
        }
        return 0.0f;
    }


    private void Recoil()
    {   

        float recoilMultiplier = 1.0f;
        float maxAngle = 90;


        if (_recoilTween == null) _recoilTween = GetTree().CreateTween(); 

        if (Mathf.Abs(RotationOffset.X) >= maxAngle) RotationOffset -= _recoilRise * 2.0f; // clamp rotatiion 
        if (SecondaryGrab.Interactor != null) recoilMultiplier = _recoilMultiplier;

        if (_yCurve != null)
        {
            _shotCount = _shotCount >= _yCurve.PointCount ? 0 : _shotCount;
            RotationOffset = new Vector3(RotationOffset.X, _yCurve.GetPointPosition(_shotCount).Y * recoilMultiplier, RotationOffset.Z);
        }

        if (_recoilTween.IsRunning())
        {
            _recoilTween.Kill();
        }

        _recoilTween = GetTree().CreateTween();
        _recoilTween.SetProcessMode(Tween.TweenProcessMode.Physics);

        _recoilTween.TweenProperty(this, "RotationOffset", RotationOffset + _recoilRise * recoilMultiplier, _recoilTimeToPeak / 2);
        _recoilTween.TweenProperty(this, "PositionOffset", PositionOffset + _recoilKick * recoilMultiplier, _recoilTimeToPeak / 2);


        _recoilTween.SetEase(Tween.EaseType.Out);
        _recoilTween.SetTrans(Tween.TransitionType.Spring);
        _recoilTween.TweenProperty(this, "PositionOffset", _initPositionOffset, _kickRecoverSpeed);

        _recoilTween.SetEase(Tween.EaseType.InOut);
        _recoilTween.Parallel().TweenProperty(this, "RotationOffset", _initRotationOffset * recoilMultiplier, _riseRecoverSpeed);
    }

   
    private void EjectChambered()
    {
        Chambered = false;
    }
}