using Godot;
using NXRPlayer;
using System;


namespace NXR;


public static class Util
{
	public static bool NodeIs(Node node, Type type)
	{
		if (node == null || type == null) return false;

		return node.GetType().IsAssignableTo(type);
	}


	public static T GetParentOrOwnerOfType<T>(Node node)
	{
		//check parent first 
		if (node.GetParent() is T t)
			return t;
		if (node.Owner is T t2)
		{
			return t2;
		}

		return default;
	}



	public static void Recenter()
	{
		XRServer.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, true);
		GD.Print("recentering");
	}


	public static Basis BasisSlerp(Basis from, Basis to, float amount)
	{
		Quaternion q1 = from.Orthonormalized().GetRotationQuaternion();
		Quaternion q2 = to.Orthonormalized().GetRotationQuaternion();
		Quaternion q3 = q1.Normalized().Slerp(q2.Normalized(), amount);

		return new Basis(q3).Orthonormalized();
	}


	public static Basis CreateOrthonormalBasis(Vector3 control, Vector3 forward, Vector3 upHint)
	{
		Vector3 x = control.Cross(forward).Normalized();

		if (x.LengthSquared() < 0.0001f)
		{
			x = forward.Cross(upHint).Normalized();
			if (x.LengthSquared() < 0.0001f)
				x = forward.Cross(Vector3.Right).Normalized();
		}

		Vector3 y = forward.Cross(x).Normalized();
		Vector3 z = forward.Normalized();

		return new Basis(x, y, z);
	}
}
