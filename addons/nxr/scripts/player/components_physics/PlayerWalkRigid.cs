using Godot;
using System;

namespace NXRPlayer; 

[GlobalClass]
public partial class PlayerWalkRigid : PlayerBehaviourRigid
{   

    [Export]
    private MovementDirection _movementDirection = MovementDirection.Camera; 

    [Export]
    private float _walkSpeed = 50.0f;


    public override void _PhysicsProcess(double delta)
    {
        _player.Accelerate(WalkDirection() * _walkSpeed * (float)delta);
    }

    private Vector3 WalkDirection()
    {
        Vector2 axis = _player.GetSecondaryJoyAxis();
        float axisLength = _player.GetSecondaryJoyAxis().Length(); 
        Vector3 right = new Vector3(GetMovementBasis().X.X, 0, GetMovementBasis().X.Z) * axis.X;
        Vector3 forward = new Vector3(-GetMovementBasis().Z.X, 0, -GetMovementBasis().Z.Z) * axis.Y;

        Vector3 movementDirection = (right + forward).Normalized() * axisLength;

        return movementDirection.Slide(_player.GetGroundNormal()); 
    }

    private Basis GetMovementBasis()
    {   
        if (_movementDirection == MovementDirection.Camera) { 
            return this._player.GetCamera().GlobalTransform.Basis; 
        }

        return this._player.GetSecondaryController().GlobalTransform.Basis; 
    }
}
