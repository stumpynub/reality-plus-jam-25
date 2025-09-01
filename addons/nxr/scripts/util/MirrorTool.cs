using Godot;
using System;

[Tool]
[GlobalClass]
public partial class MirrorTool : Node
{
    [Export] public Node3D Node3D;
    [Export] public bool _children = false;

    [ExportToolButton("Flip X")]
    private Callable _tbFlipX => Callable.From(FlipX);



    private void FlipX()
    {
        if (_children)
        {
            foreach (Node child in Node3D.GetChildren())
            {
                if (child is Node3D childNode)
                {
                    childNode.Transform = childNode.Transform.ScaledLocal(new Vector3(-1, 1, 1));
                }
            }
        }
        else
        {
            Node3D.GlobalBasis = Node3D.GlobalBasis.Scaled(new Vector3(-1, 1, 1)).Orthonormalized();
        }
    }
}
