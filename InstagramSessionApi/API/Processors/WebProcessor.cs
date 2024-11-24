using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using InstagramApiSharp.Helpers;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Classes.ResponseWrappers.Web;

namespace InstagramApiSharp.API.Processors
{
    public class WebProcessor
    {
        private readonly HttpHelper _httpHelper;
        private readonly HttpRequestProcessor _httpRequestProcessor;
        public WebProcessor()
        {
            _httpRequestProcessor = HttpRequestProcessor.GetInstance();
            _httpHelper = HttpHelper.GetInstance();
        }
        /// <summary>
        ///     Get self account information like joined date or switched to business account date
        /// </summary>
        public IResult<InstaWebAccountInfo> GetAccountInfo(ref Session session)
        {
            try
            {
                var instaUri = WebUriCreator.GetAccountsDataUri();
                var request = _httpHelper.GetWebRequest(HttpMethod.Get, instaUri, session.device);
                var response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                var html = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaWebAccountInfo> result = Result.Fail($"Error! Status code: {response.StatusCode}",
                    default(InstaWebAccountInfo));
                    result.unexceptedResponse = true;
                    return result;
                }

                var json = html.Result.GetJson();
                if (json.IsEmpty())
                {
                    IResult<InstaWebAccountInfo> result = Result.Fail($"Json response isn't available.",
                    default(InstaWebAccountInfo));
                    result.unexceptedResponse = true;
                    return result;
                }
                var obj = JsonConvert.DeserializeObject<InstaWebContainerResponse>(json);

                if (obj.Entry?.SettingsPages != null)
                {
                    var first = obj.Entry.SettingsPages.FirstOrDefault();
                    if (first != null)
                    {
                        return Result.Success(ConvertersFabric.Instance.GetWebAccountInfoConverter(first).Convert());
                    }
                }
                return Result.Fail($"Date joined isn't available.", default(InstaWebAccountInfo));
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaWebAccountInfo), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, default(InstaWebAccountInfo));
            }
        }
    }
}