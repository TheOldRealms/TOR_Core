// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

internal static class OneAgentLimbRay
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void BuildFn(IntPtr caps, float zMin, float zMax);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	private delegate bool TestFn(IntPtr agent, IntPtr caps, IntPtr ray, IntPtr hit);

	private const int BuildWildFrom = 58;

	private const int BuildWildCount = 4;

	private static IntPtr _buildAt;

	private static IntPtr _testAt;

	private static readonly byte[] BuildBytes = new byte[265]
	{
		64, 83, 72, 131, 236, 32, 72, 139, 217, 69,
		50, 201, 72, 139, 137, 8, 12, 0, 0, 72,
		139, 129, 16, 8, 0, 0, 76, 139, 144, 216,
		0, 0, 0, 72, 139, 129, 136, 5, 0, 0,
		72, 99, 144, 184, 36, 0, 0, 131, 250, 255,
		15, 132, 135, 0, 0, 0, 72, 139, 5, 81,
		240, 114, 0, 72, 107, 210, 104, 68, 15, 182,
		68, 2, 96, 65, 128, 232, 5, 65, 128, 248,
		1, 119, 108, 131, 185, 28, 2, 0, 0, 7,
		117, 99, 247, 129, 100, 5, 0, 0, 0, 64,
		0, 0, 116, 87, 72, 139, 129, 216, 10, 0,
		0, 72, 139, 144, 240, 14, 0, 0, 72, 133,
		210, 116, 68, 131, 122, 48, 0, 126, 62, 72,
		99, 130, 240, 0, 0, 0, 72, 141, 12, 128,
		72, 139, 130, 208, 0, 0, 0, 72, 193, 225,
		5, 76, 139, 68, 1, 80, 73, 15, 186, 224,
		9, 115, 7, 102, 131, 122, 8, 0, 126, 21,
		73, 193, 232, 28, 184, 1, 0, 0, 0, 65,
		246, 192, 1, 69, 15, 182, 201, 68, 15, 69,
		200, 72, 139, 203, 77, 133, 210, 116, 33, 69,
		132, 201, 116, 14, 232, 63, 0, 0, 0, 198,
		3, 1, 72, 131, 196, 32, 91, 195, 232, 177,
		3, 0, 0, 198, 3, 1, 72, 131, 196, 32,
		91, 195, 69, 132, 201, 116, 14, 232, 222, 6,
		0, 0, 198, 3, 1, 72, 131, 196, 32, 91,
		195, 232, 176, 9, 0, 0, 198, 3, 1, 72,
		131, 196, 32, 91, 195
	};

	private static readonly byte[] TestBytes = new byte[225]
	{
		72, 137, 92, 36, 8, 72, 137, 108, 36, 16,
		72, 137, 116, 36, 24, 72, 137, 124, 36, 32,
		65, 86, 72, 131, 236, 96, 72, 139, 129, 128,
		8, 0, 0, 72, 139, 234, 65, 15, 16, 0,
		15, 182, 122, 1, 77, 139, 241, 65, 15, 16,
		72, 16, 72, 139, 176, 184, 8, 0, 0, 76,
		139, 201, 65, 139, 64, 32, 72, 141, 76, 36,
		48, 72, 131, 198, 112, 137, 68, 36, 80, 72,
		139, 214, 77, 139, 208, 15, 17, 68, 36, 48,
		15, 17, 76, 36, 64, 232, 28, 157, 0, 0,
		77, 139, 137, 128, 8, 0, 0, 50, 219, 243,
		65, 15, 16, 66, 32, 243, 65, 15, 94, 129,
		208, 8, 0, 0, 243, 15, 17, 68, 36, 80,
		64, 132, 255, 126, 57, 102, 15, 31, 132, 0,
		0, 0, 0, 0, 72, 15, 190, 195, 72, 141,
		85, 4, 77, 139, 206, 72, 137, 116, 36, 32,
		76, 141, 68, 36, 48, 72, 141, 12, 64, 72,
		193, 225, 4, 72, 3, 209, 232, 75, 43, 0,
		0, 132, 192, 117, 36, 254, 195, 64, 58, 223,
		124, 208, 50, 192, 76, 141, 92, 36, 96, 73,
		139, 91, 16, 73, 139, 107, 24, 73, 139, 115,
		32, 73, 139, 123, 40, 73, 139, 227, 65, 94,
		195, 176, 1, 235, 225
	};

	private const int CapsSize = 3088;

	private const int CapsAgent = 3080;

	private const int HitBone = 32;

	private const int HitFloat = 36;

	private static BuildFn _build;

	private static TestFn _test;

	private static Func<Agent, UIntPtr> _agentPtr;

	private static IntPtr _caps;

	private static IntPtr _ray;

	private static IntPtr _hit;

	private static int _state;

	public const int Target = 0;

	public const int StruckEnemy = 1;

	public const int Friend = 2;

	public const int Inactive = 3;

	public const int Mount = 4;

	public const int Kinds = 5;

	private static readonly string[] KindNames = new string[5] { "enemy", "enemy already struck", "friend", "not active", "mount" };

	private static int _casts;

	private static int _rays;

	private static int _bothNone;

	private static int _sameAgent;

	private static int _boneDiffers;

	private static int _distDiffers;

	private static int _notCandidate;

	private static readonly int[] _engineOnly = new int[5];

	private static readonly int[] _oursOnly = new int[5];

	private static readonly int[] _otherEngine = new int[5];

	private static readonly int[] _otherOurs = new int[5];

	private static readonly int[] _logged = new int[4];

	private static readonly int[] LogBudget = new int[4] { 10, 6, 10, 10 };

	private static long _castTicks;

	private static float _maxDistDelta;

	private const int OriginSteps = 9;

	private static readonly int[] _originMen = new int[9];

	private static readonly int[] _originMounts = new int[9];

	private static float _originMenMax;

	private static float _originMountsMax;

	public static bool Available
	{
		get
		{
			if (_state == 0)
			{
				Install();
			}
			return _state > 0;
		}
	}

	private static void Install()
	{
		_state = -1;
		try
		{
			IntPtr intPtr = IntPtr.Zero;
			int size = 0;
			foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
			{
				if (string.Equals(module.ModuleName, "TaleWorlds.Native.dll", StringComparison.OrdinalIgnoreCase))
				{
					intPtr = module.BaseAddress;
					size = module.ModuleMemorySize;
					break;
				}
			}
			if (intPtr != IntPtr.Zero)
			{
				_buildAt = FindUnique(intPtr, size, BuildBytes, 58, 4);
				_testAt = FindUnique(intPtr, size, TestBytes, 0, 0);
			}
			if (_buildAt == IntPtr.Zero || _testAt == IntPtr.Zero)
			{
				DualWieldLog.Log("[LimbRay] one-agent ray: native functions not found in this build (known from 1.5.3 to 1.5.5) - the engine's ray stays");
				return;
			}
			MethodInfo methodInfo = AccessTools.Method(typeof(Agent), "GetPtr", (Type[])null, (Type[])null);
			if (methodInfo == null || methodInfo.ReturnType != typeof(UIntPtr))
			{
				DualWieldLog.Log("[LimbRay] one-agent ray: Agent.GetPtr not found - the engine's ray stays");
				return;
			}
			_agentPtr = AccessTools.MethodDelegate<Func<Agent, UIntPtr>>(methodInfo, (object)null, true);
			_build = (BuildFn)Marshal.GetDelegateForFunctionPointer(_buildAt, typeof(BuildFn));
			_test = (TestFn)Marshal.GetDelegateForFunctionPointer(_testAt, typeof(TestFn));
			_caps = Marshal.AllocHGlobal(3088);
			_ray = Marshal.AllocHGlobal(48);
			_hit = Marshal.AllocHGlobal(48);
			_state = 1;
			DualWieldLog.Log("[LimbRay] one-agent ray: native functions found and verified");
		}
		catch (Exception ex)
		{
			_state = -1;
			DualWieldLog.Log("[LimbRay] one-agent ray: off after error: " + ex.Message);
		}
	}

	[DllImport("kernel32.dll")]
	private static extern bool ReadProcessMemory(IntPtr process, IntPtr address, [Out] byte[] buffer, UIntPtr size, out UIntPtr read);

	[DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
	private static extern IntPtr CurrentProcess();

	private static IntPtr FindUnique(IntPtr module, int size, byte[] body, int wildFrom, int wildCount)
	{
		byte[] array = new byte[1048576 + body.Length];
		long num = 0L;
		int num2 = 0;
		for (int i = 0; i < size; i += 1048576)
		{
			int num3 = Math.Min(array.Length, size - i);
			if (num3 < body.Length)
			{
				break;
			}
			if (!ReadProcessMemory(CurrentProcess(), module + i, array, (UIntPtr)(ulong)num3, out var read))
			{
				continue;
			}
			int num4 = Math.Min((int)read.ToUInt64() - body.Length, 1048575);
			for (int j = 0; j <= num4; j++)
			{
				if (array[j] == body[0] && array[j + 1] == body[1])
				{
					int k;
					for (k = 2; k < body.Length && (array[j + k] == body[k] || (k >= wildFrom && k < wildFrom + wildCount)); k++)
					{
					}
					if (k == body.Length)
					{
						num = module.ToInt64() + i + j;
						num2++;
					}
				}
			}
		}
		if (num2 != 1)
		{
			return IntPtr.Zero;
		}
		return (IntPtr)num;
	}

	private static bool Matches(IntPtr address, byte[] expected)
	{
		for (int i = 0; i < expected.Length; i++)
		{
			if (Marshal.ReadByte(address, i) != expected[i])
			{
				return false;
			}
		}
		return true;
	}

	public static bool Cast(Agent agent, Vec3 from, Vec3 to, float thickness, out float distance, out sbyte bone)
	{
		distance = float.MaxValue;
		bone = -1;
		if (_state <= 0)
		{
			return false;
		}
		try
		{
			long timestamp = Stopwatch.GetTimestamp();
			IntPtr intPtr = new IntPtr((long)_agentPtr(agent).ToUInt64());
			if (intPtr == IntPtr.Zero)
			{
				return false;
			}
			Marshal.WriteByte(_caps, 0, 0);
			Marshal.WriteIntPtr(_caps, 3080, intPtr);
			_build(_caps, float.MinValue, float.MaxValue);
			WriteVec(_ray, 0, from);
			WriteVec(_ray, 16, to);
			WriteFloat(_ray, 32, thickness);
			for (int i = 0; i < 40; i += 4)
			{
				Marshal.WriteInt32(_hit, i, 0);
			}
			Marshal.WriteByte(_hit, 32, byte.MaxValue);
			WriteFloat(_hit, 36, float.MaxValue);
			bool flag = _test(intPtr, _caps, _ray, _hit);
			_casts++;
			_castTicks += Stopwatch.GetTimestamp() - timestamp;
			if (!flag)
			{
				return false;
			}
			bone = (sbyte)Marshal.ReadByte(_hit, 32);
			float num = ReadFloat(_hit, 0) - from.x;
			float num2 = ReadFloat(_hit, 4) - from.y;
			float num3 = ReadFloat(_hit, 8) - from.z;
			distance = (float)Math.Sqrt(num * num + num2 * num2 + num3 * num3);
			return true;
		}
		catch (Exception ex)
		{
			_state = -1;
			DualWieldLog.Log("[LimbRay] one-agent ray: stopped after error: " + ex.Message);
			return false;
		}
	}

	private static void WriteVec(IntPtr p, int offset, Vec3 v)
	{
		WriteFloat(p, offset, v.x);
		WriteFloat(p, offset + 4, v.y);
		WriteFloat(p, offset + 8, v.z);
		WriteFloat(p, offset + 12, v.w);
	}

	private static void WriteFloat(IntPtr p, int offset, float f)
	{
		Marshal.WriteInt32(p, offset, BitConverter.ToInt32(BitConverter.GetBytes(f), 0));
	}

	private static float ReadFloat(IntPtr p, int offset)
	{
		return BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(p, offset)), 0);
	}

	public static void NoteHitOrigin(bool human, float originToRay)
	{
		int num = Math.Min(8, Math.Max(0, (int)(originToRay / 0.5f)));
		if (human)
		{
			_originMen[num]++;
			if (originToRay > _originMenMax)
			{
				_originMenMax = originToRay;
			}
		}
		else
		{
			_originMounts[num]++;
			if (originToRay > _originMountsMax)
			{
				_originMountsMax = originToRay;
			}
		}
	}

	public static void Compare(Agent engine, float engineDist, sbyte engineBone, int engineKind, Agent ours, float ourDist, sbyte ourBone, int oursKind, float rayLen, bool engineWasCandidate, float engineOrigin)
	{
		_rays++;
		if (engine != null && engine != ours && !engineWasCandidate)
		{
			_notCandidate++;
		}
		string text = null;
		int num = 0;
		if (engine == null && ours == null)
		{
			_bothNone++;
		}
		else if (engine != null && ours == null)
		{
			Count(_engineOnly, engineKind);
			text = "engine only";
			num = 0;
		}
		else if (engine == null)
		{
			Count(_oursOnly, oursKind);
			text = "ours only";
			num = 1;
		}
		else if (engine != ours)
		{
			Count(_otherEngine, engineKind);
			Count(_otherOurs, oursKind);
			text = "another agent";
			num = 2;
		}
		else
		{
			_sameAgent++;
			num = 3;
			float num2 = Math.Abs(engineDist - ourDist);
			if (engineBone != ourBone)
			{
				_boneDiffers++;
				text = "bone differs";
			}
			if (num2 > 0.001f)
			{
				_distDiffers++;
				if (num2 > _maxDistDelta)
				{
					_maxDistDelta = num2;
				}
				text = text ?? "distance differs";
			}
		}
		if (text != null && _logged[num] < LogBudget[num])
		{
			_logged[num]++;
			DualWieldLog.Log(string.Format(CultureInfo.InvariantCulture, "[LimbRay] {0}: engine {1} bone {2} dist {3:0.000}{8} | ours {4} bone {5} dist {6:0.000} | ray {7:0.00} m", new object[9]
			{
				text,
				Describe(engine, engineKind),
				engineBone,
				(engine != null) ? engineDist : 0f,
				Describe(ours, oursKind),
				ourBone,
				(ours != null) ? ourDist : 0f,
				rayLen,
				(engine == null) ? "" : string.Format(CultureInfo.InvariantCulture, " ({0}, origin {1:0.00} m from the attacker)", engineWasCandidate ? "a candidate" : "NOT a candidate", engineOrigin)
			}));
		}
	}

	private static void Count(int[] by, int kind)
	{
		if (kind >= 0 && kind < 5)
		{
			by[kind]++;
		}
	}

	private static string Describe(Agent a, int kind)
	{
		if (a != null)
		{
			return ((kind >= 0 && kind < 5) ? KindNames[kind] : "?") + " #" + a.Index;
		}
		return "none";
	}

	private static string ByKind(int[] by)
	{
		int num = 0;
		foreach (int num2 in by)
		{
			num += num2;
		}
		StringBuilder stringBuilder = new StringBuilder().Append(num).Append(" (");
		for (int j = 0; j < 5; j++)
		{
			stringBuilder.Append((j > 0) ? ", " : "").Append(KindNames[j]).Append(' ')
				.Append(by[j]);
		}
		return stringBuilder.Append(')').ToString();
	}

	private static string Steps(int[] steps, float max)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < steps.Length; i++)
		{
			stringBuilder.Append((i > 0) ? "/" : "").Append(steps[i]);
		}
		return stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, ", max {0:0.00} m", max)).ToString();
	}

	public static string TakeSummary()
	{
		if (_casts == 0 && _rays == 0)
		{
			return null;
		}
		double num = ((_casts > 0) ? ((double)_castTicks * 1000000.0 / (double)Stopwatch.Frequency / (double)_casts) : 0.0);
		string text = string.Format(CultureInfo.InvariantCulture, "[LimbRay] one-agent casts {0}, {1:0.00} us each", _casts, num);
		if (_rays > 0)
		{
			text = text + string.Format(CultureInfo.InvariantCulture, " | shadow: rays {0}, both none {1}, same agent {2} (bone differs {3}, distance differs {4}, max {5:0.000} m)", new object[6] { _rays, _bothNone, _sameAgent, _boneDiffers, _distDiffers, _maxDistDelta }) + " | engine only " + ByKind(_engineOnly) + " | ours only " + ByKind(_oursOnly) + " | another agent: the engine's " + ByKind(_otherEngine) + ", ours " + ByKind(_otherOurs) + " | the engine's agent not among the candidates " + _notCandidate;
		}
		text = text + " | origin to ray of the agents ours hit, 0.5 m steps: men " + Steps(_originMen, _originMenMax) + "; mounts " + Steps(_originMounts, _originMountsMax);
		if (_state < 0)
		{
			text += " | OFF (other build or stopped after an error)";
		}
		_casts = (_rays = (_bothNone = (_sameAgent = (_boneDiffers = (_distDiffers = (_notCandidate = 0))))));
		Array.Clear(_engineOnly, 0, 5);
		Array.Clear(_oursOnly, 0, 5);
		Array.Clear(_otherEngine, 0, 5);
		Array.Clear(_otherOurs, 0, 5);
		Array.Clear(_logged, 0, _logged.Length);
		Array.Clear(_originMen, 0, 9);
		Array.Clear(_originMounts, 0, 9);
		_castTicks = 0L;
		_maxDistDelta = (_originMenMax = (_originMountsMax = 0f));
		return text;
	}
}
