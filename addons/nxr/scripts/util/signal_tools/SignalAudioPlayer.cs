using System;
using Godot;
using Godot.Collections;
using NXR;


[GlobalClass]
public partial class SignalAudioPlayer : AudioStreamPlayer3D, ISignalTool
{
    [Export] public Node Node { get; set; }
    [Export] public string Signal { get; set; }


    public override void _Ready()
    {
		  SignalConnector.ConnectFromNode(Node, this, Signal); 
    }

	
    public void Action()
    {
		  Play(); 
    }
}

