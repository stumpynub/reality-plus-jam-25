using Godot;
using NXR;
using NXRInteractable;

public partial class NetSync : MultiplayerSynchronizer
{

	[Export] Transform3D NodeXform = new();
	Node3D Node { get; set; }
	Transform3D SmoothedXform = new();

	Vector3 NodeVelocity = new();
	Vector3 PrevNodeVelcocity = new();
	Vector3 NextNodePosition = new();
	public override void _Ready()
	{
		Node = (Node3D)GetParent();
		NodeVelocity = Node.GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsMultiplayerAuthority())
		{
			SetOwnerXform(); 

		}
		else
		{
			Node.GlobalPosition = Node.GlobalPosition.Lerp(NodeXform.Origin, 0.1f);
			Node.GlobalBasis = Util.BasisSlerp(Node.GlobalBasis, NodeXform.Basis, 0.1f).Orthonormalized();
		}
	}	

	[Rpc(CallLocal = true)]
	public void SetOwnerXform()
	{

		NodeXform = Node.GlobalTransform;
		NodeVelocity = (Node.GlobalPosition - PrevNodeVelcocity) / (float)GetPhysicsProcessDeltaTime();
		PrevNodeVelcocity = Node.GlobalPosition;
		NextNodePosition = Node.GlobalPosition + PrevNodeVelcocity;

	}

	public void Interpolate()
	{
		Node.GlobalPosition = Node.GlobalPosition.Lerp(NodeXform.Origin, 0.2f);
		Node.GlobalBasis = Util.BasisSlerp(Node.GlobalBasis, NodeXform.Basis, 0.2f).Orthonormalized();
	}
}
