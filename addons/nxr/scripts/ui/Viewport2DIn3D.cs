using System;
using Godot;

namespace NXR
{
    /// <summary>
    /// Handles rendering 2D UI in a 3D scene.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class Viewport2DIn3D : Node3D, IPointerInteractable
    {
        [Export]
        private Vector2I Size
        {
            get => _size;
            set
            {
                _size = value;
                UpdateScreen();
            }
        }

        [Export(PropertyHint.Range, "0.01, 100.0")]
        public float ScreenScale
        {
            get => _scale;
            set
            {
                _scale = value;
                UpdateScreen();
            }
        }

        [ExportGroup("Billboard Settings")]
        [Export] private BillboardMode _billboardMode = BillboardMode.Disabled;
        [Export] private float _billboardSmoothing = 0.5f;

        [ExportGroup("Pointer Settings")]
        [Export] public bool AllowPointer { get; set; } = true;

        public MeshInstance3D Screen { get; set; }
        public Pointer CurrentPointer { get; set; }
        public Pointer SecondaryPointer { get; set; }
        public SubViewport SubViewport { get; set; }
        public StaticBody3D CollisionObject { get; set; }

        private Control CurrentScene { get; set; }
        private float _scale = 1.0f;
        private Vector2I _size = new(512, 512);
        private Billboard3D _billboardNode;
        private double _timeSinceUpdate = 0;
        private StandardMaterial3D _screenMaterial;
        private Vector2 _prevPressedPos = Vector2.Zero;

        public override void _Ready() => Initialize();

        public override void _Process(double delta)
        {
            if (Engine.IsEditorHint()) return;

            ManageBillboard();
            GetCollisionShape().Disabled = !Visible;
        }

        private void Initialize()
        {
            _billboardNode = new Billboard3D { Target = this };
            AddChild(_billboardNode);

            if (!HasNode("%SubViewport")) return;

            SubViewport = GetNode<SubViewport>("%SubViewport");
            CollisionObject = GetNode<StaticBody3D>("%CollisionObject");
            Screen = GetNode<MeshInstance3D>("%Screen");

            _screenMaterial = Screen.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
            SubViewport.RenderTargetClearMode = SubViewport.ClearMode.Always;
            _screenMaterial.AlbedoTexture = SubViewport.GetTexture();

            if (SubViewport.GetChildCount() > 0 && SubViewport.GetChild(0) is Control scene)
                CurrentScene = scene;
        }

        private void ManageBillboard()
        {
            if (Engine.IsEditorHint()) return;
            _billboardNode.Mode = _billboardMode;
            _billboardNode.Smoothing = _billboardSmoothing;
        }

        private CollisionShape3D GetCollisionShape() =>
            CollisionObject.GetChild<CollisionShape3D>(0);

        private Vector2 GetVPLocalPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = CollisionObject.ToLocal(worldPoint);
            var shape = GetCollisionShape().Shape;
            Vector3 extents = shape switch
            {
                BoxShape3D box => box.Size * 0.5f,
                SphereShape3D sphere => Vector3.One * sphere.Radius,
                CapsuleShape3D capsule => new(capsule.Radius, capsule.Height * 0.5f + capsule.Radius, capsule.Radius),
                _ => Vector3.Zero
            };
            if (extents == Vector3.Zero)
            {
                GD.PrintErr("Unsupported shape type for pointer interaction.");
                return Vector2.Zero;
            }

            Vector3 scaledExtents = extents * GetCollisionShape().Scale;
            float u = Mathf.InverseLerp(-scaledExtents.X, scaledExtents.X, localPoint.X);
            float v = Mathf.InverseLerp(-scaledExtents.Y, scaledExtents.Y, localPoint.Y);
            var viewportSize = SubViewport.Size;
            return new Vector2(u * viewportSize.X, (1.0f - v) * viewportSize.Y);
        }

        private void UpdateScreen()
        {
            if (Screen == null || CollisionObject == null || SubViewport == null) return;

            float gcd = Gcd(Size.X, Size.Y);
            Vector2 ratio = new(Size.X * gcd, Size.Y * gcd);
            if (ratio.X > 1 || ratio.Y > 1)
                ratio /= Mathf.Max(ratio.X, ratio.Y);

            Vector3 ratioScale = new(ratio.X, ratio.Y, 1.0f);
            GetCollisionShape().Scale = Screen.Scale = ratioScale * ScreenScale;
            SubViewport.Size = Size;
        }

        private static float Gcd(int a, int b)
        {
            int result = Math.Min(a, b);
            while (result > 0)
            {
                if (a % result == 0 && b % result == 0) break;
                result--;
            }
            return (float)result / Math.Max(a, b);
        }

        public void PointerEntered(Pointer pointer)
        {
            if (CurrentPointer == null)
            {
                CurrentPointer = pointer;
                UpdateCursor(true, Vector2.Zero);
            }
            else
            {
                SecondaryPointer = pointer;
                UpdateSecondaryCursor(true, Vector2.Zero);
            }
        }

        public void PointerExited(Pointer pointer)
        {
            if (pointer == CurrentPointer) CurrentPointer = null;
            if (pointer == SecondaryPointer) SecondaryPointer = null;
            if (SecondaryPointer != null && CurrentPointer == null) CurrentPointer = SecondaryPointer;
            UpdateCursor(CurrentPointer != null, Vector2.Zero);
            UpdateSecondaryCursor(SecondaryPointer != null, Vector2.Zero);
        }

        public void Pressed(Pointer pointer, Vector3 where)
        {
            if (pointer == SecondaryPointer)
            {
                SecondaryPointer = CurrentPointer;
                CurrentPointer = pointer;
            }

            InputEventMouseButton clickEvent = new()
            {
                Pressed = true,
                ButtonIndex = MouseButton.Left,
                Position = GetVPLocalPoint(where)
            };
            SubViewport.PushInput(clickEvent);
            _prevPressedPos = clickEvent.Position;
        }

        public void Released(Pointer pointer, Vector3 where)
        {
            SubViewport.PushInput(new InputEventMouseButton
            {
                Pressed = false,
                ButtonIndex = MouseButton.Left,
                Position = GetVPLocalPoint(where)
            });
        }

        public void Moved(Pointer pointer, Vector3 where)
        {
            var pos = GetVPLocalPoint(where);
            SubViewport.PushInput(new InputEventMouseMotion
            {
                Position = pos,
                GlobalPosition = pos,
                Relative = pos - _prevPressedPos,
                Pressure = 1.0f,
                ButtonMask = MouseButtonMask.Left
            });
            _prevPressedPos = pos;

            if (pointer == CurrentPointer)
                UpdateCursor(true, pos);
            else
                UpdateSecondaryCursor(true, pos);
        }

        public void AxisChanged(Vector2 axis)
        {
            SubViewport.PushInput(new InputEventJoypadMotion
			{
				Axis = JoyAxis.LeftY,
				AxisValue = axis.Y, 
            });
        }

        private void UpdateCursor(bool visible, Vector2 pos)
        {
            if (Engine.IsEditorHint() || CurrentScene == null) return;
            if (CurrentScene.GetNodeOrNull<Sprite2D>("Cursor") is { } cursor)
            {
                cursor.Position = pos;
                cursor.Visible = visible;
            }
        }

        private void UpdateSecondaryCursor(bool visible, Vector2 pos)
        {
            if (Engine.IsEditorHint() || CurrentScene == null) return;
            if (CurrentScene.GetNodeOrNull<Sprite2D>("Cursor2") is { } cursor)
            {
                cursor.Position = pos;
                cursor.Visible = visible;
            }
        }
    }
}
