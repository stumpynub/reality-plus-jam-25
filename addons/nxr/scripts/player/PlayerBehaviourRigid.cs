using Godot;
using NXR;
using System;

namespace NXRPlayer; 
public partial class PlayerBehaviourRigid : Node
{

    protected PlayerRigid _player;

    public override void _Ready()
    {
        if (Util.NodeIs(GetParent(), typeof(RigidBody3D)))
        {
            _player = (PlayerRigid)GetParent(); 
        } else
        {
            GD.PushWarning("No player body found!"); 
            GD.Print(GetParent()); 
        }
    }
}
