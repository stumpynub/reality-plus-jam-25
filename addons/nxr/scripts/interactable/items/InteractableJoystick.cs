using System;
using System.Runtime.ExceptionServices;
using Godot;
using NXR;
using NXRInteractable;



[Tool]
[GlobalClass]
public partial class InteractableJoystick : Interactable
{
    #region Exported Properties

    [Export] private bool _returnOnDrop = false;
    [Export] public float X;
    [Export] public float Y;
    [Export] public float Z;
    [Export] private float _xSnapDegree = 0;
    [Export] private float _zSnapDegree = 0;



    [Export] float _positionMultiplier = 1.0f; 

    [Export]
    public float XRatio
    {
        get
        {
            return Mathf.Clamp(_xRatio, 0.0f, 1.0f);
        }
        set
        {
            _xRatio = Mathf.Clamp(value, 0.0f, 1.0f);
            X = Mathf.DegToRad(Mathf.Lerp(_xClampMin, _xClampMax, _xRatio));
        }
    }


    [Export]
    public float ZRatio
    {
        get
        {
            return Mathf.Clamp(_zRatio, 0.0f, 1.0f);
        }
        set
        {
            _zRatio = Mathf.Clamp(value, 0.0f, 1.0f);
            Z = Mathf.DegToRad(Mathf.Lerp(_zClampMin, _zClampMax, _zRatio));
        }
    }



    [ExportGroup("Clamp Settings")]
    [Export] private bool _enableClamp = true;
    [Export(PropertyHint.Range, "-360, 0")] private float _xClampMin = -45;
    [Export(PropertyHint.Range, "0, 360")] private float _xClampMax = 45;
    [Export(PropertyHint.Range, "-360, 0")] private float _zClampMin = -45;
    [Export(PropertyHint.Range, "0, 360")] private float _zClampMax = 45;
    #endregion

    #region Private Fields

    private float _xRatio = 0.0f;
    private float _zRatio = 0.0f;
    private Vector3 _initRotation = Vector3.Zero;
    private Vector3 _primaryGrab;
    private Basis _initBasis;
    private Vector3 _rotationAngles; // Stores pitch (X) and roll (Z)
    private Vector3 _startLocGrab;
    private Vector3 _startLocGrabSeconodary;
    private Tween _returnTween;
    #endregion




    #region Godot Lifecycle Methods
    public override void _Ready()
    {
        base._Ready();

        _initBasis = GlobalBasis;
        _initRotation = GlobalRotation;
        OnGrabbed += Grabbed;
        OnFullDropped += Dropped;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        UpdateJoystickRotation();
    }
    #endregion

    private void UpdateJoystickRotation()
    {
        if (PrimaryGrab.Interactor != null)
        {
            
            Vector3 locPos = (ToLocal(PrimaryGrab.Interactor.GlobalPosition) - _startLocGrab) * _positionMultiplier;

            X += locPos.Z;
            Z -= locPos.X;

        }

        _xRatio = Mathf.InverseLerp(_xClampMin, _xClampMax, Mathf.RadToDeg(X));
        _zRatio = Mathf.InverseLerp(_zClampMin, _zClampMax, Mathf.RadToDeg(Z));

        if (_enableClamp)
        {
            X = Mathf.Clamp(Mathf.Snapped(X, Mathf.DegToRad(-_xSnapDegree)), Mathf.DegToRad(_xClampMin), Mathf.DegToRad(_xClampMax));
            Z = Mathf.Clamp(Mathf.Snapped(Z, Mathf.DegToRad(_zSnapDegree)), Mathf.DegToRad(_zClampMin), Mathf.DegToRad(_zClampMax));
        }


        if (_xClampMin == 0 && _xClampMax == 0)
        {
            X = 0.0f;
        }

        if (_zClampMin == 0 && _zClampMax == 0)
        {
            Z = 0.0f;
        }

        
        Transform3D t = Transform;
        t.Basis = Basis.FromEuler(new Vector3(X, Y, Z)).Orthonormalized();
        Transform = t;
    }


    private void Grabbed(Interactable interactable, Interactor interactor)
    {
        if (interactor == interactable.PrimaryGrab.Interactor)
        {
            _primaryGrab = interactor.GlobalPosition * Transform;
            _startLocGrab = ToLocal(interactor.GlobalPosition);
        }

        if (interactor == interactable.SecondaryGrab.Interactor)
        {
            _startLocGrabSeconodary = ToLocal(interactor.GlobalPosition);
        }

        _returnTween?.Kill();
    }

    private void Dropped()
    {
        if (!_returnOnDrop) return;

        _returnTween?.Kill();
        _returnTween = CreateTween();
        _returnTween.SetParallel(true);
        _returnTween.SetEase(Tween.EaseType.Out);
        _returnTween.SetTrans(Tween.TransitionType.Elastic);
        _returnTween.TweenProperty(this, "X", 0.0f, 0.5f);
        _returnTween.TweenProperty(this, "Y", 0.0f, 0.5f);
        _returnTween.TweenProperty(this, "Z", 0.0f, 0.5f);

    }
}