// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed BerserkWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;

namespace TOR_Core.BattleMechanics.BerserkWield.Vendor;

public sealed class FluidStrikeCap
{
	public const int Min = 16;

	public const int Max = 128;

	public const int Start = 48;

	public const int Step = 4;

	public const float RaiseFps = 55f;

	public const float LowerFps = 40f;

	public const float Every = 0.5f;

	public const float CheapShare = 0.02f;

	public const float CostlyShare = 0.05f;

	private float _dt = 1f / 60f;

	private float _share = 0.035f;

	private float _in = 0.5f;

	public int Cap { get; private set; } = 48;

	public int Update(float dt, float strikeSeconds = 0f)
	{
		if (dt <= 0f || dt > 0.5f)
		{
			return Cap;
		}
		float num = Math.Min(1f, dt);
		_dt += (dt - _dt) * num;
		_share += (Math.Max(0f, strikeSeconds) / dt - _share) * num;
		if ((_in -= dt) > 0f)
		{
			return Cap;
		}
		_in = 0.5f;
		float num2 = 1f / Math.Max(0.0001f, _dt);
		if (num2 < 40f && _share > 0.05f)
		{
			Cap = Math.Max(16, Cap - 4);
		}
		else if (num2 >= 55f || _share < 0.02f)
		{
			Cap = Math.Min(128, Cap + 4);
		}
		return Cap;
	}
}
