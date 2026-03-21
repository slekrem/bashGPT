namespace bashGPT.Tools.Filesystem;

public interface IFilesystemPolicy
{
    bool AllowRead(string absolutePath);
    bool AllowWrite(string absolutePath);
}
