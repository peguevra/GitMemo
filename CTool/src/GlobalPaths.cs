namespace CTool;

public class GlobalPaths
{
    public string BaseDir => AppContext.BaseDirectory;

    public string InputDir => Path.Combine(BaseDir, "Input");
    public string OutputDir => Path.Combine(BaseDir, "Output");
    public string WebDir => Path.Combine(BaseDir, "Web");

    public string InputFile => Path.Combine(InputDir, "面会.txt");
    public string JsonFile => Path.Combine(OutputDir, "events.json");
    public string WebJsonFile => Path.Combine(WebDir, "data", "events.json");

    public void Ensure()
    {
        Directory.CreateDirectory(InputDir);
        Directory.CreateDirectory(OutputDir);
        Directory.CreateDirectory(Path.Combine(WebDir, "data"));
    }
}