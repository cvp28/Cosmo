﻿using System.Buffers;
using System.Diagnostics;

using Utf8StringInterpolation;

namespace Cosmo;

public unsafe partial class Renderer
{
	private Thread RenderThread;
	private bool DoRender = false;

	private DoubleRenderBuffer RenderBuffers = new(300);

	private RenderBuffer BackBuffer => RenderBuffers.BackBuffer;
	private RenderBuffer FrontBuffer => RenderBuffers.FrontBuffer;

    /// <summary>
    /// Limits the render thread to run at the specified number of iterations per second
    /// </summary>
    public int RenderThreadLimiter = 0;
    private SleepState RenderThreadSleepState;

    private void RenderThreadProc()
	{
	loop_start:
		var RenderThreadStartTicks = Stopwatch.GetTimestamp();

		RenderThreadMTWait = ExecuteTimed(delegate
		{
			while (!DoRender) Thread.Yield();
		});

		WriteBuffers.BackBuffer = Utf8String.CreateWriter(out var writer);

		//RenderPixels(ref writer);
		var clearEnumerator = FrontBuffer.ToClear.Keys.GetEnumerator();
		var drawEnumerator = FrontBuffer.ToDraw.Values.GetEnumerator();

		while (clearEnumerator.MoveNext())
		{
			var NewPixel = new Pixel(clearEnumerator.Current, ' ', DefaultForeground, DefaultBackground, 0);
			RenderPixel(in NewPixel, ref writer);
		}

		//	while (clearEnumerator.MoveNext())
		//		RenderClearPixel(clearEnumerator.Current, ref writer);

		while (drawEnumerator.MoveNext())
		{
			var NewPixel = drawEnumerator.Current;
			RenderPixel(in NewPixel, ref writer);
		}

		writer.Flush(); // Fill buffer with writer contents
		writer.Dispose();

		RenderThreadWTWait = ExecuteTimed(delegate
		{
			while (DoWrite) Thread.Yield();
		});

		WriteBuffers.Swap();

		DoWrite = true;

		DoRender = false;

		var RenderThreadElapsed = Stopwatch.GetElapsedTime(RenderThreadStartTicks);

        if (FrameRateLimiterEnabled)
            Thread.Sleep(10);

		//Sleep(TimeSpan.FromSeconds(1.0 / RenderThreadLimiter) - RenderThreadElapsed, ref RenderThreadSleepState);

		goto loop_start;
	}

	private void RenderPixels(ref Utf8StringWriter<ArrayBufferWriter<byte>> writer)
	{
		var clearEnumerator = FrontBuffer.ToClear.Keys.GetEnumerator();
		var drawEnumerator = FrontBuffer.ToDraw.Values.GetEnumerator();

		while (clearEnumerator.MoveNext())
		{
			var NewPixel = new Pixel(clearEnumerator.Current, ' ', DefaultForeground, DefaultBackground, 0);
			RenderPixel(in NewPixel, ref writer);
		}
			//RenderClearPixel(clearEnumerator.Current, ref writer);

		while (drawEnumerator.MoveNext())
		{
			var NewPixel = drawEnumerator.Current;
			RenderPixel(in NewPixel, ref writer);
		}
	}

	private Pixel LastPixel;
	private int GetY(int Index) => Index / Width;

	private void RenderPixel(in Pixel NewPixel, ref Utf8StringWriter<ArrayBufferWriter<byte>> writer)
	{
		var LastIndex = LastPixel.Index;
		var CurrentIndex = NewPixel.Index;

		// First, handle position
		if (CurrentIndex - LastIndex != 1)
		{
			if (GetY(LastIndex) == GetY(CurrentIndex) && CurrentIndex > LastIndex)
			{
				int count = CurrentIndex - LastIndex - 1;
				writer.AppendFormat($"\u001b[{count}C");            // If indices are on same line and spaced further than 1 cell apart, shift right
			}
			else
			{
				(int Y, int X) = Math.DivRem(CurrentIndex, Width);
				writer.AppendFormat($"\u001b[{Y + 1};{X + 1}H");    // If anywhere else, set absolute position
			}
		}

		// Then, handle colors and styling
		if (NewPixel.Foreground != LastPixel.Foreground)
			NewPixel.Foreground.AsForegroundVT(ref writer);

		if (NewPixel.Background != LastPixel.Background)
			NewPixel.Background.AsBackgroundVT(ref writer);

		if (NewPixel.Style != LastPixel.Style)
		{
			(byte ResetMask, byte SetMask) = MakeStyleTransitionMasks(LastPixel.Style, NewPixel.Style);

			writer.Append(StyleTransitionSequences[ResetMask, SetMask]);
			//AppendStyleTransitionSequence(ResetMask, SetMask, ref writer);
		}

		writer.Append(NewPixel.Character);
		LastPixel = NewPixel;
	}

	//[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private void RenderClearPixel(int CurrentIndex, ref Utf8StringWriter<ArrayBufferWriter<byte>> writer)
	{
		var LastIndex = LastPixel.Index;
		//var CurrentIndex = NewPixel.Index;

		// First, handle position
		if (CurrentIndex - LastIndex != 1)
		{
			if (GetY(LastIndex) == GetY(CurrentIndex) && CurrentIndex > LastIndex)
			{
				int count = CurrentIndex - LastIndex - 1;
				writer.AppendFormat($"\u001b[{count}C");            // If indices are on same line and spaced further than 1 cell apart, shift right
			}
			else
			{
				(int Y, int X) = Math.DivRem(CurrentIndex, Width);
				writer.AppendFormat($"\u001b[{Y + 1};{X + 1}H");    // If anywhere else, set absolute position
			}
		}

		writer.Append(' ');

		LastPixel = new Pixel(CurrentIndex, ' ', Color24.White, Color24.Black, 0);
	}
}
