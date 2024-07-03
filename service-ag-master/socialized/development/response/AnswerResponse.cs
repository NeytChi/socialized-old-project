namespace socialized.response
{
    public class AnswerResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public AnswerResponse(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }
    }
}