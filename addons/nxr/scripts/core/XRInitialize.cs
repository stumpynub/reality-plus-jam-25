using System;
using System.Linq;
using Godot;
using Godot.Collections;
using NXR;

[GlobalClass]
public partial class XRInitialize : Node
{
    private enum RefreshRate
    {
        _72 = 72,
        _90 = 90,
        _120 = 120
    }

    [Export] private RefreshRate _preferredRefreshRate = RefreshRate._90;

    [ExportGroup("Web")]
    [Export] private Button _startButton;

    private OpenXRInterface _openXRInterface;
    private WebXRInterface _webXRInterface;
    private float _refreshRate = 72f;

    public override void _Ready()
    {
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

        _openXRInterface = XRServer.FindInterface("OpenXR") as OpenXRInterface;
        _webXRInterface = XRServer.FindInterface("WebXR") as WebXRInterface;

        if (_openXRInterface != null)
        {
            SetupOpenXR();
        }
        else
        {
            GD.PrintErr("OpenXR not found. Ensure OpenXR plugin is active.");
        }

        if (_webXRInterface != null)
        {
            SetupWebXR();
        }
        else
        {
            GD.Print("WebXR not found. Running in native or non-WebXR environment.");
        }

        if (_startButton != null)
        {
            _startButton.Pressed += () => { GD.Print("Start button pressed."); };
        }
    }

    private void PoseRecenter()
    {
        GD.Print("Re-centering HMD...");
        XRServer.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, true);
    }

    private void SetupWebXR()
    {
        _webXRInterface.SessionSupported += (string mode, bool supported) =>
        {
            GD.Print($"WebXR session '{mode}' support: {supported}");
        };

        _webXRInterface.SessionStarted += () =>
        {
            GD.Print("WebXR session started.");
        };

        _webXRInterface.SessionEnded += () =>
        {
            GD.Print("WebXR session ended.");
        };

        _webXRInterface.SessionFailed += (string error) =>
        {
            GD.PrintErr($"WebXR session failed: {error}");
            OS.Alert($"WebXR session failed: {error}");
        };

        // Optional: proactively check session support
        _webXRInterface.IsSessionSupported("immersive-vr");
    }

    private void SetupOpenXR()
    {
        GetViewport().UseXR = true;

        // Defensive: check if initialized first
        if (_openXRInterface.IsInitialized())
        {
            _openXRInterface.SessionBegun += OnOpenXRSessionBegun;


            GD.Print("OpenXR initialized and session hooks attached.");
        }
        else
        {
            // Fallback: Wait for initialization (optional)
            GD.Print("OpenXR not initialized. Consider waiting or retrying.");
        }
    }

    private void OnOpenXRSessionBegun()
    {

        //_openXRInterface.SessionVisible += () => Util.Recenter();
        //_openXRInterface.PoseRecentered += () => Util.Recenter();
        GD.Print("OpenXR session begun. Setting up refresh rate...");
        SetupRefreshRate();
    }

    private void SetupRefreshRate()
    {
        float reportedRate = 72f;
        Godot.Collections.Array rates = new();

        try
        {
            reportedRate = (float)_openXRInterface.Call("get_display_refresh_rate");
            rates = _openXRInterface.GetAvailableDisplayRefreshRates();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to query display refresh rate: {ex.Message}");
        }

        GD.Print($"Available refresh rates: {string.Join(", ", rates.Cast<Variant>())}");

        if (rates.Count > 0 && rates.Contains((float)_preferredRefreshRate))
        {
            _refreshRate = (float)_preferredRefreshRate;
            GD.Print($"Using preferred refresh rate: {_refreshRate} Hz");
        }
        else if (rates.Count > 0)
        {
            _refreshRate = (float)rates[rates.Count - 1]; // Use highest available
            GD.Print($"Preferred not found. Using highest available rate: {_refreshRate} Hz");
        }
        else
        {
            _refreshRate = reportedRate;
            GD.Print($"No available rates found. Falling back to reported: {_refreshRate} Hz");
        }

        try
        {
            _openXRInterface.Call("set_display_refresh_rate", _refreshRate);
            Engine.PhysicsTicksPerSecond = (int)_refreshRate;
            GD.Print($"Set display refresh rate to {_refreshRate} Hz and physics ticks to {(int)_refreshRate}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to set refresh rate: {ex.Message}");
        }
    }
}
