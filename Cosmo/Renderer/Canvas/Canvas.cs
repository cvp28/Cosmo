﻿using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Utf8StringInterpolation;

namespace Cosmo;

public unsafe partial class Renderer
{
	public int Width { get; private set; }
	public int Height { get; private set; }
	
	private int TotalCellCount => Width * Height;
	
	public int BufferDumpQuantity = 0;
	
	public Renderer()
	{
		Width = Console.WindowWidth;
		Height = Console.WindowHeight;
		
		PrecacheSequences();
		
		#region Platform-specific Writing Delegates
		if (OperatingSystem.IsWindows())
		{
			var Handle = k32GetStdHandle(-11);
			
			PlatformWriteStdout = delegate (ReadOnlyMemory<byte> Buffer)
			{
				using var hBuffer = Buffer.Pin();
				k32WriteFile(Handle, (byte*) hBuffer.Pointer, (uint) Buffer.Length, out _, nint.Zero);
			};
		}
		else if (OperatingSystem.IsLinux())
		{
			PlatformWriteStdout = delegate (ReadOnlyMemory<byte> Buffer)
			{
				using var hBuffer = Buffer.Pin();
				libcWrite(1, (byte*) hBuffer.Pointer, (uint) Buffer.Length);
			};
		}
		#endregion
		
		// This sounds kinda british...
		#region Threading Init
		RenderThread = new(RenderThreadProc)
		{
			Name = "Render Thread",
			IsBackground = true,
			Priority = ThreadPriority.AboveNormal
		};
		
		WriteThread = new(WriteThreadProc)
		{
			Name = "Write Thread",
			IsBackground = true,
			Priority = ThreadPriority.Normal
		};
		
		RenderThread.Start();
		WriteThread.Start();
		#endregion

		InitScreen();
	}
	
	
	
	private void InitScreen()
	{
		// Set up the screen state (set screen to full white on black and reset cursor to home)
		Console.Write($"\u001b[0m\u001b[38;2;255;255;255m\u001b[48;2;0;0;0m{new string(' ', TotalCellCount)}\u001b[;H");
		
		// Set up render state
		LastPixel = new(0, ' ', Color24.White, Color24.Black, 0);
	}
	
	/// <summary>
	/// Resizes the canvas.
	/// </summary>
	/// <param name="NewWidth">The new width</param>
	/// <param name="NewHeight">The new height</param>
	public void Resize(int NewWidth = 0, int NewHeight = 0)
	{
		while (DoRender);

		if (NewWidth == 0)
			NewWidth = Console.WindowWidth;
		
		if (NewHeight == 0)
			NewHeight = Console.WindowHeight;
		
		Width = NewWidth;
		Height = NewHeight;
		
		Console.Clear();
		
		FrontBuffer.ToDraw.Clear();
		FrontBuffer.ToClear.Clear();
		FrontBuffer.ToSkip.Clear();
		
		BackBuffer.ToDraw.Clear();
		BackBuffer.ToClear.Clear();
		BackBuffer.ToSkip.Clear();

		InitScreen();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ScreenIX(int X, int Y) => Y % Height * Width + (X % Width);

	public void DoBufferDump(int Quantity)
	{
		BufferDumpQuantity = Quantity;
		if (File.Exists(@".\BufferDump.txt"))
			File.Delete(@".\BufferDump.txt");

		File.AppendAllText(@".\BufferDump.txt", "Buffer Dump\n\n");
	}
	
	public TimeSpan MainThreadWait { get; private set; }
	public TimeSpan RenderThreadMTWait { get; private set; }
	public TimeSpan RenderThreadWTWait { get; private set; }
	public TimeSpan WriteThreadWait { get; private set; }
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private TimeSpan ExecuteTimed(Action ActionToMeasure)
	{
		var start = Stopwatch.GetTimestamp();
		ActionToMeasure();
		return Stopwatch.GetElapsedTime(start);
	}
	
	public void Flush()
	{
		// Time our wait for the render thread to finish
		MainThreadWait = ExecuteTimed(delegate
		{
			while (DoRender) Thread.Yield();
		});
		
		RenderBuffers.Swap();
		
		DoRender = true;
		
		BackBuffer.ToClear.Clear();
		BackBuffer.ToDraw.Clear();
		BackBuffer.ToSkip.Clear();
		
		// Accessing FrontBuffer from the main thread would otherwise be wrong
		// But, we are only reading from it here - so it should be fine
		
		foreach (var p in FrontBuffer.ToSkip) BackBuffer.ToClear[p.Index] = p;
		foreach (var p in FrontBuffer.ToDraw) BackBuffer.ToClear[p.Key] = p.Value;
	}
	
	public string[,] StyleTransitionSequences = new string[256,256];

	private void PrecacheSequences()
	{
		for (int r = 0; r < 256; r++)
			for (int s = 0; s < 256; s++)
				StyleTransitionSequences[r, s] = GetStyleTransitionSequence((byte) r, (byte) s);
	}

	/// <summary>
	/// <para>Takes two style masks: a current one and a new one and produces</para>
	/// <br/>
	/// <para>a reset mask (all of the styles that need to be reset)</para>
	/// <br/>
	/// <para>and a set mask (all of the styles that need to be set)</para>
	/// </summary>
	/// <param name="CurrentStyle">The currently applied style mask (all styles that have been flushed to the screen)</param>
	/// <param name="NewStyle">The desired style mask to be applied</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (byte ResetMask, byte SetMask) MakeStyleTransitionMasks(byte CurrentStyle, byte NewStyle)
	{
		byte r = (byte) (~NewStyle & CurrentStyle);
		byte s = (byte) ((NewStyle | r) ^ CurrentStyle);
		
		return (r, s);
	}

	private void AppendStyleTransitionSequence(byte ResetMask, byte SetMask, ref Utf8StringWriter<ArrayBufferWriter<byte>> writer)
	{
		writer.Append("\u001b[");

		if (ResetMask != 0)
			AppendResetSequence(ResetMask, ref writer);

		AppendSetSequence(SetMask, ref writer);

		writer.Append('m');
	}
	
	public void AppendResetSequence(byte ResetMask, ref Utf8StringWriter<ArrayBufferWriter<byte>> writer)
	{
		writer.Append("\u001b[");

		Span<StyleCode> Codes = stackalloc StyleCode[8];

		StyleHelper.UnpackStyle(ResetMask, ref Codes, out int Count);

		for (int i = 0; i < Count; i++)
			if ((ResetMask & Codes[i].GetMask()) >= 1)
			{
				writer.AppendFormat($"{Codes[i].GetResetCode()}");
				if (i != Count - 1) writer.Append(';');
			}
	}
	
	/// <summary>
	/// Produces a single VT escape sequence of style codes chained together using a bitmask
	/// </summary>
	/// <param name="SetMask">The mask to set</param>
	/// <returns>The resulting VT escape sequence</returns>
	public void AppendSetSequence(byte SetMask, ref Utf8StringWriter<ArrayBufferWriter<byte>> writer)
	{
		writer.Append("\u001b[");

		Span<StyleCode> Codes = stackalloc StyleCode[8];

		StyleHelper.UnpackStyle(SetMask, ref Codes, out int Count);

		for (int i = 0; i < Count; i++)
			if ((SetMask & Codes[i].GetMask()) >= 1)
			{
				writer.AppendFormat($"{Codes[i].GetCode()}");
				if (i != Count - 1) writer.Append(';');
			}
	}
	
	private string GetStyleTransitionSequence(byte ResetMask, byte SetMask)
	{
		string temp = "\u001b[";

		if (ResetMask != 0)
			temp += GetResetSequence(ResetMask);

		temp += GetSetSequence(SetMask);
		
		temp = temp.TrimEnd(';');
		temp += 'm';
		
		return temp;
	}
	
	private string GetSetSequence(byte SetMask)
	{
		string temp = string.Empty;

		Span<StyleCode> Codes = stackalloc StyleCode[8];

		StyleHelper.UnpackStyle(SetMask, ref Codes, out int Count);

		for (int i = 0; i < Count; i++)
			if ((SetMask & Codes[i].GetMask()) >= 1)
			{
				temp += $"{Codes[i].GetCode()};";
			}

		return temp;
	}

	private string GetResetSequence(byte ResetMask)
	{
		string temp = string.Empty;

		Span<StyleCode> Codes = stackalloc StyleCode[8];

		StyleHelper.UnpackStyle(ResetMask, ref Codes, out int Count);

		for (int i = 0; i < Count; i++)
			if ((ResetMask & Codes[i].GetMask()) >= 1)
			{
				temp += $"{Codes[i].GetResetCode()};";
			}

		return temp;
	}
}

public static class StyleHelper
{	
	// Packs an array of stylecode enums into a single byte
	public static byte PackStyle(params StyleCode[] StyleCodes)
	{
		byte Temp = 0;

		for (int i = 0; i < StyleCodes.Length; i++)
			Temp = (byte) (Temp | StyleCodes[i].GetMask());

		return Temp;
	}
	
	/// <summary>
	/// <para>Gets every stylecode enum contained in the packed byte and returns them in "Dest"</para>
	/// <para>This allows client code to easily prevent heap allocations from parsing packed styles</para>
	/// <br/>
	/// <para>(Dest should be a minimum size of 8)</para>
	/// </summary>
	/// <param name="PackedStyle">Packed style byte</param>
	/// <param name="Dest">Unpacked style codes</param>
	/// <param name="Length">Count of style codes that were written to the span</param>
	public static void UnpackStyle(byte PackedStyle, ref Span<StyleCode> Dest, out int Length)
	{
		if (Dest.Length < 8)
		{
			Length = 0;
			return;
		}
		
		int Index = 0;
		
		for (int i = 1; i != 128; i *= 2)
			if ((PackedStyle & i) >= 1)
			{
				Dest[Index] = (StyleCode) i;
				Index++;
			}
		
		Length = Index;
	}
}