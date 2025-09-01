using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
[GlobalClass]
public partial class Line3D : MeshInstance3D
{
    public enum TextureTileMode { RATIO, DISTANCE }
    public enum BillboardMode { NONE, VIEW, Z }
    public enum MaterialType { SOLID, SOLID_UNLIT, MIX, MIX_UNLIT, ADD, CUSTOM }

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Width
    {
        get => _width;
        set
        {
            _width = value;
            Rebuild();
        }
    }

    [Export]
    public Curve WidthCurve
    {
        get => _widthCurve;
        set => SetWidthCurve(value);
    }

    [Export]
    public Gradient Gradient
    {
        get => _gradient;
        set => SetGradient(value);
    }

    [Export]
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            Rebuild();
        }
    }

    [Export] public bool UseGlobalSpace { get; set; } = false;
    [Export] public BillboardMode Billboard { get; set; } = BillboardMode.VIEW;
    [Export] public TextureTileMode TextureTile { get; set; } = TextureTileMode.RATIO;
    [Export] public float TextureOffset { get; set; } = 0.0f;
    [Export] public MaterialType MaterialTypeSetting { get; set; } = MaterialType.SOLID_UNLIT;
    [Export] public Material CustomMaterialSetting { get; set; }

    [Export]
    public Vector3[] Points
    {
        get => [.. _points];
        set
        {
            _points = value?.ToList() ?? [];
            Rebuild();
        }
    }

    [Export] public Vector3[] CurveNormals { get; set; } = Array.Empty<Vector3>();

    private Gradient _gradient;
    private Color _color = Colors.White;
    private Curve _widthCurve;
    private float _width = 0.05f;
    protected List<Vector3> _points = [];
    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Color[] _colors;
    private Vector2[] _uvs;
    private int[] _indices;
    private Godot.Collections.Array _arrays = [];
    private bool _autoRebuild = true;

    private static readonly Dictionary<MaterialType, ShaderMaterial> BuiltInMaterials = new();
    private static readonly Dictionary<MaterialType, ShaderMaterial> BuiltInBillboardMaterials = new();

    private void SetWidthCurve(Curve newCurve)
    {
        if (_widthCurve != null)
            _widthCurve.Changed -= OnWidthCurveChanged;

        _widthCurve = newCurve;

        if (_widthCurve != null)
            _widthCurve.Changed += OnWidthCurveChanged;

        Rebuild();
    }

    private void SetGradient(Gradient newGradient)
    {
        if (_gradient != null)
            _gradient.Changed -= OnGradientChanged;

        _gradient = newGradient;

        if (_gradient != null)
            _gradient.Changed += OnGradientChanged;

        Rebuild();
    }

    private void OnWidthCurveChanged()
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        Rebuild();
    }

    private void OnGradientChanged()
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        Rebuild();
    }

    public override void _Ready()
    {
        Rebuild();
    }

    public override void _ExitTree()
    {
        // Extra safety
        if (_widthCurve != null)
            _widthCurve.Changed -= OnWidthCurveChanged;

        if (_gradient != null)
            _gradient.Changed -= OnGradientChanged;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTransformChanged && UseGlobalSpace)
        {
            Rebuild();
        }
        else if (what == NotificationPredelete)
        {
            // Ensure delegates are detached before object is destroyed
            if (_widthCurve != null)
                _widthCurve.Changed -= OnWidthCurveChanged;
            if (_gradient != null)
                _gradient.Changed -= OnGradientChanged;
        }
    }

    public void AddPoint(Vector3 point)
    {
        _points.Add(point);
        Rebuild();
    }

    public void RemovePoint(int index)
    {
        if (index < 0 || index >= _points.Count) return;
        _points.RemoveAt(index);
        Rebuild();
    }

    public int GetPointCount() => _points.Count;

    public Vector3 GetPointPosition(int i)
    {
        if (i < 0 || i >= _points.Count) return Vector3.Zero;
        return _points[i];
    }

    public void UpdatePointPosition(int index, Vector3 position)
    {
        if (index > _points.Count - 1) return;
        _points[index] = position;
        Rebuild();
    }

    public void Clear()
    {
        Points = Array.Empty<Vector3>();
        CurveNormals = Array.Empty<Vector3>();
        _vertices = Array.Empty<Vector3>();
        _normals = Array.Empty<Vector3>();
        _colors = Array.Empty<Color>();
        _uvs = Array.Empty<Vector2>();
        _indices = [];
        Rebuild();
    }

    public void Rebuild()
    {
        if (!IsInsideTree() || !IsNodeReady()) return;

        if (Mesh is not ArrayMesh arrayMesh)
        {
            arrayMesh = new ArrayMesh();
            Mesh = arrayMesh;
        }
        else if (arrayMesh.GetSurfaceCount() > 0)
        {
            arrayMesh.ClearSurfaces();
        }

        arrayMesh.ResourceLocalToScene = true;

        int pointCount = Points.Length;
        if (pointCount < 2) return;

        _vertices = new Vector3[pointCount * 3];
        _normals = new Vector3[pointCount * 3];
        _colors = new Color[pointCount * 3];
        _uvs = new Vector2[pointCount * 3];

        _indices = new int[(pointCount - 1) * 12];
        for (int i = 0; i < pointCount - 1; i++)
        {
            int j = i * 12;
            int k = i * 3;
            _indices[j + 0] = k;
            _indices[j + 1] = k + 3;
            _indices[j + 2] = k + 1;
            _indices[j + 3] = k + 1;
            _indices[j + 4] = k + 3;
            _indices[j + 5] = k + 4;
            _indices[j + 6] = k + 1;
            _indices[j + 7] = k + 4;
            _indices[j + 8] = k + 2;
            _indices[j + 9] = k + 2;
            _indices[j + 10] = k + 4;
            _indices[j + 11] = k + 5;
        }

        Transform3D invGlobalTf = UseGlobalSpace ? GlobalTransform.AffineInverse() : default;

        float totalLength = 0.0f;
        for (int i = 1; i < pointCount; i++)
            totalLength += Points[i - 1].DistanceTo(Points[i]);

        float dist = 0.0f;
        for (int i = 0; i < pointCount; i++)
        {
            int j0 = i * 3;
            int j1 = j0 + 1;
            int j2 = j0 + 2;

            Vector3 p = Points[i];
            if (i > 0)
                dist += Points[i - 1].DistanceTo(p);

            float ratio = totalLength > 0 ? dist / totalLength : 0f;
            float u = TextureTile == TextureTileMode.RATIO ? ratio : dist;
            u += TextureOffset;

            float halfWidth = Width / 2;
            if (_widthCurve != null)
                halfWidth *= _widthCurve.SampleBaked(ratio);

            Vector3 tangent = (i == 0) ? (Points[i + 1] - p).Normalized()
                : (i == pointCount - 1) ? (p - Points[i - 1]).Normalized()
                : ((Points[i + 1] - Points[i - 1]) * 0.5f).Normalized();

            if (UseGlobalSpace)
            {
                p = invGlobalTf * p;
                tangent = invGlobalTf.Basis * tangent;
            }

            if (Billboard == BillboardMode.VIEW)
            {
                _vertices[j0] = p;
                _vertices[j1] = p;
                _vertices[j2] = p;
                _normals[j0] = _normals[j1] = _normals[j2] = tangent;
                _uvs[j0] = new Vector2(u, -halfWidth);
                _uvs[j1] = new Vector2(u, 0);
                _uvs[j2] = new Vector2(u, halfWidth);
            }
            else
            {
                Vector3 curveNormal = Vector3.Back.Cross(tangent).Normalized();
                Vector3 normal = Billboard == BillboardMode.Z ? Vector3.Back : tangent.Cross(curveNormal).Normalized();

                _vertices[j0] = p + curveNormal * halfWidth;
                _vertices[j1] = p;
                _vertices[j2] = p - curveNormal * halfWidth;
                _normals[j0] = _normals[j1] = _normals[j2] = normal;
                _uvs[j0] = new Vector2(u, 0);
                _uvs[j1] = new Vector2(u, 0.5f);
                _uvs[j2] = new Vector2(u, 1);
            }

            Color c = Color;
            if (_gradient != null)
                c *= _gradient.Sample(ratio);

            _colors[j0] = _colors[j1] = _colors[j2] = c;
        }

        _arrays.Resize((int)Mesh.ArrayType.Max);
        _arrays[(int)Mesh.ArrayType.Vertex] = _vertices;
        _arrays[(int)Mesh.ArrayType.Normal] = _normals;
        _arrays[(int)Mesh.ArrayType.Color] = _colors;
        _arrays[(int)Mesh.ArrayType.TexUV] = _uvs;
        _arrays[(int)Mesh.ArrayType.Index] = _indices;

        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _arrays);
        RefreshMaterial();
    }

    private void RefreshMaterial()
    {
        if (Mesh is not ArrayMesh am) return;

        ShaderMaterial mat = null;
        if (MaterialTypeSetting == MaterialType.CUSTOM)
        {
            mat = CustomMaterialSetting as ShaderMaterial;
            if (mat != null)
                mat.ResourceLocalToScene = true;
        }
        else
        {
            var dict = Billboard == BillboardMode.VIEW ? BuiltInBillboardMaterials : BuiltInMaterials;
            if (!dict.TryGetValue(MaterialTypeSetting, out mat))
            {
                string path = "res://addons/nxr/assets/shaders/line_3d_";
                if (Billboard == BillboardMode.VIEW)
                    path += "billboard_";
                path += MaterialTypeSetting.ToString().ToLower() + ".gdshader";
                mat = new ShaderMaterial { Shader = GD.Load<Shader>(path) };
                mat.ResourceLocalToScene = true;
                dict[MaterialTypeSetting] = mat;
            }
        }

        if (am.GetSurfaceCount() > 0 && am.SurfaceGetMaterial(0) != mat)
            am.SurfaceSetMaterial(0, mat);
    }
}
