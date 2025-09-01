using System;
using System.Collections.Generic;
using Godot;
using NXRInteractable;

namespace NXR
{
	[Tool]
	[GlobalClass]
	public partial class HandPoseBase : RemoteTransform3D
	{
		#region Pose Request Struct

		public struct PoseRequest(Hand hand, string pose, bool resetXform)
		{
			public Hand Hand = hand;
			public string Pose = pose;
			public bool ResetXform = resetXform;
		}

		#endregion

		#region Exported Properties

		[Export] protected GrabType _grabType = GrabType.Primary;
		[Export] protected bool _overridable = false;
		[Export] protected bool _snapToPose = true;



		[ExportGroup("Blend Settings")]
		[ExportSubgroup("BlendAnimations")]
		[Export] protected string _blend1 = "";
		[Export] protected string _blend2 = "";
		[Export] protected string _blend3 = "";


		[ExportSubgroup("BlendInputActions")]
		[Export] protected string _blend1InputAction = "";
		[Export] protected string _blend2InputAction = "";
		[Export] protected string _blend3InputAction = "";

		[ExportGroup("Pause Settings")]
		[Export] protected string _pausePose;
		[Export] protected bool _keepRemote = true;

		[ExportGroup("Mirror Settings")]
		[Export] protected bool _localScale = false;
		[Export] protected bool _mirrorX = true;
		[Export] protected bool _mirrorY = false;
		[Export] protected bool _mirrorZ = false;

		[ExportGroup("Animation Settings")]
		[Export] protected float _timeToPoseXform = 0.1f;

		#endregion

		#region Fields

		protected NodePath _lastPath;
		protected Vector3 _initScale;
		protected Transform3D _initXform;
		protected Transform3D _poseXform;
		protected Hand _currentHand;

		private readonly Dictionary<string, float> _oldFloatInputsValues = new();
		private readonly Dictionary<string, float> _newFloatInputValues = new();

		#endregion

		#region Godot Lifecycle

		public override void _Ready()
		{
			GlobalScale(Vector3.One);
			_initScale = Scale;
			_initXform = Transform;
		}

		public override void _Process(double delta)
		{
			if (Engine.IsEditorHint() && GetChildCount() > 0 && GetChild(0) is Node3D hand)
				hand.GlobalTransform = GlobalTransform;

			InterpolateInputs();
		}

		#endregion

		#region Pose Logic

		public virtual void Pose(Hand hand, string pose, bool resetXform = true)
		{
			if (hand.CurrentPoseBase != null && hand.CurrentPoseBase != this)
			{
				hand.CurrentPoseBase.RequestOverride(this, hand, new PoseRequest(hand, pose, resetXform));
				return;
			}

			hand.CurrentPoseBase = this; 
			_currentHand = hand;
			_poseXform = resetXform ? _initXform : Transform;

			ApplyMirrorIfNeeded();
			Transform = _poseXform;
			PoseTween(hand);

			if (_snapToPose)
			{
				_poseXform = hand.GlobalTransform;
				_lastPath = hand.GetPath();
				RemotePath = _lastPath;
			}
			else
			{
				hand.ResetXformTween();
			}

			hand.SetHandPose(this, pose, _blend1, _blend2);
		}

		private void ApplyMirrorIfNeeded()
		{
			if (_currentHand.Scale.X < 0)
			{
				float mx = _mirrorX ? -1 : 1;
				float my = _mirrorY ? -1 : 1;
				float mz = _mirrorZ ? -1 : 1;
				Vector3 mirror = new(mx, my, mz);

				_poseXform = _localScale
					? _poseXform.ScaledLocal(mirror)
					: _poseXform.Scaled(mirror);
			}
			else
			{
				_poseXform = _poseXform.Scaled(_initScale);
			}
		}

		public void PoseTween(Hand hand)
		{
			GlobalTransform = hand.GlobalTransform;

			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(this, "transform", _poseXform, _timeToPoseXform);
		}

		public void ResetPose()
		{
			if (_currentHand?.CurrentPoseBase != this)
				return;

			if (_currentHand.GetPath() == RemotePath)
				RemotePath = "";

			if (_currentHand.GetPath() == _lastPath)
				_lastPath = "";

			_currentHand.ResetHand();
			_currentHand = null;
		}
		
		public void Pause()
		{
			if (_lastPath == null)
				return;

			if (!_keepRemote)
				RemotePath = null;

			if (IsInstanceValid(GetNode(_lastPath)) && GetNode(_lastPath) is Hand hand)
			{
				hand.ResetHand(false);
				hand.SetHandPose(this, _pausePose);
			}
		}

		#endregion

		#region Input Handling

		private void InterpolateInputs()
		{
			if (_currentHand == null)
				return;

			foreach (var (key, value) in _currentHand.InterpolatedInputs)
			{
				if (key == _blend1InputAction) SetPoseBlendInput(1, (float)value);
				else if (key == _blend2InputAction) SetPoseBlendInput(2, (float)value);
				else if (key == _blend3InputAction) SetPoseBlendInput(3, (float)value);
			}
		}

		private void SetPoseBlendInput(int blendIndex, float value)
		{
			_currentHand?.SetPoseBlendInut(blendIndex, value);
		}

		#endregion

		#region Pose Override System

		public void RequestOverride(HandPoseBase to, Hand hand, PoseRequest request)
		{
			if (_overridable)
			{
				to.AcceptOverride(request);
				_currentHand = null; 
			}
		}

		public void AcceptOverride(PoseRequest request)
		{
			request.Hand.CurrentPoseBase = this;
			Pose(request.Hand, request.Pose, request.ResetXform);
		}

		#endregion

		#region Utility

		public Hand GetHand(Interactor interactor)
		{
			foreach (Node child in interactor.GetChildren())
			{
				if (child is Hand foundHand)
					return foundHand;
			}

			return null;
		}

		#endregion
	}
}
