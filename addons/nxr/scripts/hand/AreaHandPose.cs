using Godot;
using NXR;


[GlobalClass]
public partial class AreaHandPose : HandPoseBase
{
	[Export] private Area3D _area;

	[Export] private string _pose = "";
	public override void _Ready()
	{
		base._Ready();

		if (_area != null)
		{
			_area.BodyExited += HandExited;
		}
	}


	public override void _Process(double delta)
	{
		if (_area == null) return;

		base._Process(delta);

		foreach (Node3D body in _area.GetOverlappingBodies())
		{
			if (body is Hand hand)
			{
				if (hand.CurrentState != Hand.HandState.Posed)
					Pose(hand, _pose);
			}
		}
	}

	private void HandExited(Node3D node)
	{
		if (node is Hand hand)
		{
			if (hand.CurrentPoseBase == this)
				hand.ResetHand(); 
		}
	}
}
