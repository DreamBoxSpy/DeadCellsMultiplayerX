using System;
using dc.h3d;
using HaxeProxy.Runtime;
using MessagePack;
using MessagePack.Formatters;

namespace DeadCellsMultiplayerX.Common.Serializers.Data
{
    public class H3DVectorFormatter : IMessagePackFormatter<Vector?>
    {
        public void Serialize(ref MessagePackWriter writer, Vector? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(4);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public Vector? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return new Vector(Ref<double>.In(0), Ref<double>.In(0), Ref<double>.In(0), Ref<double>.In(0));

            double x = reader.ReadDouble();
            double y = reader.ReadDouble();
            double z = reader.ReadDouble();
            double w = reader.ReadDouble();
            return new Vector(Ref<double>.In(x), Ref<double>.In(y), Ref<double>.In(z), Ref<double>.In(w));
        }
    }
}