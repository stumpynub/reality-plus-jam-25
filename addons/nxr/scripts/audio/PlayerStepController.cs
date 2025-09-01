using Godot;
using NXRPlayer;


[GlobalClass]
public partial class PlayerStepController : AudioStreamPlayer3D
{

    [Export] private Player _player;
    [Export] private AudioStream _stepClip;
    [Export] private float _timeBetweenSteps = 0.5f; // Time in seconds between steps
    [Export] private float _velocityEffect = 1.0f; 
    private float t = 0f;


    public override void _Ready()
    {
        if (_player == null || _stepClip == null)
            ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta)
    {
        if (!_player.IsOnGround()) return;

        t += (float)delta;

        float velMutliplier = _player.Velocity.Length() * _velocityEffect;
        if (t >= _timeBetweenSteps * velMutliplier)
        {
            Play();
            t = 0f;
        }
    }
}

