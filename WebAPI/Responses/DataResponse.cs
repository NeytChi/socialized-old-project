namespace WebAPI.Responses
{
    public class DataResponse
    {
        public bool Success { get; set; }
        public dynamic Data { get; set; }

        public DataResponse(bool success, dynamic data)
        {
            Success = success;
            Data = data;
        }
    }
}
