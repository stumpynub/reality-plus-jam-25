using Godot;

public partial class ResHaptic : Resource
{
	[Export] public float Frequency = 0.5f;
	[Export] public float Amplitude = 0.5f;
	[Export] public float Duration = 0.1f;

	public ResHaptic() : this(0.0f, 0.0f, 0.0f) {}

	public ResHaptic(float _freq, float _amp, float _dur)
	{
		Frequency = _freq;
		Amplitude = _amp;
		Duration = _dur;
	}
}
