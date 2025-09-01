using System;
using System.Collections.Generic;
using Godot;
using NXRInteractable;

namespace NXR;

[GlobalClass]
public partial class Hand : RigidBody3D
{

	public enum HandState
	{
		Idle, 
		Posed
	}

	#region Exported 
	[Export] public XRControllerInteractor Interactor;
	[Export] private AnimationTree _animTree;


	[ExportGroup("Input Settings")]
	[Export(PropertyHint.Range, "0.0, 1.0, 0.01")] private float _inputInterpolation = 0.25f; 
	#endregion


	#region  Public
	public HandState CurrentState = Hand.HandState.Idle;
	public HandPoseBase CurrentPoseBase; 
	#endregion


	#region  Private 
	protected Transform3D _initTransform;
	protected Vector3 _initRotation;
	protected Vector3 _initOffset;
	protected AnimationTree _currentAnimTree;
	protected string _lastBlendName;
	protected string _poseSpaceName = "PoseSpace";
	private Tween _resetTween;
	public Dictionary<String, float> InterpolatedInputs = new();
	private Dictionary<String, float> _newFloatInputValues = new();
	#endregion


	[Signal] public delegate void HandPoseChangedEventHandler(HandPoseBase poseBase, Hand hand); 


	public override void _Ready()
	{
		_initTransform = Transform.Orthonormalized();
		_initRotation = _initTransform.Basis.GetRotationQuaternion().GetEuler();

		if (IsInstanceValid(Interactor))
		{
			_initOffset = Interactor.GlobalPosition - GlobalPosition;
		}

		if (IsInstanceValid(_animTree))
		{
			_currentAnimTree = _animTree;
		}

		if (IsInstanceValid(Interactor))
		{
			Interactor.Controller.InputFloatChanged += InputFloat;
			Interactor.Controller.InputVector2Changed += InputVec2;
			Interactor.Controller.ButtonPressed += ButtonPresed;
			Interactor.Controller.ButtonReleased += ButtonReleased;
		}
	}


	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;

		foreach (string key in InterpolatedInputs.Keys)
		{
			InterpolatedInputs[key] = Mathf.Lerp(
				InterpolatedInputs[key],
				_newFloatInputValues[key],
				_inputInterpolation
			);

			_animTree.Set(string.Format("parameters/IdleTree/{0}/blend_amount", key), InterpolatedInputs[key]);
			_animTree.Set(string.Format("parameters/IdleTree/{0}/blend_position", key), InterpolatedInputs[key]);
			_animTree.Set(string.Format("parameters/PoseTree/{0}/blend_position", key), InterpolatedInputs[key]);
		}
	}


	public void SetHandPose(HandPoseBase poseBase, String pose, string sub1 = "", string sub2 = "", string sub3 = "")
	{
		ResetPoseTree();
		_resetTween?.Stop();

		AnimationNodeAnimation blendAnim = (AnimationNodeAnimation)GetPoseTree().GetNode("DefaultPose");
		List<AnimationNodeAnimation> subAnims =
		[
			GetPoseTree().GetNode("BlendAnim1") as AnimationNodeAnimation,
			GetPoseTree().GetNode("BlendAnim2") as AnimationNodeAnimation,
			GetPoseTree().GetNode("BlendAnim3") as AnimationNodeAnimation,
		];

		if (sub1 != "" && _animTree.HasAnimation(sub1)) subAnims[0].Animation = sub1;
		if (sub2 != "" && _animTree.HasAnimation(sub2)) subAnims[1].Animation = sub2;
		if (sub3 != "" && _animTree.HasAnimation(sub3)) subAnims[2].Animation = sub3;
		blendAnim.Animation = pose;

		GetPlayback().Travel("PoseTree");
		CurrentState = HandState.Posed;

		EmitSignal(nameof(HandPoseChanged), poseBase, this); 
	}


	public void ResetPoseTree(bool resetAnims=false) { 
		AnimationNodeAnimation blendAnim = (AnimationNodeAnimation)GetPoseTree().GetNode("DefaultPose"); 
		List<AnimationNodeAnimation> subAnims =
        [
            GetPoseTree().GetNode("BlendAnim1") as AnimationNodeAnimation,
            GetPoseTree().GetNode("BlendAnim2") as AnimationNodeAnimation,
            GetPoseTree().GetNode("BlendAnim3") as AnimationNodeAnimation,
            GetPoseTree().GetNode("BlendAnim4") as AnimationNodeAnimation,
            GetPoseTree().GetNode("BlendAnim5") as AnimationNodeAnimation,
        ]; 

		SetPoseBlendInut(1, 0.0f); 
		SetPoseBlendInut(2, 0.0f); 
		SetPoseBlendInut(3, 0.0f); 
		SetPoseBlendInut(4, 0.0f); 
		SetPoseBlendInut(5, 0.0f); 

		if (!resetAnims) return;
		
		foreach (AnimationNodeAnimation subAnim in subAnims)
		{
			subAnim.Animation = "RESET";
		}

		blendAnim.Animation = "RESET";
	}


	public void ResetHand(bool resetTransform = true)
	{
		if (resetTransform)
		{
			ResetXformTween();
		}

		ResetPoseTree();
		GetPlayback().Travel("IdleTree");
		CurrentState = HandState.Idle;
		CurrentPoseBase = null;
	}


	private void InputFloat(String inputName, double value)
	{
		if (!InterpolatedInputs.ContainsKey(inputName)) InterpolatedInputs.Add(inputName, 0.0f);
		if (!_newFloatInputValues.ContainsKey(inputName)) _newFloatInputValues.Add(inputName, (float)value);
		else _newFloatInputValues[inputName] = (float)value; 
	}


	private void InputVec2(String inputName, Vector2 value)
	{
		_animTree.Set(string.Format("parameters/PoseTree/{0}/blend_position", inputName), value);
	}


	private void ButtonPresed(string button)
	{
		if (!InterpolatedInputs.ContainsKey(button)) InterpolatedInputs.Add(button, 0.0f);
		if (!_newFloatInputValues.ContainsKey(button)) _newFloatInputValues.Add(button, 1.0f);
		else _newFloatInputValues[button] = 1.0f; 

	}


	private void ButtonReleased(string button)
	{
		if (_newFloatInputValues.ContainsKey(button)) _newFloatInputValues[button] = 0f; 
	}


	public AnimationNodeBlendTree GetPoseTree()
	{
		AnimationNodeStateMachine stateMachine = (AnimationNodeStateMachine)_animTree.TreeRoot;
		AnimationNodeBlendTree poseTree = (AnimationNodeBlendTree)stateMachine.GetNode("PoseTree");
		return poseTree;
	}


	public AnimationNodeBlendTree GetIdleTree()
	{
		AnimationNodeStateMachine stateMachine = (AnimationNodeStateMachine)_animTree.TreeRoot;
		AnimationNodeBlendTree poseTree = (AnimationNodeBlendTree)stateMachine.GetNode("IdleTree");
		return poseTree;
	}


	private AnimationNodeStateMachine GetBaseStateMachine()
	{
		return (AnimationNodeStateMachine)_animTree.TreeRoot;
	}
	

	private AnimationNodeStateMachinePlayback GetPlayback()
	{
		return (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
	}


	private AnimationNodeBlendSpace2D GetPoseBlendSace()
	{

		if (GetPoseTree().HasNode(_poseSpaceName))
		{

			return (AnimationNodeBlendSpace2D)GetPoseTree().GetNode(_poseSpaceName);
		}

		return null;
	}


	public void SetPoseBlendInut(int index, float value)
	{
		String blend = String.Format("parameters/PoseTree/BlendInput{0}/blend_amount", index);
		_animTree.Set(blend, value);
	}


	public void PosePlayShot(string anim) { 
		_animTree.Set("parameters/PoseTree/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire); 
	}


	public void ResetXformTween()
	{
		_resetTween = GetTree().CreateTween();
		_resetTween?.SetProcessMode(Tween.TweenProcessMode.Physics);
		_resetTween?.TweenProperty(this, "transform", _initTransform, 0.2f);
	}
}