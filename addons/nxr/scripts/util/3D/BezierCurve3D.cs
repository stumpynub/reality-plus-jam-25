using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class BezierCurve3D : Path3D
{
    [Export] public bool update = true;
    [Export] public float UpdateTime = 0.1f; 
    [Export] public int Resolution = 10;
    [Export] public Array<Vector3> ControlPoints = new Array<Vector3> { Vector3.Zero, new Vector3(0, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), Vector3.Forward };

    [ExportGroup("Animate")]
    [Export]
    public float SinSpeed = 0.0f;
    [Export(PropertyHint.Range, "0.0, 5")]
    public float SinAmplitude = 0.0f;
    [Export]
    public float CosSpeed = 0.0f;
    [Export(PropertyHint.Range, "0.0, 5")]
    public float CosAmplitude = 0.0f;

    private float _sinTime = 0.0f;
    private float _cosTime = 0.0f;
    private float _throttle = 0.0f; 
    private float _throttleAmount = 5.0f; 


    public override void _Process(double delta)
    {
        if (!update) return;

        _sinTime += (float)delta; 
        _cosTime += (float)delta; 
        UpdateCurve(); 
    }


    public void UpdateCurve()
    {
        Curve.ClearPoints();
        Curve.UpVectorEnabled = true;
        Curve.ResourceLocalToScene = true;

        for (int i = 0; i < Resolution; i++)
        {
            float t = (float)i / (Resolution - 1);

            Vector3 point = GetCurve(t);
            Vector3 sin = Vector3.Up * Mathf.Sin(i + (SinSpeed * _sinTime)) * SinAmplitude;
            Vector3 cos = Vector3.Right * Mathf.Cos((i + _cosTime) * CosSpeed) * CosAmplitude;

            Curve.AddPoint(point + sin + cos);
        }
    }


    public void ClearPoints()
    {
        ControlPoints.Clear(); 
    }

    
    public Vector3 GetFirstPoint() { 
        return ControlPoints[0]; 
    }

    public Vector3 GetLastPoint() { 
        return ControlPoints[^1]; 
    }

    // Generalized Bézier curve calculation
    public Vector3 GetCurve(float t)
    {
        if (ControlPoints.Count == 0)
            return Vector3.Zero;

        Vector3 result = Vector3.Zero;
        int n = ControlPoints.Count - 1;

        for (int i = 0; i <= n; i++)
        {
            float binomialCoefficient = BinomialCoefficient(n, i);
            float term = binomialCoefficient * Mathf.Pow(1 - t, n - i) * Mathf.Pow(t, i);
            result += term * ControlPoints[i];
        }

        return result;
    }

    // Calculate binomial coefficient (n choose k)
    private float BinomialCoefficient(int n, int k)
    {
        return Factorial(n) / (Factorial(k) * Factorial(n - k));
    }

    // Calculate factorial
    private float Factorial(int num)
    {
        if (num <= 1) return 1;
        float result = 1;
        for (int i = 2; i <= num; i++)
        {
            result *= i;
        }
        return result;
    }
}