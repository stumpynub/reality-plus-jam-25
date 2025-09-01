using Godot;
using System;

public partial class CamFollower : Node
{

    [Export] private Node3D _target;
    
    [Export] private Vector3 _offset = new Vector3(0, 1.5f, -2.0f);
    private Camera3D _camera;


    public override void _Ready()
    {
        _camera = GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        if (_camera == null || _target == null) return;
        Vector3 forward = -_camera.GlobalBasis.Z;

        forward.Y = 0; 
        Vector3 origin = _camera.GlobalPosition + (forward * _offset.Z);
        _target.GlobalPosition = origin;

        _target.LookAt(_camera.GlobalPosition, Vector3.Up, true); 
    }
}
