using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class Trail3D : Line3D
{
    [Export(PropertyHint.Range, "0.001,1.0")]
    public float PointSpacing { get; set; } = 0.05f;

    [Export]
    public float MaxLength { get; set; } = 10f;

    [Export]
    public float FadeDuration { get; set; } = 1.5f;

    [Export]
    public bool EnableLerp { get; set; } = false;

    [Export(PropertyHint.Range, "0.01,50.0")]
    public float LerpSpeed { get; set; } = 5f;

    [Export(PropertyHint.Range, "2,512")]
    public int MaxPoints { get; set; } = 100;

    private List<float> _segmentLengths = new();
    private List<Vector3> _targetPositions = new();
    private float _currentLength = 0f;


    public override void _Ready()
    {
        base._Ready();
    }


    public override void _Process(double delta)
    {
        Vector3 pos = GlobalPosition;

        if (GetPointCount() == 0 || Points[^1].DistanceTo(pos) >= PointSpacing)
        {
            if (GetPointCount() > 0)
            {
                float segmentLength = Points[^1].DistanceTo(pos);
                _segmentLengths.Add(segmentLength);
                _currentLength += segmentLength;
            }


            if (GetPointCount() < MaxPoints)
            {
                AddPoint(pos);
                _targetPositions.Add(pos);
            }
        }

        if (EnableLerp)
        {
            UpdatePointPosition(0, pos);
            for (int i = GetPointCount() - 1; i > 0; i--)
            {
                Vector3 current = GetPointPosition(i);
                Vector3 target = GetPointPosition(i - 1);
                Vector3 next = current.Lerp(target, (float)delta * LerpSpeed);
                UpdatePointPosition(i, next);
            }
        }


        UpdateColorFade();
    }
    private void UpdateColorFade()
    {
        if (Gradient == null || FadeDuration <= 0f) return;

        Gradient = new Gradient();

        int count = GetPointCount();
        if (count < 2) return;

        float accumulatedLength = 0f;
        float total = Math.Max(_currentLength, 0.001f);

        Gradient.Offsets = new float[count];
        Gradient.Colors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            float t = 1f - Mathf.Clamp(accumulatedLength / total, 0f, 1f);
            Gradient.Offsets[i] = (float)i / (count - 1);
            Gradient.Colors[i] = new Color(Color, t);

            if (i < _segmentLengths.Count)
                accumulatedLength += _segmentLengths[i];
        }
    }
}
