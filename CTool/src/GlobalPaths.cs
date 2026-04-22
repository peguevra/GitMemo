public class GlobalPaths
{
    public string BaseDir => AppContext.BaseDirectory;

    public string RootDir => Path.GetFullPath(
        Path.Combine(BaseDir, "..", "..", "..", "..")
    );

    public string InputDir => Path.Combine(BaseDir, "Input");
    public string OutputDir => Path.Combine(BaseDir, "Output");

    public string DocsDir => Path.Combine(RootDir, "docs");

    public string InputFile => Path.Combine(InputDir, "memo.txt");

    public string JsonFile => Path.Combine(OutputDir, "events.json");

    public string WebJsonFile => Path.Combine(DocsDir, "data", "events.json");

    public void Ensure()
    {
        Directory.CreateDirectory(InputDir);
        Directory.CreateDirectory(OutputDir);
        Directory.CreateDirectory(Path.Combine(DocsDir, "data"));
    }
}