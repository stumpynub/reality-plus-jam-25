using Godot;
using System;


[Tool]
[GlobalClass]
public partial class TwoPointCurve : BezierCurve3D
{
    [Export] Node3D _point1; 
    [Export] Node3D _point2; 


    public override void _Process(double delta)
    {

        if (_point1 == null || _point2 == null) return; 
        ClearPoints(); 
        
        Node3D parent = (Node3D)GetParent(); 
        Vector3 p1Local = ToLocal(_point1.GlobalPosition); 
        Vector3 p2Local = ToLocal(_point2.GlobalPosition); 

        ControlPoints.Add(p1Local); 
        ControlPoints.Add(p2Local); 
        UpdateCurve(); 
    }
}
