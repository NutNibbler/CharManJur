namespace CharManJur.Services;

public static class AppPaths
{
    // C:\Users\<you>\AppData\Local\CharManJur
    public static string DataDirectory { get; } = EnsureDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CharManJur"));

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}