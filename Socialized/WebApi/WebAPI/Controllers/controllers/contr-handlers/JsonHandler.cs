using Serilog.Core;
using Newtonsoft.Json.Linq;

namespace Controllers
{
    public class JsonHandler : BaseHandler, IHandlerGS
    {
        public JsonHandler(Logger log)
        {
            this.log = log;
        }
        public Logger log;
        
        public JToken handle(ref JObject json, string key, JTokenType tokenType, ref string message)
        {
            if (json != null)
                if (CheckJsonKey(ref json, key, ref message))
                    return GetTokenValue(ref json, key, tokenType, ref message);
            else
                message = "Json is null.";
            return null;
        }
        public JToken handle(JObject json, string key, JTokenType tokenType)
        {
            if (CheckJsonKey(json, key))
                return GetTokenValue(json, key, tokenType);
            return null;
        }
        public bool CheckJsonKey(ref JObject json, string key, ref string message)
        {
            if (json != null && !string.IsNullOrEmpty(key)) {
                if (json.ContainsKey(key))
                    return true;
                message = "Json without key -> '"+ key + "'.";
            }
            else
                message = "Json input key is null or empty.";
            return false;
        }
        public bool CheckJsonKey(JObject json, string key)
        {
            if (json != null && !string.IsNullOrEmpty(key)) {
                if (json.ContainsKey(key))
                    return true;
                log.Warning("Json without key -> '"+ key + "'.");
            }
            else
                log.Warning("Json input key is null or empty.");
            return false;
        }
        public JToken GetTokenValue(ref JObject json, string key, JTokenType tokenType, ref string message)
        {
            if (json != null && !string.IsNullOrEmpty(key)) {
                JToken jsonVarible = json.GetValue(key);
                if (jsonVarible.Type == tokenType)
                    return jsonVarible;
                message = "Json key -> '" + key + "' has incorrect type." +
                "Required type -> " + tokenType.ToString() + ".";
            }
            return null;
        }
        public JToken GetTokenValue(JObject json, string key, JTokenType tokenType)
        {
            if (json != null && !string.IsNullOrEmpty(key)) {
                JToken jsonVarible = json.GetValue(key);
                if (jsonVarible.Type == tokenType)
                    return jsonVarible;
                log.Warning("Json key -> '" + key + "' has incorrect type." +
                "Required type -> " + tokenType.ToString() + ".");
            }
            return null;
        }
    }
}