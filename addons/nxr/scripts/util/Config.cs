using Godot;
using Godot.NativeInterop;
using System;
using System.Diagnostics;
using System.IO;

public partial class Config : Node
{
	
	private string _fs = "user://"; 
	private string _fileName = "config"; 
	private ConfigFile _file = new ConfigFile();


    public void CreateFile() { 
		Error err = _file.Load(GetConfigPath()); 

		if (err != Error.Ok) { 
			_file.Save(GetConfigPath());
		}
	}


	public ConfigFile LoadConfig()  { 
		Error err = _file.Load(GetConfigPath()); 

		switch (err) { 
			case Error.Ok:
				return _file; 
			case Error.FileNotFound:
				CreateFile();  
				err = _file.Load(GetConfigPath()); 
				break; 
			default: 
				return null; 
		}

		if (err != Error.Ok) return null; 

		return _file; 
	}
		

	public void SaveValue(String section, String key, Variant value) { 

		ConfigFile file = LoadConfig(); 

		if (file == null) return; 
		GD.Print("file found"); 
		file.SetValue(section, key, value); 
		file.Save(GetConfigPath()); 
	}


	public string GetConfigPath() { 
		return _fs + _fileName + ".cfg";
	}
}
