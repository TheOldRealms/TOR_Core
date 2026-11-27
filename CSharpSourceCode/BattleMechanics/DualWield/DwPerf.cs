// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System.Diagnostics;
using System.Text;

namespace TOR_Core.BattleMechanics.DualWield;

internal static class DwPerf
{
	public const int Tick = 0;

	public const int NpcWatch = 1;

	public const int NpcStrikes = 2;

	public const int Cleanup = 3;

	public const int ActionPrefix = 4;

	public const int WieldPostfix = 5;

	public const int Stats = 6;

	public const int Blow = 7;

	public const int StrikePose = 8;

	public const int StrikeGuards = 9;

	public const int StrikeRays = 10;

	public const int StrikeHits = 11;

	public const int Log = 12;

	public const int LimbRay = 13;

	public const int SceneRay = 14;

	public const int StrikeReact = 15;

	public const int Slots = 16;

	private static readonly string[] Names = new string[16]
	{
		"mission tick (all)", "npc watch", "npc strikes", "bookkeeping", "action prefix", "wield postfix", "stats", "blow magnitude", "strike pose", "strike guards",
		"strike rays", "strike hits", "log", "limb rays", "scene rays", "block + reactions"
	};

	private static readonly bool[] Nested = new bool[16]
	{
		false, true, true, true, false, false, false, false, true, true,
		true, true, true, true, true, true
	};

	public static int MaxConcurrent;

	public static int StrikeCap;

	private static readonly long[] _ticks = new long[16];

	private static readonly int[] _calls = new int[16];

	private static readonly Stopwatch _clock = Stopwatch.StartNew();

	private static long _windowStart = _clock.ElapsedTicks;

	public static void Concurrent(int running)
	{
		if (running > MaxConcurrent)
		{
			MaxConcurrent = running;
		}
	}

	public static long Start()
	{
		return _clock.ElapsedTicks;
	}

	public static float Seconds(long started)
	{
		return (float)((double)(_clock.ElapsedTicks - started) / (double)Stopwatch.Frequency);
	}

	public static void Stop(int slot, long started)
	{
		_ticks[slot] += _clock.ElapsedTicks - started;
		_calls[slot]++;
	}

	public static void Count(int slot)
	{
		_calls[slot]++;
	}

	public static string Report()
	{
		long elapsedTicks = _clock.ElapsedTicks;
		double num = (double)(elapsedTicks - _windowStart) / (double)Stopwatch.Frequency;
		if (num <= 0.0)
		{
			return "";
		}
		double num2 = 0.0;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < 16; i++)
		{
			double num3 = (double)_ticks[i] * 1000.0 / (double)Stopwatch.Frequency / num;
			if (!Nested[i])
			{
				num2 += num3;
			}
			if (_calls[i] > 0)
			{
				stringBuilder.Append((_ticks[i] > 0) ? $", {Names[i]} {num3:0.0} ({(double)_calls[i] / num:0}/s)" : $", {Names[i]} {(double)_calls[i] / num:0}/s");
			}
			_ticks[i] = 0L;
			_calls[i] = 0;
		}
		_windowStart = elapsedTicks;
		string arg = $", strikes at once max {MaxConcurrent} (cap {StrikeCap})";
		MaxConcurrent = 0;
		return $"[Perf] our code {num2:0.0} ms per second{stringBuilder}{arg}";
	}
}
