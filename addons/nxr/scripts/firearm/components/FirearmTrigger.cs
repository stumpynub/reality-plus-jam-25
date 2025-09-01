using Godot;
using NXR;
using NXRFirearm;



[Tool]
[GlobalClass]
public partial class FirearmTrigger : FirearmPathInteractable
{


    private Firearm _firearm = null;


    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        RunTool(); 
    }

    public override void _PhysicsProcess(double delta)
    {
        RunTool(); 

        if (Engine.IsEditorHint() || Firearm == null) return; 
         
        InterpolateTransforms(Firearm.GetTriggerPullValue());
    }
}
