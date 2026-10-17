using CharManJur.Models;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
#if WINDOWS
using Microsoft.Maui.Handlers;
#endif

namespace CharManJur.Services;

// Applies the font chosen in Settings > Skins > Text. Runs once at startup (a new choice needs a restart).
// Windows: after the usual font mapping, the WinUI FontFamily of each text control is replaced by the
// pack font, unless the control explicitly asked for some other font.
public static class SkinFonts
{
    private static readonly string[] DefaultFamilies = { "OpenSansRegular", "OpenSansSemibold" };

#if WINDOWS
    private static Microsoft.UI.Xaml.Media.FontFamily? _family;
#endif

    public static void Register(ISkinService skins)
    {
#if WINDOWS
        var font = skins.AppliedFont;
        if (font == null) return;

        string? family = BuildFontFamily(font);
        if (family == null) return;

        _family = new Microsoft.UI.Xaml.Media.FontFamily(family);

        LabelHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        ButtonHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        EntryHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        EditorHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        PickerHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        SearchBarHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        RadioButtonHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        DatePickerHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
        TimePickerHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, view) => Apply(handler));
#endif
    }

#if WINDOWS
    private static void Apply(IElementHandler handler)
    {
        if (_family == null) return;
        if (handler.VirtualView is not ITextStyle style) return;

        // Controls that asked for a specific non-default font keep it
        string? requested = style.Font.Family;
        if (!string.IsNullOrEmpty(requested) && !DefaultFamilies.Contains(requested, StringComparer.OrdinalIgnoreCase)) return;

        switch (handler.PlatformView)
        {
            case Microsoft.UI.Xaml.Controls.TextBlock textBlock:
                textBlock.FontFamily = _family;
                break;
            case Microsoft.UI.Xaml.Controls.Control control:
                control.FontFamily = _family;
                break;
        }
    }

    // Uses MAUI's own font loader to make the file loadable (same route MAUI uses for its bundled fonts),
    // then names the family inside the file, with the system font as a glyph fallback.
    private static string? BuildFontFamily(SkinFontInfo font)
    {
        try
        {
            var names = ReadFamilyNames(font.FullPath);
            if (names.Count == 0) names.Add(Path.GetFileNameWithoutExtension(font.FileName));

            long stamp = new FileInfo(font.FullPath).LastWriteTimeUtc.Ticks;
            string cacheName = Sanitize($"skin_{font.PackFolder}_{stamp}_{font.FileName}");

            using var stream = File.OpenRead(font.FullPath);
            string? location = new EmbeddedFontLoader().LoadFont(new EmbeddedFont
            {
                FontName = cacheName,
                ResourceStream = stream
            });

            if (string.IsNullOrEmpty(location)) return null;

            return string.Join(", ", names.Select(n => $"{location}#{n}")) + ", Segoe UI";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SkinFonts: could not prepare '{font.FullPath}': {ex.Message} ===");
            return null;
        }
    }

    private static string Sanitize(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '-' ? c : '_');
        }
        return sb.ToString();
    }
#endif

    // Reads the family name(s) from the font's 'name' table (typographic family first, then the classic one)
    private static List<string> ReadFamilyNames(string path)
    {
        var typographic = new List<string>();
        var classic = new List<string>();

        try
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            uint tag = U32(br);
            long start = 0;
            if (tag == 0x74746366)   // 'ttcf' (font collection): use the first font
            {
                fs.Position = 12;
                start = U32(br);
                fs.Position = start;
                tag = U32(br);
            }

            int tables = U16(br);
            fs.Position = start + 12;

            uint nameOffset = 0;
            for (int i = 0; i < tables; i++)
            {
                uint t = U32(br);
                U32(br);   // checksum
                uint offset = U32(br);
                U32(br);   // length
                if (t == 0x6E616D65) nameOffset = offset;   // 'name'
            }

            if (nameOffset == 0) return new List<string>();

            fs.Position = nameOffset;
            U16(br);   // format
            int count = U16(br);
            int stringOffset = U16(br);

            var records = new List<(int Platform, int Lang, int Id, int Length, int Offset)>();
            for (int i = 0; i < count; i++)
            {
                int platform = U16(br);
                U16(br);   // encoding
                int lang = U16(br);
                int id = U16(br);
                int length = U16(br);
                int offset = U16(br);
                if (id == 1 || id == 16) records.Add((platform, lang, id, length, offset));
            }

            foreach (var r in records.OrderBy(r => r.Lang == 0x409 ? 0 : 1))
            {
                fs.Position = nameOffset + stringOffset + r.Offset;
                byte[] bytes = br.ReadBytes(r.Length);

                string text = (r.Platform == 3 || r.Platform == 0)
                    ? Encoding.BigEndianUnicode.GetString(bytes)
                    : Encoding.Latin1.GetString(bytes);

                text = text.Trim();
                if (text.Length == 0) continue;

                (r.Id == 16 ? typographic : classic).Add(text);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SkinFonts: could not read font names from '{path}': {ex.Message} ===");
        }

        return typographic.Concat(classic).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static uint U32(BinaryReader br) => BinaryPrimitives.ReadUInt32BigEndian(br.ReadBytes(4));
    private static int U16(BinaryReader br) => BinaryPrimitives.ReadUInt16BigEndian(br.ReadBytes(2));
}