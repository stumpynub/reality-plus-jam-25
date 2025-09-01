using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using NXRInteractable;

namespace NXR;

public enum DirectionalAxis
{
	X,
	Y,
	Z
}


[Tool]
[GlobalClass]
public partial class HandDirectionalPose : HandPoseBase
{

	[Export] private Interactable _interactable;
	[Export] private bool _excludeX = false;
	[Export] private bool _excludeY = false;
	[Export] private bool _excludeZ = false;
	[Export] private DirectionalAxis _axis = DirectionalAxis.Z;

	private GrabType _grabType = GrabType.Primary;
	private float _tolerance = 0.3f;

	[Export] private string _positivePose;
	[Export] private string _middlePosPose;
	[Export] private string _middleNegPose;
	[Export] private string _negativePose;


	[Export] private Transform3D _xPosXform;
	[Export] private Transform3D _xNegXform;

	[Export] private Transform3D _yPosXform;
	[Export] private Transform3D _yNegXform;


	[Export] private Transform3D _zPosXform;
	[Export] private Transform3D _zNegXform;


	[ExportGroup("Transform Settings")]
	[Export]
	private bool SetPositiveTransform
	{
		get
		{
			return false;
		}
		set
		{
			_positiveTransform = Transform;
		}
	}


	[Export]
	private bool GoPositiveTransform
	{
		get
		{
			return false;
		}
		set
		{
			Transform = _positiveTransform;
		}
	}

	[Export] private Transform3D _positiveTransform;


	[Export]
	private bool SetMiddlePosTransform
	{
		get
		{
			return false;
		}
		set
		{
			_middlePosTransform = Transform;
		}
	}


	[Export]
	private bool GoMiddlePosTransform
	{
		get
		{
			return false;
		}
		set
		{
			Transform = _middlePosTransform;
		}
	}
	[Export] private Transform3D _middlePosTransform;



	[Export]
	private bool SetMiddleNegTransform
	{
		get
		{
			return false;
		}
		set
		{
			_middleNegTransform = Transform;
		}
	}


	[Export]
	private bool GoMiddleNegTransform
	{
		get
		{
			return false;
		}
		set
		{
			Transform = _middleNegTransform;
		}
	}
	[Export] private Transform3D _middleNegTransform;





	[Export]
	private bool SetNegativeTransform
	{
		get
		{
			return false;
		}
		set
		{
			_negativeTransform = Transform;
		}
	}


	[Export]
	private bool GoNegativeTransform
	{
		get
		{
			return false;
		}
		set
		{
			Transform = _negativeTransform;
		}
	}

	private string _lastPose = "";
	[Export] private Transform3D _negativeTransform;
	[Export] private DirectionalAxis _handAxis = DirectionalAxis.Z;


	public override void _Ready()
	{
		base._Ready();

		if (_interactable == null && Util.NodeIs(GetParent(), typeof(Interactable)))
		{
			_interactable = (Interactable)GetParent();
		}

		if (_interactable == null) return;

		_interactable.OnGrabbed += OnGrab; 
		_interactable.OnDropped += OnDrop;
	}


	public override void _Process(double delta)
	{
		base._Process(delta);


		if (_interactable.IsGrabbed())
		{
			Interactor interactor = _grabType == GrabType.Primary
				? _interactable.PrimaryGrab.Interactor
				: _interactable.SecondaryGrab.Interactor;

			if (interactor != null)
				UpdatePose(interactor);
		}
	}

	private void UpdatePose(Interactor interactor)
	{
		if (GetHand(interactor) == null) return;

		Vector3 axisVec = _axis switch
		{
			DirectionalAxis.X => _interactable.GlobalTransform.Basis.X,
			DirectionalAxis.Y => _interactable.GlobalTransform.Basis.Y,
			DirectionalAxis.Z => -_interactable.GlobalTransform.Basis.Z,
			_ => Vector3.Zero,
		};

		Vector3 handVec = _handAxis switch
		{
			DirectionalAxis.X => interactor.GlobalTransform.Basis.X,
			DirectionalAxis.Y => interactor.GlobalTransform.Basis.Y,
			DirectionalAxis.Z => interactor.GlobalTransform.Basis.Z,
			_ => interactor.GlobalTransform.Basis.Z,
		};

		float dot = handVec.Dot(axisVec);
		string pose = dot > _tolerance ? _positivePose :
					  dot < -_tolerance ? _negativePose :
					  dot > 0 ? _middlePosPose :
								_middleNegPose;

		Transform3D targetXform = pose == _positivePose ? _positiveTransform :
								  pose == _negativePose ? _negativeTransform :
								  pose == _middlePosPose ? _middlePosTransform :
														   _middleNegTransform;

		Hand hand = GetHand(interactor);
		if (pose != _lastPose)
		{
			Transform = targetXform;
			Pose(hand, pose, false);
			_lastPose = pose;
		}
	}


	private void OnGrab(Interactable interactable, Interactor interactor)
	{

	}
	
	private void OnDrop(Interactable interactable, Interactor interactor)
	{

		if (GetHand(interactor) == null) return;

		Hand hand = GetHand(interactor);

		if (hand.GetPath() == RemotePath)
		{
			RemotePath = "";
			hand.ResetHand();
		}

		if (hand.GetPath() == _lastPath)
		{
			_lastPath = "";
			hand.ResetHand();
		}

		Transform = _initXform;
	}

}
