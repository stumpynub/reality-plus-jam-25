using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using NXR;

namespace NXRInteractable;


[GlobalClass]
public partial class Interactor : Area3D, IInteractor
{


	public Interactable GrabbedInteractable { get; set; }


	[Signal] public delegate void OnGrabbedEventHandler(Interactable interactable);
	[Signal] public delegate void OnDroppedEventHandler(Interactable interactable);
	[Signal] public delegate void OnHoverEnteredEventHandler(Interactable interactable);
	[Signal] public delegate void OnHoverExitedEventHandler(Interactable interactable);


	public virtual void Grab(Interactable interactable)
	{
		if (interactable.Disabled) return;

		GrabbedInteractable = interactable;
		interactable.Grab(this);


		EmitSignal(nameof(OnGrabbed), interactable);
	}



	public virtual void Drop()
	{
		if (GrabbedInteractable == null) return; 
		
		Interactable interactable = GrabbedInteractable;

		GrabbedInteractable.Drop(this);
		GrabbedInteractable = null;
		
		EmitSignal(nameof(OnDropped), interactable);
	}
}