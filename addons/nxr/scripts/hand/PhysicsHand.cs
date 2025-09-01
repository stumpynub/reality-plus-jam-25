using System;
using Godot;
using NXRPlayer;

namespace NXR;


[GlobalClass]
public partial class PhysicsHand : RigidBody3D
{
	[Export] private PlayerRigid _rigidPlayer; 


	[ExportGroup("Damping")]
	[Export] private float _linearDamp = 40f;
	[Export] private float _angularDamp = 60f;
	[Export] private float _contactLinearDamp = 20f;
	[Export] private float _contactAngularDamp = 20f;

	[Export] private float _velFix = 1.65f; 

	private Vector3 lVelocity; 
	private Vector3 aVelocity;
	


	public override void _PhysicsProcess(double delta)
	{
		// Quaternion currentRotation = GlobalTransform.Basis.GetRotationQuaternion();
		// Quaternion newRotation = Interactor.GlobalTransform.Basis.GetRotationQuaternion() * Basis.FromEuler(_initRotation).GetRotationQuaternion();
		// Quaternion rotationChange = currentRotation * newRotation.Inverse();
		// Vector3 dir = Interactor.GlobalPosition - GlobalPosition;

		
		// Vector3 playerVel = Vector3.Zero; 

		// if (_rigidPlayer != null) 
		// 	playerVel = _rigidPlayer.LinearVelocity * _velFix; 

		// lVelocity = dir - _initOffset;
		// aVelocity = rotationChange.Inverse().GetEuler();


		// lVelocity /= (float)delta;
		// aVelocity /= (float)delta;


		// LinearVelocity = lVelocity + playerVel;
		// AngularVelocity = aVelocity;
	}
}
