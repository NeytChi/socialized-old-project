namespace WebAPI.response
{
    public class AnswerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public AnswerResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}