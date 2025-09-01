using System.Runtime.Serialization.Formatters;
using Godot;
using NXR;
using NXRInteractable;


[GlobalClass]
public partial class InteractableJointGrab : Generic6DofJoint3D
{

	[Export] private bool _snapToHand = false; 
	[Export] private float _grabbedDampening = 5.0f; 
	[Export] private GrabPointType _grabTpe = GrabPointType.Primary; 
	[Export] private Interactable _interactable;
	[Export] private CollisionShape3D _handColider; 
	[Export] private float angularStrength = 1.0f; 
	[Export] private float linearStrength = 1.0f; 

	public override void _Ready()
	{
		_interactable ??= Util.GetParentOrOwnerOfType<Interactable>(this);

		if (_interactable != null) { 
			_interactable.OnGrabbed += Grabbed; 
			_interactable.OnDropped += Dropped; 
		}
		
		Set("linear_spring_x/stiffness", angularStrength * 1000); 
		Set("linear_spring_y/stiffness", angularStrength * 1000); 
		Set("linear_spring_z/stiffness", angularStrength * 1000); 
		Set("angular_spring_x/stiffness", angularStrength * 1000); 
		Set("angular_spring_y/stiffness", angularStrength * 1000); 
		Set("angular_spring_z/stiffness", angularStrength * 1000); 
	}


    private void Grabbed(Interactable interactable, Interactor interactor) { 
		
		if (_grabTpe == GrabPointType.Primary && interactor == _interactable.SecondaryGrab.Interactor ) return; 
		
		
		if (_snapToHand) {
			Basis rot_offset = GlobalBasis.Inverse() * interactable.GlobalBasis; 
			interactable.GlobalBasis = rot_offset; 

			
			Vector3 pos_offset = GlobalPosition - interactable.GlobalPosition;  
			interactable.GlobalPosition = interactor.GlobalPosition - pos_offset; 
		}

		if (_grabTpe == GrabPointType.Primary && interactor == _interactable.PrimaryGrab.Interactor ) { 
			NodeB = _interactable.GetPath();
		}

		
		if (_grabTpe == GrabPointType.Secondary && interactor == _interactable.SecondaryGrab.Interactor ) { 
			NodeB = _interactable.GetPath();
		}

		interactable.LinearDamp = _grabbedDampening;
		interactable.AngularDamp = _grabbedDampening;

	}


	private void Dropped(Interactable interactable, Interactor interactor) { 
		if (_grabTpe == GrabPointType.Primary && interactor == _interactable.PrimaryGrab.Interactor ) { 
			NodeA = null;
			NodeB = null;
		}

		
		if (_grabTpe == GrabPointType.Secondary && interactor == _interactable.SecondaryGrab.Interactor ) { 
			NodeA = null;
			NodeB = null;
		}
	}
}
