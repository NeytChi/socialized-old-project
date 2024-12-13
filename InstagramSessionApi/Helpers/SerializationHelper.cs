using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace InstagramApiSharp.Helpers
{
    public class SerializationHelper
    {
        public static Stream SerializeToStream(object o)
        {
            var stream = new MemoryStream();
            string json = SerializeToString(o);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            stream = new MemoryStream(bytes);
            return stream;
        }
        public static T DeserializeFromStream<T>(Stream stream)
        {
            var json = new StreamReader(stream).ReadToEnd();
            return DeserializeFromString<T>(json);
        }
        public static string SerializeToString(object o)
        {
            return JsonConvert.SerializeObject(o);
        }

        public static T DeserializeFromString<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}