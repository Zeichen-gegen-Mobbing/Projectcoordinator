using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZgM.ProjectCoordinator.Shared
{
    internal class IdJsonConverter<Id, BaseType> : JsonConverter<Id> where Id : AbstractId<Id, BaseType>
    {
        public override Id? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString()!;
            BaseType id;

            if (typeof(BaseType) == typeof(string))
            {
                id = (BaseType)(object)value;
            }
            else
            {
                id = (BaseType)Activator.CreateInstance(typeof(BaseType), value)!;
            }

            return (Id)Activator.CreateInstance(typeof(Id), id)!;
        }

        public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
