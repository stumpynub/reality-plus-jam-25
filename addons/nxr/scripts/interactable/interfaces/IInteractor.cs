using Godot;
using NXRInteractable;
using System;


namespace NXR;

public interface IInteractor
{

    [Signal] public delegate void OnGrabbedEventHandler(Interactable interactable);
	[Signal] public delegate void OnDroppedEventHandler(Interactable interactable);
    [Signal] public delegate void OnHoverEnteredEventHandler(Interactable interactable);
    [Signal] public delegate void OnHoverExitedEventHandler(Interactable interactable);

    public Interactable GrabbedInteractable { get; set; }

    public void Grab(Interactable interactable);
    public void Drop(); 
}