﻿
using Collections.Pooled;

namespace Cosmo;

public partial class Renderer
{
	public Color24 DefaultForeground { get; set; } = Color24.White;
	public Color24 DefaultBackground { get; set; } = Color24.Black;
	
	public void WriteAt(int X, int Y, string Text) => WriteAt(X, Y, Text, DefaultForeground, DefaultBackground, StyleCode.None);
	
	public void WriteAt(int X, int Y, char Character) => WriteAt(X, Y, Character, DefaultForeground, DefaultBackground, StyleCode.None);
	
	public void WriteAt(int X, int Y, string Text, Color24 Foreground, Color24 Background, StyleCode Style)
	{
		for (int i = 0; i < Text.Length; i++)
			WriteAt(X + i, Y, Text[i], Foreground, Background, Style);
	}
	
	public void WriteAt(int X, int Y, char Character, Color24 Foreground, Color24 Background, StyleCode Style)
	{
		int Index = ScreenIX(X, Y);
		TryModifyPixel(Index, Character, Foreground, Background, (byte) Style);
	}
	
	public void DrawBox(int X, int Y, int Width, int Height, string Title = "")
	{
		var TopLeftIndex = ScreenIX(X, Y);
		var TopRightIndex = ScreenIX(X + Width - 1, Y);
		var BottomLeftIndex = ScreenIX(X, Y + Height - 1);
		var BottomRightIndex = ScreenIX(X + Width - 1, Y + Height - 1);
		
		TryModifyPixel(TopLeftIndex, '╭', Color24.White, Color24.Black, 0);
		TryModifyPixel(TopRightIndex, '╮', Color24.White, Color24.Black, 0);
		TryModifyPixel(BottomLeftIndex, '╰', Color24.White, Color24.Black, 0);
		TryModifyPixel(BottomRightIndex, '╯', Color24.White, Color24.Black, 0);
		
		for (int i = 0; i < Width - 2; i++)
		{
			var TopIndex = ScreenIX(X + 1 + i, Y);
			var BottomIndex = ScreenIX(X + 1 + i, Y + Height - 1);
			
			TryModifyPixel(TopIndex, '─', Color24.White, Color24.Black, 0);
			TryModifyPixel(BottomIndex, '─', Color24.White, Color24.Black, 0);
		}
		
		for (int i = 1; i < Height - 1; i++)
		{
			var NextLeftIndex = ScreenIX(X, Y + i);
			var NextRightIndex = ScreenIX(X + Width - 1, Y + i);
			
			TryModifyPixel(NextLeftIndex, '│', Color24.White, Color24.Black, 0);
			TryModifyPixel(NextRightIndex, '│', Color24.White, Color24.Black, 0);
		}
	}
	
	public void DrawLine(int x1, int y1, int x2, int y2)
	{
		// Blatantly stolen from DotDotDot on GitHub
		// Testing purposes only
		
		// Bresenham's line algorithm
		int x_diff = x1 > x2 ? x1 - x2 : x2 - x1;
		int y_diff = y1 > y2 ? y1 - y2 : y2 - y1;
		int x_direction = x1 <= x2 ? 1 : -1;
		int y_direction = y1 <= y2 ? 1 : -1;

		int err = (x_diff > y_diff ? x_diff : -y_diff) / 2;
		while (true)
		{
			TryModifyPixel(ScreenIX(x1, y1), '*', Color24.White, Color24.Black, 0);
			if (x1 == x2 && y1 == y2)
			{
				break;
			}
			int err2 = err;
			if (err2 > -x_diff)
			{
				err -= y_diff;
				x1 += x_direction;
			}
			if (err2 < y_diff)
			{
				err += x_diff;
				y1 += y_direction;
			}
		}
	}

	private char[] RenderBlacklist = [ '\n', '\r', '\t' ];

	public void DrawPixelBuffer(int X, int Y, int ViewWidth, int ViewHeight, int BufferWidth, in Span<Pixel> Buffer)
	{
		int OffX = 0;
		int OffY = 0;

		for (int i = 0; i < ViewWidth * ViewHeight; i++)
		{
			var CurrentPixel = Buffer[OffX + BufferWidth * OffY];

			if (!RenderBlacklist.Contains(CurrentPixel.Character))
				TryModifyPixel(ScreenIX(OffX, OffY), CurrentPixel.Character, CurrentPixel.Foreground, CurrentPixel.Background, CurrentPixel.Style);

			if (OffX == ViewWidth - 1)
			{
				OffX = 0;
				OffY++;
			}
			else
			{
				OffX++;
			}
		}
	}

	// Entry point for modifying the screen
	private void TryModifyPixel(int Index, char Character, Color24 Foreground, Color24 Background, byte StyleMask)
	{
		// If this space is not actually going to be visible, cull it
		if (Character == ' ' && Background == DefaultBackground)
			return;
		
		var NewPixel = new Pixel(Index, Character, Foreground, Background, StyleMask);
		
		// If we are adding a pixel to a screen location that was already going to be cleared,
		// then remove that location from the clear list since we are going to overwrite or skip it anyways
		if (BackBuffer.ToClear.TryGetValue(Index, out Pixel OldPixel))
		{
			BackBuffer.ToClear.Remove(Index);
			
			if (OldPixel == NewPixel)
				BackBuffer.ToSkip.Add(NewPixel);
			else
				BackBuffer.ToDraw[Index] = NewPixel;
		}
		else
		{
			BackBuffer.ToDraw[Index] = NewPixel;
		}
		
		return;
	}
}
