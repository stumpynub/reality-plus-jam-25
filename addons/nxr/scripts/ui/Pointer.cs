using Godot;
using NXR;
using NXRInteractable;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;


enum PointerInputType
{
	Press,
	Release,
	Move,
}


public partial class Pointer : ShapeCast3D
{

	#region Exported
	[Export] private Controller _controller;
	[Export] public bool Disabled = false;

	[Export(PropertyHint.Range, "0.0, 100.0")] private float _visiblePercent = 100.0f;
	[Export(PropertyHint.Range, "0.0, 1.0")] private float _velocityEffectStrength = 0.5f;
	[Export(PropertyHint.Range, "0.0, 1.0")] private float _arcStrength = 0.15f;


	[ExportGroup("Actions")]
	[Export] private string _pressAction = "trigger_click";
	[Export] private string _releaseAction = "trigger_click";


	[Export] float _defaultLength = 2.0f;


	[ExportGroup("InteractorSettings")]
	[Export] Interactor _interactor;
	[Export] bool _disableWhenGrabbing = true;


	[ExportGroup("Line Settings")]
	[Export] private int _resolution = 10;
	[Export] private Line3D _line;
	[Export] private float _positionSmoothing = 0.6f;
	[Export] private float _rotationSmoothing = 0.3f;
	#endregion

	private IPointerInteractable _prevInteractable;



	public override void _Ready()
	{
		if (_controller != null)
		{
			_controller.ButtonPressed += OnButtonPressed;
			_controller.ButtonReleased += OnButtonReleased;
		}

		if (_interactor != null)
		{
			_interactor.OnGrabbed += OnGrabbed;
			_interactor.OnDropped += OnDropped;
		}

		if (_controller != null)
		{
			TopLevel = true;
		}
	}



	public override void _PhysicsProcess(double delta)
	{
		if (_controller == null) return;

		GlobalPosition = GlobalPosition.Lerp(_controller.GlobalPosition, _positionSmoothing);
		GlobalBasis = GlobalTransform.Basis.Slerp(
			_controller.GlobalTransform.Basis.Orthonormalized(),
			_rotationSmoothing
		);
	}

	public override void _Process(double delta)
	{

		UpdateLine();
		if (Disabled) return;


		if (GetPointerInteractable() != null)
		{

			TrySendInput(PointerInputType.Move);

			// handle pointer enter 
			if (_prevInteractable == null && GetPointerInteractable() != null)
			{
				_prevInteractable = GetPointerInteractable();
				_prevInteractable.PointerEntered(this);
			}
		}
		else
		{
			// handle pointer exit 
			if (_prevInteractable != null)
			{
				_prevInteractable.PointerExited(this);
				_prevInteractable = null;
			}
		}
	}


	public void UpdateLine()
	{
		if (_line == null) return;

		float len = _defaultLength;

		if (GetCollisionCount() > 0)
			len = GlobalPosition.DistanceTo(GetCollisionPoint(0));

		if (GetPointerInteractable() != null)
		{
			_line.Visible = true;
		}
		else
		{
			_line.Visible = false;
		}

		float step = len / _resolution;
		_line.Points = [];
		for (int i = 0; i < _resolution; i++)
		{
			float arcHeight = Mathf.Sin((float)i / _resolution * Mathf.Pi) * _arcStrength;
			Vector3 velOffset = _controller.GetLocalVelocity() * _velocityEffectStrength * arcHeight;
			_line.AddPoint(-new Vector3(-velOffset.X, -velOffset.Y, i * step));
		}
	}


	private void OnButtonPressed(String button)
	{
		if (button != _pressAction) return;

		TrySendInput(PointerInputType.Press);
	}


	private void OnButtonReleased(String button)
	{
		if (button != _pressAction) return;

		TrySendInput(PointerInputType.Release);
	}


	private void TrySendInput(PointerInputType type)
	{
		if (GetPointerInteractable() == null) return;


		switch (type)
		{
			case PointerInputType.Press:
				GetPointerInteractable().Pressed(this, GetCollisionPoint(0));
				break;
			case PointerInputType.Release:
				GetPointerInteractable().Released(this, GetCollisionPoint(0));
				break;
			case PointerInputType.Move:
				GetPointerInteractable().Moved(this, GetCollisionPoint(0));
				break;
		}
	}


	private IPointerInteractable GetPointerInteractable()
	{
		if (GetCollisionCount() <= 0) return null;

		return Util.GetParentOrOwnerOfType<IPointerInteractable>((Node)GetCollider(0));
	}



	public void OnGrabbed(Interactable interactable)
	{
		if (_disableWhenGrabbing)
		{
			Disabled = true;
			Visible = false;
		}
	}


	public void OnDropped(Interactable interactable)
	{
		if (_disableWhenGrabbing)
		{
			Disabled = false;
			Visible = true;
		}
	}
}
