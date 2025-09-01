using Godot;
using NXR;
using NXRInteractable;
using System;


[GlobalClass]
public partial class InteractableHoverVisualizer : Node
{
	[Export] Interactable _interactable;
	[Export] MeshInstance3D _mesh; 
	[Export] Material _material;

	private int _waitFrames = 0; 

	public override void _Ready()
	{
		_interactable ??= Util.GetParentOrOwnerOfType<Interactable>(this);
	}

	public override void _Process(double delta)
	{

		if (_interactable == null || _mesh == null) return;  

		if(_interactable.HoveredInteractors.Count > 0 && !_interactable.IsGrabbed()) { 
			_waitFrames = 0;
			_mesh.MaterialOverlay = _material; 
		}
		else { 
			_waitFrames += 1;
		}

		if (_waitFrames > 5) { 

			_mesh.MaterialOverlay = null; 
		}
	}
}
