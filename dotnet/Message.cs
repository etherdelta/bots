using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EhterDelta.Bots.Dontnet
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
            if (messageString.StartsWith("42"))
            {
                messageString = messageString.Remove(0, 2);
                var tmpData = JsonConvert.DeserializeObject(messageString);

                if (tmpData != null)
                {
                    if (tmpData.GetType() == typeof(JArray))
                    {
                        var array = (JArray)tmpData;
                        if (array.Count > 0 && array[0].GetType() == typeof(JValue))
                        {
                            message.Event = array[0].ToString();
                        }

                        if (array.Count > 1)
                        {
                            message.Data = (object)array[1];
                        }
                    }
                }

            }

            return message;
        }
    }
}