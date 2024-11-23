namespace Core
{
    public interface IFileManager
    {
        string SaveFile(Stream stream, string RelativePath);
        bool SaveTo(Stream file, string relativePath, string fileName);
    }
}
