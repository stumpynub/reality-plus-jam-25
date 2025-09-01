using Godot;

namespace NXR; 


[GlobalClass]
public partial class HapticTicker : Node
{   
    private float _currentStep = 0;
    private int _tickCount = 0;



    public void Tick(Controller controller, float value, float step=0.1f, float freq=0.2f, float amp=0.2f, float dur=0.1f)
    {
        float snapped = Mathf.Snapped(value, step);
        if (_tickCount == 0)
        {
            _currentStep = value;

            if (_currentStep != snapped)
                _tickCount++;
            return;
        }

        if (_currentStep != snapped)
        {
            _currentStep = snapped;
            controller?.Pulse(freq, amp, dur);
        }

        _tickCount++;
    }
}
