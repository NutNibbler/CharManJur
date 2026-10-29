using System.Text.Json.Serialization;

namespace CharManJur.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScratchPageType { Text, Drawing }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShapeKind { Pen, Line, Rectangle, Circle, Text }

// One entry in a character's page list.
public class ScratchPageInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled";
    public ScratchPageType Type { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

// The page list of one character: order and names. The pages themselves are separate files.
public class ScratchIndex
{
    public Guid CharacterGuid { get; set; }
    public List<ScratchPageInfo> Pages { get; set; } = new();
}

public class DrawPoint
{
    public float X { get; set; }
    public float Y { get; set; }
}

// One drawn shape. Pen uses Points; Line, Rectangle and Circle use X1..Y2 (a circle is center X1,Y1 and a point on its edge X2,Y2);
// Text sits at X1,Y1.
public class DrawShape
{
    public ShapeKind Kind { get; set; }
    public string Color { get; set; } = "Black";   // Black, Red or Blue
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public string? Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DrawPoint>? Points { get; set; }
}

// The content of one page.
public class ScratchPageData
{
    public Guid Id { get; set; }
    public ScratchPageType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<DrawShape> Shapes { get; set; } = new();
    public string? BackgroundImage { get; set; }
}