using Godot;


[Tool]
[GlobalClass]
public partial class PolygonCircle : CsgPolygon3D
{
    [Export] private float _radius = 5.0f;
    [Export(PropertyHint.Range, "3, 50")] private int _resolution = 10;

    [Export] bool _autoUpdate = true;

    [ExportToolButton("Generate")] public Callable GenerateButton => Callable.From(Generate);

    
    public override void _Process(double delta)
    {
        if (!_autoUpdate) return;

        Godot.Vector2[] circle = new Godot.Vector2[_resolution];

        for (int i = 0; i < _resolution; i++)
        {
            float pointRadius = _radius;


            float x = (pointRadius / 100) * Mathf.Cos(Mathf.Pi * 2 * i / _resolution);
            float y = (pointRadius / 100) * Mathf.Sin(Mathf.Pi * 2 * i / _resolution);
            Vector2 point = new Vector2(x, y);

            circle.SetValue(point, i);
        }

        Polygon = circle;
    }

    public void Generate()
    {
        Godot.Vector2[] circle = new Godot.Vector2[_resolution];

        for (int i = 0; i < _resolution; i++)
        {
            float pointRadius = _radius;


            float x = (pointRadius / 100) * Mathf.Cos(Mathf.Pi * 2 * i / _resolution);
            float y = (pointRadius / 100) * Mathf.Sin(Mathf.Pi * 2 * i / _resolution);
            Vector2 point = new Vector2(x, y);

            circle.SetValue(point, i);
        }

        Polygon = circle;
    }
}
