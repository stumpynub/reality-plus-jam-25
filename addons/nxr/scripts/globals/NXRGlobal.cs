using Godot;
using System;

public partial class NXRGlobal : Node
{
    public static NXRGlobal Instance { get; private set; }
    public Node3D StageRoot { get; set; }

    public override void _Ready()
    {
        Instance = this; 
    }
}
