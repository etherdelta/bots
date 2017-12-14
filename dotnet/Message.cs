using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EhterDelta.Bots.DotNet
{
    internal class Message
    {
        internal Message()
        {
            Data = new { };
        }
        public string Event { get; set; }

        public dynamic Data { get; set; }

        public override string ToString()
        {
            var ret = $"42[\"{this.Event}\", {JsonConvert.SerializeObject(Data)}]";
            return ret;
        }

        internal static Message ParseMessage(string messageString)
        {
            var message = new Message();

            // message is Text/Json
            if (!messageString.StartsWith("42")) return message;
            messageString = messageString.Remove(0, 2);
            var tmpData = JsonConvert.DeserializeObject(messageString);

            if (tmpData == null || tmpData.GetType() != typeof(JArray)) return message;
            var array = (JArray)tmpData;
            if (array.Count > 0 && array[0].GetType() == typeof(JValue))
            {
                message.Event = array[0].ToString();
            }

            if (array.Count > 1)
            {
                message.Data = array[1];
            }

            return message;
        }
    }
}