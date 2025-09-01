using Godot;

namespace NXRInteractable;

[GlobalClass]
public partial class InteractableGrabSpawn : Interactable
{
	[Export] public bool Disabled = false;
	[Export] protected PackedScene _scene;

	private Interactor _prevInteractor;

	public override void _Ready()
	{
		base._Ready();
		OnGrabbed += Grabbed;
		OnFullDropped += FullDropped;

		if (Disabled) Visible = false;
	}

	private void Grabbed(Interactable interactable, Interactor interactor)
	{
		if (Disabled) return;
		_prevInteractor = interactor;
		CallDeferred("FullDrop"); 
		//CallDeferred(nameof(DeferredSpawnAndGrab), interactor);
	}

	private void FullDropped()
	{
		if (Disabled) return;
		CallDeferred(nameof(DeferredSpawnAndGrab), _prevInteractor);
	}

	private void DeferredSpawnAndGrab(Interactor interactor)
	{

		Interactable inst = (Interactable)_scene.Instantiate();
		GetParent().AddChild(inst);
		inst.GlobalTransform = GlobalTransform;

		interactor.Grab(inst); 
	}
}
