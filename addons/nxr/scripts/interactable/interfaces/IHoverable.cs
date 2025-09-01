using Godot;
using System;

public interface IHoverable
{
    public bool IsHovered { get; set; }
    [Signal] public delegate void OnHoverEventHandler();  
    [Signal] public delegate void OnHoverExitEventHandler();  
}