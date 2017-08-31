using System;
using System.Collections.Generic;
using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fabric.Identity.API
{
    public class SerializationSettings : ISerializationSettings
    {
        public JsonSerializerSettings JsonSettings => new JsonSerializerSettings
        {                     
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,                     
            Converters = new List<JsonConverter> { new ClaimConverter()}
        };

        private class ClaimConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Claim);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                string type = (string)jo["Type"];
                if (string.IsNullOrEmpty(type))
                {
                    return null;
                }
                string value = (string)jo["Value"];
                string valueType = (string)jo["ValueType"];
                string issuer = (string)jo["Issuer"];
                string originalIssuer = (string)jo["OriginalIssuer"];
                return new Claim(type, value, valueType, issuer, originalIssuer);
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }

    public interface ISerializationSettings
    {
        JsonSerializerSettings JsonSettings { get; }
    }
}
