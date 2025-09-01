using Godot;
using NXRInteractable;



[GlobalClass]
public partial class InteractableLever : Interactable
{
    #region Exported Properties

    [Export] public float SnapDegree = 0.0f;
    [Export] public bool EnableClamp = true;
    [Export] public float MinPitch = -45.0f; // Minimum tilt angle for pitch (X-axis)
    [Export] public float MaxPitch = 45.0f;  // Maximum tilt angle for pitch (X-axis)
    [Export] public float MinRoll = -45.0f;  // Minimum tilt angle for roll (Z-axis)
    [Export] public float MaxRoll = 45.0f;   // Maximum tilt angle for roll (Z-axis)

    #endregion

    #region Private Fields

    private Vector3 _primaryGrab;
    private Basis _initBasis;
    private Vector3 _rotationAngles; // Stores pitch (X) and roll (Z)

    #endregion

    #region Godot Lifecycle Methods

    public override void _Ready()
    {
        base._Ready();

        _initBasis = GlobalBasis;

        OnGrabbed += Grabbed;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (PrimaryGrab.Interactor == null) return;

        UpdateJoystickRotation();
    }

    #endregion

    #region Joystick Logic

    private void UpdateJoystickRotation()
    {
        // Calculate the local grab position relative to the joystick
        Vector3 localGrab = PrimaryGrab.Interactor.GlobalPosition * Transform;

        // Remove the Y-axis component to focus on pitch (X) and roll (Z)
        localGrab.Y = 0.0f;

        // Calculate pitch (X-axis tilt) and roll (Z-axis tilt) based on the grab position
        float pitch = (_primaryGrab.Z - localGrab.Z) * localGrab.Y / Mathf.Abs(localGrab.Y);
        float roll = (_primaryGrab.X - localGrab.X) * localGrab.Y / Mathf.Abs(localGrab.Y);

        // Apply snapping if enabled
        if (SnapDegree > 0)
        {
            pitch = Mathf.Snapped(pitch, Mathf.DegToRad(SnapDegree));
            roll = Mathf.Snapped(roll, Mathf.DegToRad(SnapDegree));
        }

        // Update the rotation angles
        _rotationAngles = new Vector3(pitch, 0.0f, roll);

        // Clamp the rotation angles if clamping is enabled
        if (EnableClamp)
        {
            _rotationAngles.X = Mathf.Clamp(_rotationAngles.X, MinPitch, MaxPitch);
            _rotationAngles.Z = Mathf.Clamp(_rotationAngles.Z, MinRoll, MaxRoll);
        }

        // Apply the rotation to the joystick
        RotationDegrees = new Vector3(
            Mathf.RadToDeg(_rotationAngles.X),
            RotationDegrees.Y,
            Mathf.RadToDeg(_rotationAngles.Z)
        );
    }
    #endregion

    #region Event Handlers

    private void Grabbed(Interactable interactable, Interactor interactor)
    {
        if (interactor == interactable.PrimaryGrab.Interactor)
        {
            _primaryGrab = interactor.GlobalPosition * Transform;
        }
    }

    #endregion
}