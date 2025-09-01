using Godot;
using NXR;

[GlobalClass]
public partial class PlayerSpawn : Marker3D
{


	[Export] private PackedScene _player;
	RayCast3D _ray = new(); 

	public override void _Ready()
	{

		AddChild(_ray);
		_ray.TargetPosition = Vector3.Down * 10f; 

		CallDeferred("spawn"); 
	}


	private void spawn() { 
		Node3D inst = (Node3D)_player.Instantiate(); 

		GetParent().AddChild(inst);
		GetParent().MoveChild(inst, 0);  

		if (_ray.IsColliding())
		{
			Vector3 pos = _ray.GetCollisionPoint();
			inst.GlobalPosition = pos;
		}
	
		inst.GlobalTransform = GlobalTransform; 

		Util.Recenter(); 
	}
}
