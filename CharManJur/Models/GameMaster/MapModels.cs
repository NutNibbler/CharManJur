using System.Text.Json.Serialization;

namespace CharManJur.Models;

// One entry in a campaign's map list.
public class MapInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Map";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

// The campaign's map list: order and names. Each map's content is a separate file.
public class MapCatalog
{
    public List<MapInfo> Maps { get; set; } = new();
}

// A symbol placed on a map. SymbolId says which library symbol it shows.
public class MapSymbol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SymbolId { get; set; }
    public float X { get; set; }          // center, in map pixels
    public float Y { get; set; }
    public float Size { get; set; } = 100f;
    public float Rotation { get; set; }   // degrees, clockwise

    public MapSymbol Clone() => new()
    {
        Id = Id,
        SymbolId = SymbolId,
        X = X,
        Y = Y,
        Size = Size,
        Rotation = Rotation
    };
}

// The content of one map.
public class MapData
{
    public Guid Id { get; set; }
    public string? OriginalFile { get; set; }   // the picture as it was imported, never changed
    public string? ImageFile { get; set; }      // what is drawn: the original, or a 3000 px working copy of a bigger one
    public List<DrawShape> Shapes { get; set; } = new();
    public List<MapSymbol> Symbols { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SymbolKind { Image, Token }

// A symbol in the campaign's library: an image file, or a generic colored token.
public class SymbolDef
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Symbol";
    public SymbolKind Kind { get; set; }
    public string? FileName { get; set; }   // images
    public string? Letter { get; set; }     // tokens: up to 3 characters, may be empty
    public string Color { get; set; } = "Red";
}

public class SymbolLibrary
{
    public List<SymbolDef> Symbols { get; set; } = new();
}