using Godot;
using NXR;
using NXRFirearm;
using System;

[Tool]
[GlobalClass]
public partial class FirearmCylinder : FirearmPathInteractable
{
    #region Exported Properties

    [Export] public RotationAxis RotationAxis { get; set; } = RotationAxis.Y;
    [Export] private Node3D _bulletQueue;
    [Export] private Node3D _cylinderMesh;
    [Export] private int _bulletCount = 6;
    [Export] private FirearmHammer _hammer;

    [Export] private bool _exludeTriggerValue = false; 
    [Export] private bool _exludeHammerValue = false;

    [ExportGroup("Eject Settings")]
    [Export] private float _ejectThreshold = 0.7f;


    #endregion

    #region Private Fields

    private float _currentStep = 0;
    private float _prevStep = 0;
    private bool _triggerReset = true;
    private int _fireIndex = 0;
    private Vector3 _initRotation;
    private Tween _spinTween;
    
    #endregion

    #region Signals

    [Signal] public delegate void OnStepChangedEventHandler(int from, int to);

    #endregion

    #region Godot Lifecycle Methods

    public override void _Ready()
    {
        base._Ready();

        InitializeCylinder();

        if (Owner is Firearm firearm)
        {
            firearm.OnFire += OnFire;
        }

        OnStepChanged += StepChanged;
        GoToStart();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        RunTool();

        if (Firearm == null) return;

        HandleOpenCloseInput();
        UpdateFirearmState();
        HandleRotation();
        HandleTrigger();
    }

    #endregion

    #region Cylinder Logic

    private void InitializeCylinder()
    {
        if (_bulletQueue is FirearmBulletZoneQueue queue)
        {
            queue.DisableAll();
        }

        _initRotation = _cylinderMesh.Rotation;
    }

    private void HandleOpenCloseInput()
    {
        if (AtStart() && GetOpenInput())
        {
            Open();
            Spin(5, 1f);
        }

        if (AtEnd() && GetCloseInput())
        {
            Close();
        }
    }

    private void UpdateFirearmState()
    {
        Firearm.BlockFire = !AtStart();

        if (AtEnd() && _bulletQueue is FirearmBulletZoneQueue queue)
        {
            queue.EjectAll(true);
        }
    }

    private void HandleRotation()
    {
        Vector3 rotation = _initRotation;

        float rotationAmount = Mathf.Lerp(
            0,
            Mathf.DegToRad(360),
            Mathf.InverseLerp(0, _bulletCount, _currentStep)
        );

        switch (RotationAxis)
        {
            case RotationAxis.X:
                rotation.X += rotationAmount;
                break;
            case RotationAxis.Y:
                rotation.Y += rotationAmount;
                break;
            case RotationAxis.Z:
                rotation.Z += rotationAmount;
                break;
        }

        _currentStep = Mathf.Wrap(_currentStep, 0, _bulletCount);
        _prevStep = Mathf.Wrap(_prevStep, 0, _bulletCount);
        _cylinderMesh.Rotation = rotation;
    }

    private void HandleTrigger()
    {
        if (!AtStart()) return;


        if (_triggerReset && GetRotationValue() > 0)
        {
            _spinTween?.Kill();
            _currentStep = Mathf.Clamp(_prevStep + GetRotationValue(), _prevStep, _bulletCount);
        }

        if (GetRotationValue() >= 1 && _triggerReset)
        {
            _prevStep++;
            _triggerReset = false;
            EmitSignal(nameof(OnStepChanged), _prevStep - 1, _prevStep);
        }

        if (GetRotationValue() <= 0)
        {
            _triggerReset = true;
        }
    }

    #endregion

    #region Event Handlers

    private void StepChanged(int from, int to)
    {
        FirearmBullet bullet = GetBullet(from);
        Firearm.Chambered = bullet != null;

        if (bullet != null)
        {
            _fireIndex = from;
        }
    }

    private void OnFire()
    {
        FirearmBullet bullet = GetBullet(_fireIndex);
        if (bullet != null)
        {
            Firearm.ChamberedBullet = bullet;
            bullet.Spent = true;
        }
    }

    #endregion

    #region Utility Methods

    public FirearmBullet GetBullet(int index)
    {
        if (_bulletQueue is not FirearmBulletZoneQueue queue) return null;

        int step = Mathf.Wrap((_bulletCount - index) - 1, 0, _bulletCount);
        var zone = queue.GetZoneIndex(step);

        return zone?.IsLoaded() == true ? zone.Bullet : null;
    }

    private float GetRotationValue() { 
        float value = 0.0f; 
        float hammerValue = 0.0f;
        float triggerValue = Firearm.GetTriggerPullValue();

        if (_hammer is FirearmHammer hammer && !_exludeHammerValue) { 
            hammerValue = hammer.GetJoyValue();
        }

        if (_exludeTriggerValue) { 
            triggerValue = 0.0f; 
        }

        value = triggerValue + hammerValue;
        value = Mathf.Clamp(value, 0.0f, 1.0f);

        return value; 
    }

    public void Open()
    {
        if (_bulletQueue is FirearmBulletZoneQueue queue)
        {
            queue.EnableAll();
        }

        GoToEnd();
    }

    public void Close()
    {
        if (_bulletQueue is FirearmBulletZoneQueue queue)
        {
            queue.DisableAll();
        }
		
		_prevStep = _currentStep; 
        GoToStart();
    }

    public void Spin(int rotations, float speed, bool invert = false)
    {
        _spinTween = GetTree().CreateTween();
        _spinTween.SetEase(Tween.EaseType.Out);
        _spinTween.SetTrans(Tween.TransitionType.Cubic);

        float targetStep = invert
            ? _currentStep - rotations
            : _currentStep + rotations;
		
        _spinTween.TweenProperty(this, nameof(_currentStep), Mathf.RoundToInt(targetStep), speed);
    }

    private bool GetOpenInput()
    {
        if (Firearm.PrimaryGrab.Interactor == null) return false;

        Controller controller = Firearm.GetPrimaryController();
        Vector3 direction = -controller.Transform.Basis.X;

        return controller.IsButtonPressed("by_button") && controller.VelMatches(direction, 1f);
    }

    private bool GetCloseInput()
    {
        if (Firearm.PrimaryGrab.Interactor == null) return false;

        Controller controller = Firearm.GetPrimaryController();
        Vector3 direction = controller.Transform.Basis.X;

        return controller.VelMatches(direction, 1f);
    }

    #endregion
}