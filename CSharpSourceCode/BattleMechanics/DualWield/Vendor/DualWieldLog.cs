// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;
using System.IO;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

public static class DualWieldLog
{
	private const long MaxBytes = 26214400L;

	private static readonly string LogPath;

	private static readonly object Lock;

	private static StreamWriter _writer;

	private static long _written;

	private static bool _capped;

	private static DateTime _lastFlush;

	public static bool Unlimited { get; set; }

	static DualWieldLog()
	{
		Lock = new object();
		_lastFlush = DateTime.MinValue;
		try
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Mount and Blade II Bannerlord", "Logs");
			Directory.CreateDirectory(text);
			LogPath = Path.Combine(text, "DualWield_debug.log");
		}
		catch
		{
			LogPath = null;
		}
	}

	public static void Clear()
	{
		if (LogPath == null)
		{
			return;
		}
		try
		{
			lock (Lock)
			{
				_writer?.Dispose();
				_writer = null;
				try
				{
					if (File.Exists(LogPath))
					{
						string text = Path.ChangeExtension(LogPath, ".prev.log");
						if (File.Exists(text))
						{
							File.Delete(text);
						}
						File.Move(LogPath, text);
					}
				}
				catch
				{
				}
				_writer = new StreamWriter(new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				{
					AutoFlush = false
				};
				_written = 0L;
				_capped = false;
				Write($"=== DualWield Debug Log — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
				_writer.Flush();
			}
		}
		catch
		{
			_writer = null;
		}
	}

	public static void Log(string message)
	{
		if (LogPath == null)
		{
			return;
		}
		long started = DwPerf.Start();
		try
		{
			lock (Lock)
			{
				if (_writer == null)
				{
					_writer = new StreamWriter(new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
					{
						AutoFlush = false
					};
				}
				if (_capped)
				{
					return;
				}
				if (!Unlimited && _written > 26214400)
				{
					_capped = true;
					Write($"[{DateTime.Now:HH:mm:ss.fff}] --- log size limit reached, further lines dropped (Debug Mode logs without limit) ---\n");
					_writer.Flush();
					return;
				}
				Write($"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
				DateTime utcNow = DateTime.UtcNow;
				if ((utcNow - _lastFlush).TotalSeconds >= 0.5)
				{
					_writer.Flush();
					_lastFlush = utcNow;
				}
			}
		}
		catch
		{
		}
		finally
		{
			DwPerf.Stop(12, started);
		}
	}

	public static void Flush()
	{
		try
		{
			lock (Lock)
			{
				_writer?.Flush();
			}
		}
		catch
		{
		}
	}

	public static void Verbose(string message)
	{
		if (Unlimited)
		{
			Log(message);
		}
	}

	private static void Write(string line)
	{
		_writer.Write(line);
		_written += line.Length;
	}
}
