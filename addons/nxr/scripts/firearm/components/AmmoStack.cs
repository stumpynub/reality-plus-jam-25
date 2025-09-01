using Godot;
using NXR;
using NXRFirearm;
using System;


[Tool]
[GlobalClass]

public partial class AmmoStack : BezierCurve3D
{
	[Export] private int _stackSize = 30;
	[Export] private int _columns = 2;
	[Export] private float _columnOffset = 0.01f;
	[Export] private float _rowOffset = 0f;
	[Export] private float _rowSeperation = 0.6f;
	[Export] private float _rotationOffset = 0.0f;


	private FirearmMag _mag;

	public override void _Ready()
	{
		if (Util.GetParentOrOwnerOfType<FirearmMag>(this) != null)
		{
			_mag = Util.GetParentOrOwnerOfType<FirearmMag>(this);
			_stackSize = _mag.Capacity;
			_mag.AmmoTaken += AmmoTaken;
			GD.Print("found mag");
		}
	}

	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint()) return; 
		
		base._Process(delta);

		Resolution = _stackSize;

		for (int i = 0; i < GetChildCount(); i++)
		{
			if (i > _stackSize) return;

			float row = (-i / _columns) * _rowSeperation;

			Node3D child = GetChild(i) as Node3D;
			Vector3 pos = Curve.GetPointPosition(i);
			child.Position = new Vector3(
				_columnOffset * Mathf.PingPong(i % _columns, _columns) - (_columns / 2) * _columnOffset,
				(-i / _columns) * _rowSeperation + i * _rowOffset,
				pos.Z
			);

			child.Rotation = new Vector3(
				Mathf.InverseLerp(0, _stackSize, i) * _rotationOffset,
				0,
				0
			);
		}
	}


	private void UpdateStatck()
	{
		for (int i = 0; i < GetChildCount(); i++)
		{
			if (i > _stackSize) return;

			float row = (-i / _columns) * _rowSeperation;

			Node3D child = GetChild(i) as Node3D;
			Vector3 pos = Curve.GetPointPosition(i);
			child.Position = new Vector3(
				_columnOffset * Mathf.PingPong(i % _columns, _columns) - (_columns / 2) * _columnOffset,
				(-i / _columns) * _rowSeperation + i * _rowOffset,
				pos.Z
			);

			child.Rotation = new Vector3(
				Mathf.InverseLerp(0, _stackSize, i) * _rotationOffset,
				0,
				0
			);
		}
	}


	public FirearmBullet PopAmmo()
	{
		return GetChild(GetChildCount()) as FirearmBullet;
	}


	private void AmmoTaken(int index)
	{
		if (index > GetChildCount() - 1) return;

		GetChild(index)?.QueueFree();
	}
}
