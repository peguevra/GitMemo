namespace CTool;

public class GlobalPaths
{
    public string BaseDir => AppContext.BaseDirectory;

    // ★ リポジトリルート（bin/Debug/net8.0 から4階層上）
    public string RootDir => Path.GetFullPath(
        Path.Combine(BaseDir, "..", "..", "..", "..")
    );

    public string InputDir => Path.Combine(BaseDir, "Input");
    public string OutputDir => Path.Combine(BaseDir, "Output");

    // ★ GitHub Pages用（docs）
    public string DocsDir => Path.Combine(RootDir, "docs");

    public string InputFile => Path.Combine(InputDir, "面会.txt");
    public string JsonFile => Path.Combine(OutputDir, "events.json");

    // ★ iPhoneが参照するJSON（ここが最重要）
    public string WebJsonFile => Path.Combine(DocsDir, "data", "events.json");

    public void Ensure()
    {
        Directory.CreateDirectory(InputDir);
        Directory.CreateDirectory(OutputDir);

        // ★ docs/data を確実に作成
        Directory.CreateDirectory(Path.Combine(DocsDir, "data"));
    }
}