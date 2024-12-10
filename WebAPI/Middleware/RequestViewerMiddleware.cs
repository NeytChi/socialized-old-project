using Microsoft.Extensions.Primitives;

namespace WebAPI.Middleware
{
    public class RequestViewerMiddleware
    {
        private readonly RequestDelegate next;

        public RequestViewerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine("Request: ");
            Console.WriteLine("\tMethod: " + context.Request.Method);
            Console.WriteLine("\tPath: " + context.Request.Path);
            Console.WriteLine("\tProtocol: " + context.Request.Protocol);
            Console.WriteLine("\tContentLength: " + context.Request.ContentLength);
            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                Console.WriteLine("\t" + header.Key + ": " + header.Value.ToString());
            }
            //context.Request.EnableRewind();
            using (var reader = new StreamReader(context.Request.Body))
            {
                var body = reader.ReadToEnd();
                Console.WriteLine("\tBody: " + body);
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                //await _next.Invoke(context);
                Stream originalBodyStream = context.Response.Body;
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;
                    //Continue down the Middleware pipeline, eventually returning to this class
                    await next(context);
                    await FormatResponse(context.Response);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
        }
        private async Task FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string text = await new StreamReader(response.Body).ReadToEndAsync();
            Console.WriteLine("Response:");
            Console.WriteLine("\tStatusCode: " + response.StatusCode);
            foreach (KeyValuePair<string, StringValues> header in response.Headers)
            {
                Console.WriteLine("\t" + header.Key + ": " + header.Value.ToString());
            }
            Console.WriteLine("\tBody: \r\n" + text);
            response.Body.Seek(0, SeekOrigin.Begin);
        }
    }
}