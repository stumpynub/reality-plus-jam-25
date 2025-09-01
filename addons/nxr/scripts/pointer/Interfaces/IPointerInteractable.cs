using Godot;
using System;

public interface IPointerInteractable 
{ 
	public Pointer CurrentPointer { get; set; }
	public Pointer SecondaryPointer { get; set; }
	
	public void PointerEntered(Pointer pointer); 
	public void PointerExited(Pointer pointer); 
	public void Pressed(Pointer pointer, Vector3 where); 	
	public void Released(Pointer pointer, Vector3 where); 	
	public void Moved(Pointer pointer, Vector3 where); 	
}