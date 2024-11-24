using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace InstagramApiSharp.Classes
{
    public class HttpRequestProcessor
    {
        private HttpRequestProcessor()
        {

        }
        public static HttpRequestProcessor instance;
        public static HttpRequestProcessor GetInstance()
        {
            if (instance == null)
            {
                instance = new HttpRequestProcessor();
            }
            return instance;
        }
        public HttpResponseMessage SendAsync(HttpRequestMessage requestMessage, HttpClient Client)
        {
            Task<HttpResponseMessage> response = Client.SendAsync(requestMessage);
            response.Wait();
            return response.Result;
        }
        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpClient Client)
        {
            var response = await Client.GetAsync(requestUri);
            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage,
            HttpCompletionOption completionOption, HttpClient Client)
        {
            var response = await Client.SendAsync(requestMessage, completionOption);
            return response;
        }

        public async Task<string> SendAndGetJsonAsync(HttpRequestMessage requestMessage,
            HttpCompletionOption completionOption, HttpClient Client)
        {
            var response = await Client.SendAsync(requestMessage, completionOption);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GeJsonAsync(Uri requestUri, HttpClient Client)
        {
            var response = await Client.GetAsync(requestUri);
            return await response.Content.ReadAsStringAsync();
        }
    }
}