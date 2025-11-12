
namespace Cosmo;

public enum Style : byte
{
	None		= 0,
	Bold		= 1,
	Dim			= 2,
	Italic		= 4,
	Underlined	= 8,
	Blink		= 16,
	Inverted	= 32,
	CrossedOut	= 64
}

public static class CodeHelper
{
	public static byte GetMask(this Style Style) => Style switch
	{
		Style.Bold			=> 0b00000001,
		Style.Dim			=> 0b00000010,
		Style.Italic		=> 0b00000100,
		Style.Underlined	=> 0b00001000,
		Style.Blink			=> 0b00010000,
		Style.Inverted		=> 0b00100000,
		Style.CrossedOut	=> 0b01000000,

		Style.None			=> 0b00000000,
		_						=> 0b00000000
	};
	
	public static byte GetCode(this Style Style) => Style switch
	{
		Style.Bold			=> 1,
		Style.Dim			=> 2,
		Style.Italic		=> 3,
		Style.Underlined	=> 4,
		Style.Blink			=> 5,
		Style.Inverted		=> 7,
		Style.CrossedOut	=> 9,

		Style.None			=> 0,
		_						=> 0
	};
	
	public static byte GetResetCode(this Style Style) => Style switch
	{
		Style.Bold			=> 22,
		Style.Dim			=> 22,
		Style.Italic		=> 23,
		Style.Underlined	=> 24,
		Style.Blink			=> 25,
		Style.Inverted		=> 27,
		Style.CrossedOut	=> 29,

		Style.None			=> 0,
		_						=> 0
	};
}