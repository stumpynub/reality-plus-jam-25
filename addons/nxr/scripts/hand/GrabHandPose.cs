using Godot;
using NXRInteractable;

namespace NXR;

[GlobalClass]
public partial class GrabHandPose : HandPoseBase
{

	#region Exported
	[Export] private Interactable _interactable;
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

		_interactable ??= Util.GetParentOrOwnerOfType<Interactable>(this);

		if (_interactable == null) return;

		_interactable.OnGrabbed += OnGrab;
		_interactable.OnDropped += OnDrop;
	}

	public override void _Process(double delta)
	{
		base._Process(delta); 
	}


	private void OnGrab(Interactable interactable, Interactor interactor)
	{
		if (GetHand(interactor) == null) return;
		Hand hand = GetHand(interactor);
		if (_grabType == GrabType.Primary)
		{
			if (interactor == _interactable.PrimaryGrab.Interactor)
			{
				Pose(hand, _pose);
			}
		}

		if (_grabType == GrabType.Secondary)
		{
			if (interactor == _interactable.SecondaryGrab.Interactor)
			{
				Pose(hand, _pose);
			}
		}
	}


	private void OnDrop(Interactable interactable, Interactor interactor)
	{
		if (_currentHand == null) return; 
		if (interactor != _currentHand.Interactor) return;

		ResetPose(); 
	}
}
