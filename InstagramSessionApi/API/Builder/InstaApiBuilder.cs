using System;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Classes;

namespace InstagramApiSharp.API.Builder
{
    public static class InstaApiBuilder
    {
        private static HttpRequestProcessor httpRequestProcessor;
        private static InstaApiVersionType? apiVersionType;
        
        /// <summary>
        ///     Create new API instance
        /// </summary>
        /// <returns>
        ///     API instance
        /// </returns>
        /// <exception cref="ArgumentNullException">User auth data must be specified</exception>
        public static InstagramApi Build()
        {
            try
            {
                InstaApiConstants.TIMEZONE_OFFSET = int.Parse(DateTimeOffset.Now.Offset.TotalSeconds.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); 
            }
            if (httpRequestProcessor == null)
            {
                httpRequestProcessor = HttpRequestProcessor.GetInstance();
            }
            if (apiVersionType == null)
            {
                apiVersionType = InstaApiVersionType.Version86;
            }
            InstagramApi instagramApi = InstagramApi.CreateInstance(apiVersionType.Value);
            return instagramApi;
        }
    }
}