using Godot;
using NXRInteractable;

namespace NXR;

[GlobalClass]
public partial class SnapZoneHandPose : HandPoseBase
{

	#region Exported
	[Export] private InteractableSnapZone _snapZone;
	[Export] private string _pose = "";
	#endregion


	#region Private 
	private Vector3 _initScale;
	private Transform3D _poseXform; 
	#endregion



	public override void _Ready()
	{	
		if (Engine.IsEditorHint()) return;
		
		base._Ready(); 

		_snapZone ??= Util.GetParentOrOwnerOfType<InteractableSnapZone>(this);


		_snapZone.OnSnapStart += OnSnap;
		_snapZone.OnSnapExit += OnSnapExit;
	}

	public override void _Process(double delta)
	{
		base._Process(delta); 
	}


	private void OnSnap(Interactable interactable)
	{
		if (interactable.PrimaryGrab.Interactor == null) return; 
		
		Interactor interactor = interactable.PrimaryGrab.Interactor;

		if (GetHand(interactor) == null) return;
		Hand hand = GetHand(interactor);
		
		Pose(hand, _pose);
	
	}


	private void OnSnapExit(Interactable interactable)
	{
		if (_currentHand == null) return; 

		ResetPose(); 
	}
}
