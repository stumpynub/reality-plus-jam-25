#if TOOLS
using System;
using System.ComponentModel;
using Godot;

[Tool]
public partial class Plugin : EditorPlugin
{

	public override void _EnterTree()
	{
	}

	public override void _EnablePlugin()
	{
		base._EnablePlugin();
		AddProjectSetting("NXR/default_interactable_layer", (int)Variant.Type.Int, (int)PropertyHint.Layers3DPhysics, "", 1u << 3); 
    }


    public override void _ExitTree()
	{
		base._ExitTree();
	}


    public void AddProjectSetting(String name, int type, int hint, string hintString, Variant defaultValue) {

		// use Variant.Type for type 
		// use PropertyHint.Enum for hint 
		
		ProjectSettings.Singleton.Set(name, defaultValue); 

        var setting = new Godot.Collections.Dictionary { 
				{"name", name},
				{"type", type},
				{"hint", hint}, 
				{"hint_string", hintString},
			};

    	ProjectSettings.AddPropertyInfo(setting);
		ProjectSettings.Singleton.SetInitialValue(name, defaultValue); 
		
	}
}
#endif
