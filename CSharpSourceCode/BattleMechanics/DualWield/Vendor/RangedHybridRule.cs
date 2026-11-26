// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

public sealed class RangedHybridRule
{
	public const float FarMetres = 20f;

	public const float NearMetres = 10f;

	public const float FarSeconds = 3f;

	public const int Keep = 0;

	public const int Release = 1;

	public const int Pair = 2;

	private float _far;

	public int Step(float dt, bool leftHeld, bool mainOneHanded, bool rangedReady, float nearestEnemy)
	{
		if (!rangedReady)
		{
			_far = 0f;
			if (!(!leftHeld & mainOneHanded))
			{
				return 0;
			}
			return 2;
		}
		if (leftHeld)
		{
			if (nearestEnemy <= 20f)
			{
				_far = 0f;
				return 0;
			}
			_far += dt;
			if (_far < 3f)
			{
				return 0;
			}
			_far = 0f;
			return 1;
		}
		_far = 0f;
		if (!mainOneHanded || !(nearestEnemy < 10f))
		{
			return 0;
		}
		return 2;
	}
}
