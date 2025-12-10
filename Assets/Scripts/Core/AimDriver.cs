using UnityEngine;

public static class AimDriver
{
	public static void Rotate(Transform pivot, Vector3 dir, float speedDeg)
	{
		Vector3 flat = new Vector3(dir.x, 0, dir.z);
		if (flat.sqrMagnitude < 0.0001f) return;

		Quaternion targetRot = Quaternion.LookRotation(flat);
		pivot.rotation = Quaternion.RotateTowards(
			pivot.rotation,
			targetRot,
			speedDeg * Time.deltaTime);
	}
}