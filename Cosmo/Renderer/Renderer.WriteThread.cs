using System.Diagnostics;

using Utf8StringInterpolation;

namespace Cosmo;

public unsafe partial class Renderer
{
	private Thread WriteThread;
	private bool DoWrite = false;

	private DoubleFrameBuffer WriteBuffers = new();

	private Utf8StringBuffer FrontWriteBuffer => WriteBuffers.FrontBuffer;
	private Utf8StringBuffer BackWriteBuffer => WriteBuffers.BackBuffer;

	private Action<ReadOnlyMemory<byte>> PlatformWriteStdout;

	/// <summary>
	/// Limits the thread responsible for writing to stdout to run at the specified number of iterations per second
	/// </summary>
    public int WriteThreadLimiter = 0;
	private SleepState WriteThreadSleepState;

    private void WriteThreadProc()
	{
	loop_start:
		var WriteThreadStartTicks = Stopwatch.GetTimestamp();

		WriteThreadWait = ExecuteTimed(delegate
		{
			while (!DoWrite) Thread.Yield();
		});

		if (FrontWriteBuffer.WrittenCount != 0)
			PlatformWriteStdout(FrontWriteBuffer.WrittenMemory);

		DoWrite = false;

		var WriteThreadElapsed = Stopwatch.GetElapsedTime(WriteThreadStartTicks);

		if (FrameRateLimiterEnabled)
			Thread.Sleep(10);

		//Sleep(TimeSpan.FromSeconds(1.0 / WriteThreadLimiter) - WriteThreadElapsed, ref WriteThreadSleepState);

		goto loop_start;
	}
}
