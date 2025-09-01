using Godot;
using Godot.Collections;
using System;


public static class SignalConnector
{	
	public static void ConnectFromNode(Node from, ISignalTool to, String signal) { 
		

		if (from == null || to == null) return; 
		
		if (!from.HasSignal(signal)) return;

		foreach (Dictionary item in from.GetSignalList())
		{
			if (item["name"].ToString() == signal)
			{
				int argCount = item["args"].AsGodotArray().Count;

				if (argCount > 0)
				{
					Action<int[]> signalAction = to.OnSignalParams;
					Callable callable = Callable.From(signalAction);
					Signal newSignal = new Signal(from, signal);
					from.Connect(signal, callable);
					return;
				}
				else
				{
					Callable callable = Callable.From(() => to.OnSignal());
					Signal newSignal = new Signal(from, signal);
					from.Connect(signal, callable);
					return;
				}
			}
		}
	}

}

