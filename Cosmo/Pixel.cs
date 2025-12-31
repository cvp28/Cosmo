
namespace Cosmo;

public readonly struct Pixel : IEqualityComparer<Pixel>, IComparable<Pixel>
{
	public int Index { get; init; }
	
	public char Character { get; init; }
	
	public Color24 Foreground { get; init; }
	public Color24 Background { get; init; }
	
	public byte Style { get; init; }
	
	internal Pixel(int Index, char Character, Color24 Foreground, Color24 Background, byte Style)
	{
		this.Index = Index;
		this.Character = Character;
		this.Foreground = Foreground;
		this.Background = Background;
		this.Style = Style;
		
		_hash = HashCode.Combine(Index, Character, Foreground, Background, Style);
	}
	
	public static bool operator ==(Pixel x, Pixel y) => x.Equals(y);
	public static bool operator !=(Pixel x, Pixel y) => !x.Equals(y);
	
	public bool Equals(Pixel x, Pixel y) => x.GetHashCode() == y.GetHashCode();
	public override bool Equals(object obj) => GetHashCode() == obj.GetHashCode();

	private readonly int _hash;

	public int GetHashCode(Pixel obj) => obj._hash;
	public override int GetHashCode() => _hash;
	
	public int CompareTo(Pixel other) => GetHashCode().CompareTo(other.GetHashCode());
}