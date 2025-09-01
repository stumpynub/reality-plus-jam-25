using Godot;
using NXRInteractable;



[GlobalClass]
public partial class InteractableSlider : Interactable
{
    #region Exported Properties


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

        OnGrabbed += Grabbed;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        UpdateJoystickRotation();
    }
    #endregion

    #region Joystick Logic

    private void UpdateJoystickRotation()
    {
        if (PrimaryGrab.Interactor == null) return;
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