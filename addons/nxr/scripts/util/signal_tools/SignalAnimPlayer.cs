using System;
using Godot;
using NXR;


[GlobalClass]
public partial class SignalAnimPlayer : AnimationPlayer, ISignalTool
{

    [Export] public Node Node { get; set; }
    [Export] public string Signal { get; set; }
    
    [Export] private string _animationName; 
    [Export] private bool _resetOnfinished = false; 

  
    

    public override void _Ready()
    {
        SignalConnector.ConnectFromNode(Node, this, Signal); 

        AnimationFinished += Finished; 
    }

    public void Action()
    {
        Play(_animationName); 
    }

    private void Finished(StringName anim)
    {
        if (anim == _animationName && _resetOnfinished)
        {
            Play("RESET"); 
        }
    }
}

