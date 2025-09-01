using System.Threading.Tasks;
using Godot;
using NXR;
using NXRInteractable;

[GlobalClass]
public partial class InteractableGrabPoint : Interactable
{

    [Export] public Interactable Interactable = null;
    [Export] public GrabPointType GrabType = GrabPointType.Primary;

    [Export] public bool GrabPointGrabEnabled = true; 
    [Export] public bool InteractableGrabEnabled = true; 


    private Vector3 _offset = new();


    public override void _Ready()
    {
        base._Ready();


        OnGrabbed += Grabbed;
        OnDropped += Dropped;


        Interactable ??= Util.GetParentOrOwnerOfType<Interactable>(this);

        if (Interactable != null)
        {
            if (GrabType == GrabPointType.Primary)
            {
                Interactable.PrimaryGrabPoint = this;
            }
            else
            {
                Interactable.SecondaryGrabPoint = this;
            }

            if (!GrabPointGrabEnabled) return;
        }
    }


    private void Grabbed(Interactable interactable, Interactor interactor)
    {
        if (Interactable == null) return;

        if (GrabType == GrabPointType.Primary)
        {
            if (Interactable.PrimaryGrab.Interactor == null)
            {
                Interactable.PrimaryGrabPoint = this;
                Interactable.Grab(interactor);
            }
        }

        if (GrabType == GrabPointType.Secondary)
        {
            if (Interactable.SecondaryGrab.Interactor == null)
            {
                Interactable.SecondaryGrabPoint = this;
                Interactable.Grab(interactor, true);
            }
        }

        if (GrabType == GrabPointType.Generic)
        {
            if (Interactable.PrimaryGrab.Interactor == null)
            {
                Interactable.PrimaryGrabPoint = this;
                Interactable.Grab(interactor);
                return; 
            }
            if (Interactable.SecondaryGrab.Interactor == null)
            {
                Interactable.SecondaryGrabPoint = this;
                Interactable.Grab(interactor);
                return; 
            }
        }
    }


    private void Dropped(Interactable interactable, Interactor interactor)
    {
        Interactable.Drop(interactor);
    }

}
