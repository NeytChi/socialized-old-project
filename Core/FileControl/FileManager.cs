using Serilog;.

namespace Core.FileControl
{
    public class FileManager : IFileManager
    {
        private readonly string currentDirectory = Directory.GetCurrentDirectory();
        public readonly ILogger Logger;
        public DateTime currentTime = DateTime.Now;
        public string dailyFolder = "/" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "/";

        public FileManager(ILogger logger)
        {
            Logger = logger;
        }
        public virtual string SaveFile(Stream file, string RelativePath)
        {
            string fileName = Guid.NewGuid().ToString();
            ChangeDailyPath();
            string fileRelativePath = "/" + RelativePath + dailyFolder;
            CheckDirectory(fileRelativePath);
            if (SaveTo(file, fileRelativePath, fileName))
            {
                return fileRelativePath + fileName;
            }
            return null;
        }
        public virtual bool SaveTo(Stream file, string relativePath, string fileName)
        {
            if (File.Exists(currentDirectory + relativePath + fileName))
            {
                Logger.Error("Сервер не може зберегти в файловій системі файл з такою самою назвою.");
                return false;
            }
            using (var stream = new FileStream(currentDirectory + fileName, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            Logger.Information($"Був створений новий файл={fileName}.");
            return true;
        }
        public void ChangeDailyPath()
        {
            if (currentTime.Day != DateTime.Now.Day)
            {
                currentTime = DateTime.Now;
                dailyFolder = "/" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "/";
            }
        }
        public void CheckDirectory(string fileRelativePath)
        {
            if (!Directory.Exists(currentDirectory + fileRelativePath))
            {
                Directory.CreateDirectory(currentDirectory + fileRelativePath);
            }
        }
        public void DeleteFile(string relativePath)
        {
            if (File.Exists(currentDirectory + relativePath))
            {
                File.Delete(currentDirectory + relativePath);
                Logger.Information("File was deleted. Relative path ->/" + relativePath);
            }
        }
    }
}