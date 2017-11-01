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
            Converters = new List<JsonConverter>
            {
                new ClaimConverter()
            }
        };
    }

    internal class ClaimConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Claim);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = (string) jo["Type"];
            if (string.IsNullOrEmpty(type))
            {
                return null;
            }
            var value = (string) jo["Value"];
            var valueType = (string) jo["ValueType"];
            var issuer = (string) jo["Issuer"];
            var originalIssuer = (string) jo["OriginalIssuer"];
            return new Claim(type, value, valueType, issuer, originalIssuer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public interface ISerializationSettings
    {
        JsonSerializerSettings JsonSettings { get; }
    }
}