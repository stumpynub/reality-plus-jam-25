using Godot;
using NXRInteractable;
using System;

public enum RotationAxis
{
    X,
    Y,
    Z
}

[GlobalClass]
public partial class InteractableWheel : Interactable
{
    #region Exported Properties

    [Export] public RotationAxis Axis = RotationAxis.X;
    [Export] public float SnapDegree = 0.0f;
    [Export] public bool EnableClamp = false;
    [Export] public float MinAngle = -45.0f;
    [Export] public float MaxAngle = 45.0f;
    [Export] public float SmoothingSpeed = 5.0f; // Higher values = faster smoothing

    #endregion

    #region Private Fields

    private Vector3 _initialGrabPosition;
    private float _targetRotationAngle;
    private float _currentRotationAngle;

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

        if (PrimaryGrab.Interactor == null) return;

        UpdateWheelRotation(delta);
    }

    #endregion

    #region Wheel Logic

    private void UpdateWheelRotation(double delta)
    {
        // Determine the grab position based on one or two interactors
        Vector3 localGrab;
        if (PrimaryGrab.Interactor != null && SecondaryGrab.Interactor != null)
        {
            // Average the positions of the two interactors
            Vector3 midpoint = (PrimaryGrab.Interactor.GlobalPosition + SecondaryGrab.Interactor.GlobalPosition) / 2;
            localGrab = ToLocal(midpoint);
        }
        else if (PrimaryGrab.Interactor != null)
        {
            localGrab = ToLocal(PrimaryGrab.Interactor.GlobalPosition);
        }
        else
        {
            return; // No interactors, exit early
        }

        // Remove the component of the grab position along the rotation axis
        localGrab = RemoveAxisComponent(localGrab, Axis);

        // Calculate the target rotation angle relative to the initial grab position
        _targetRotationAngle = CalculateRotationAngle(localGrab);

        // Normalize the angle to prevent large jumps
        _targetRotationAngle = Mathf.Wrap(_targetRotationAngle, MinAngle, MaxAngle);

        // Apply snapping if enabled
        if (SnapDegree > 0)
        {
            _targetRotationAngle = Mathf.Snapped(_targetRotationAngle, SnapDegree);
        }

        // Smoothly interpolate the current angle toward the target angle
        _currentRotationAngle = Mathf.Lerp(_currentRotationAngle, _targetRotationAngle, (float)(SmoothingSpeed * delta));

        // Clamp the rotation if enabled
        if (EnableClamp)
        {
            _currentRotationAngle = Mathf.Clamp(_currentRotationAngle, MinAngle, MaxAngle);
        }

        // Apply the smoothed rotation to the wheel
        SetRotationAngle(Axis, _currentRotationAngle);
    }

    private float CalculateRotationAngle(Vector3 localGrab)
    {
        // Calculate the angle relative to the initial grab position
        switch (Axis)
        {
            case RotationAxis.X:
                return Mathf.RadToDeg(Mathf.Atan2(localGrab.Z, localGrab.Y));
            case RotationAxis.Y:
                return Mathf.RadToDeg(Mathf.Atan2(localGrab.X, localGrab.Z));
            case RotationAxis.Z:
                return Mathf.RadToDeg(Mathf.Atan2(localGrab.X, localGrab.Y));
            default:
                return 0.0f;
        }
    }

    private void SetRotationAngle(RotationAxis axis, float angle)
    {
        RotationDegrees = axis switch
        {
            RotationAxis.X => new Vector3(angle, RotationDegrees.Y, RotationDegrees.Z),
            RotationAxis.Y => new Vector3(RotationDegrees.X, angle, RotationDegrees.Z),
            RotationAxis.Z => new Vector3(RotationDegrees.X, RotationDegrees.Y, angle),
            _ => RotationDegrees
        };
    }

    private Vector3 RemoveAxisComponent(Vector3 vector, RotationAxis axis)
    {
        return axis switch
        {
            RotationAxis.X => new Vector3(0.0f, vector.Y, vector.Z),
            RotationAxis.Y => new Vector3(vector.X, 0.0f, vector.Z),
            RotationAxis.Z => new Vector3(vector.X, vector.Y, 0.0f),
            _ => vector
        };
    }

    #endregion

    #region Event Handlers

    private void Grabbed(Interactable interactable, Interactor interactor)
    {
        if (interactor == interactable.PrimaryGrab.Interactor)
        {
            _initialGrabPosition = ToLocal(interactor.GlobalPosition);
        }
    }

    #endregion
}