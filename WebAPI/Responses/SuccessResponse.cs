namespace WebAPI.Responses
{
    public class SuccessResponse
    {
        public bool Success { get; set; }
        public SuccessResponse(bool success) { Success = success; }
    }
}
