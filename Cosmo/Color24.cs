using System.Buffers;
using System.Text.Json.Serialization;

using Utf8StringInterpolation;

namespace Cosmo;

public readonly struct Color24
{
	public byte R { get; init; }

    public byte G { get; init; }

    public byte B { get; init; }

	[JsonConstructor]
	public Color24(byte Red, byte Green, byte Blue)
	{
		R = Red;
		G = Green;
		B = Blue;
        _hash = HashCode.Combine(R, G, B);
	}
	
	public void AsForegroundVT(ref Utf8StringWriter<ArrayBufferWriter<byte>> sb)
	{
		sb.AppendFormat($"\u001b[38;2;{R};{G};{B}m");
	}
	public void AsBackgroundVT(ref Utf8StringWriter<ArrayBufferWriter<byte>> sb)
	{
		sb.AppendFormat($"\u001b[48;2;{R};{G};{B}m");
	}

	public static bool operator==(Color24 first, Color24 second) => first.GetHashCode() == second.GetHashCode();
	
	public static bool operator!=(Color24 first, Color24 second) => first.GetHashCode() != second.GetHashCode();
	
	//public static bool Equals(Color24 x, Color24 y) => x.Red == y.Red && x.Green == y.Green && x.Blue == y.Blue;
	
	public override bool Equals(object obj) => GetHashCode() == obj.GetHashCode();

	private readonly int _hash;

	public override int GetHashCode() => _hash;

	#region Hardcoded Colors
	/// <summary>
	/// Generated using AI, sue me.
	/// </summary>

	// Common basics
	public static readonly Color24 White = new(255, 255, 255);
	public static readonly Color24 Black = new(0, 0, 0);
	public static readonly Color24 Gray = new(128, 128, 128);
	public static readonly Color24 LightGray = new(211, 211, 211); // pale neutral gray
	public static readonly Color24 DarkGray = new(64, 64, 64); // deep neutral gray

	// Primary and close variants
	public static readonly Color24 Red = new(255, 0, 0);
	public static readonly Color24 DarkRed = new(139, 0, 0); // deep red, almost maroon
	public static readonly Color24 Crimson = new(220, 20, 60); // strong bluish-red
	public static readonly Color24 Pink = new(255, 192, 203); // soft pink
	public static readonly Color24 HotPink = new(255, 105, 180); // bright pink

	public static readonly Color24 Green = new(0, 255, 0);
	public static readonly Color24 DarkGreen = new(0, 100, 0); // forest green
	public static readonly Color24 Lime = new(0, 255, 0); // alias for bright green
	public static readonly Color24 LimeGreen = new(50, 205, 50); // vivid lime-like green
	public static readonly Color24 SeaGreen = new(46, 139, 87); // muted green with blue tint
	public static readonly Color24 Olive = new(128, 128, 0); // dull yellow-green
	public static readonly Color24 OliveDrab = new(107, 142, 35); // earthy olive

	public static readonly Color24 Blue = new(0, 0, 255);
	public static readonly Color24 Navy = new(0, 0, 128); // very dark blue
	public static readonly Color24 SkyBlue = new(135, 206, 235); // light airy blue
	public static readonly Color24 DodgerBlue = new(30, 144, 255); // vivid medium blue
	public static readonly Color24 SteelBlue = new(70, 130, 180); // muted cool blue
	public static readonly Color24 MidnightBlue = new(25, 25, 112); // very dark blue with slight purple

	// Cyan / Teal family
	public static readonly Color24 Cyan = new(0, 255, 255); // bright aqua
	public static readonly Color24 Aqua = Cyan; // alias
	public static readonly Color24 Teal = new(0, 128, 128); // dark cyan
	public static readonly Color24 Turquoise = new(64, 224, 208); // greenish-blue
	public static readonly Color24 LightSeaGreen = new(32, 178, 170); // sea-green tint
	public static readonly Color24 MediumTurquoise = new(72, 209, 204);

	// Purple / Magenta family
	public static readonly Color24 Magenta = new(255, 0, 255); // vivid fuchsia
	public static readonly Color24 Fuchsia = Magenta; // alias
	public static readonly Color24 Purple = new(128, 0, 128); // balanced purple
	public static readonly Color24 Indigo = new(75, 0, 130); // deep purple-blue
	public static readonly Color24 Violet = new(238, 130, 238); // pale purple
	public static readonly Color24 Plum = new(221, 160, 221); // soft purple

	// Browns / earth tones
	public static readonly Color24 Brown = new(165, 42, 42); // warm medium brown
	public static readonly Color24 SaddleBrown = new(139, 69, 19); // dark warm brown
	public static readonly Color24 Sienna = new(160, 82, 45); // reddish brown
	public static readonly Color24 Peru = new(205, 133, 63); // tan-brown
	public static readonly Color24 Tan = new(210, 180, 140); // light brown
	public static readonly Color24 Chocolate = new(210, 105, 30); // rich brown

	// Yellows / golds
	public static readonly Color24 Yellow = new(255, 255, 0); // bright primary yellow
	public static readonly Color24 Gold = new(255, 215, 0); // metallic warm yellow (existing)
	public static readonly Color24 Goldenrod = new(218, 165, 32); // warm golden brown
	public static readonly Color24 Khaki = new(240, 230, 140); // sandy pale yellow
	public static readonly Color24 LemonChiffon = new(255, 250, 205); // pale buttery yellow
	public static readonly Color24 PaleGoldenrod = new(238, 232, 170); // muted pale gold

	// Oranges
	public static readonly Color24 Orange = new(255, 165, 0); // vivid orange
	public static readonly Color24 DarkOrange = new(255, 140, 0); // deeper orange
	public static readonly Color24 Coral = new(255, 127, 80); // warm pinkish-orange
	public static readonly Color24 Tomato = new(255, 99, 71); // red-orange, tomato-like
	public static readonly Color24 PeachPuff = new(255, 218, 185); // light peach

	// Salmon / pink-oranges
	public static readonly Color24 Salmon = new(250, 128, 114); // soft pink-orange
	public static readonly Color24 LightSalmon = new(255, 160, 122); // pale salmon

	// Whites and near-whites
	public static readonly Color24 Ivory = new(255, 255, 240); // warm off-white
	public static readonly Color24 FloralWhite = new(255, 250, 240); // very pale warm white
	public static readonly Color24 AntiqueWhite = new(250, 235, 215); // old-paper white
	public static readonly Color24 Linen = new(250, 240, 230); // soft off-white
	public static readonly Color24 Seashell = new(255, 245, 238); // pale pinkish-white
	public static readonly Color24 Snow = new(255, 250, 250); // crisp white with tiny cool tint

	// Greens - lighter and pastel
	public static readonly Color24 MintCream = new(245, 255, 250); // extremely pale green
	public static readonly Color24 Honeydew = new(240, 255, 240); // pale greenish-white
	public static readonly Color24 PaleGreen = new(152, 251, 152); // soft pale green
	public static readonly Color24 SpringGreen = new(0, 255, 127); // bright spring green
	public static readonly Color24 MediumSpringGreen = new(0, 250, 154);

	// Blues - light variants
	public static readonly Color24 PowderBlue = new(176, 224, 230); // soft blue
	public static readonly Color24 LightBlue = new(173, 216, 230); // gentle baby blue
	public static readonly Color24 LightSkyBlue = new(135, 206, 250); // pale sky blue
	public static readonly Color24 CornflowerBlue = new(100, 149, 237); // classic medium blue

	// Neutrals and accents
	public static readonly Color24 Silver = new(192, 192, 192); // metallic light gray
	public static readonly Color24 Gainsboro = new(220, 220, 220); // very light gray
	public static readonly Color24 Beige = new(245, 245, 220); // warm neutral
	public static readonly Color24 OldLace = new(253, 245, 230); // delicate cream

	// Miscellaneous named colors that are commonly used
	public static readonly Color24 CoralRed = new(255, 64, 64); // intense coral-tinted red
	public static readonly Color24 RebeccaPurple = new(102, 51, 153); // web-safe deep purple
	public static readonly Color24 MediumVioletRed = new(199, 21, 133); // vivid reddish-purple
	public static readonly Color24 DarkOrchid = new(153, 50, 204); // deep purple-pink

	// Aliases for clarity (descriptive alternate names)
	public static readonly Color24 PinkishOrange = Coral; // describes Coral
	public static readonly Color24 SoftPeach = PeachPuff; // describes PeachPuff
	#endregion
}