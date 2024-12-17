namespace FfmpegConverter
{
    public interface IFileConverter
    {
        Stream ConvertImage(Stream streamFile, string contentType);
        string ConvertVideo(Stream streamVideo, string contentType);
        bool ConvertImage(string contentType, string pathFile);
        bool ConvertVideo(string contentType, string pathFile);
        Stream GetVideoThumbnail(string pathFile);
    }
}
