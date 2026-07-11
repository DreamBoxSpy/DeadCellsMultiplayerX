using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.libs.heaps.slib;
using MessagePack;
using MessagePack.Formatters;

namespace DeadCellsMultiplayerX.Common.Serializers.Data
{
    public class SLIBSpritePivotFormatter : IMessagePackFormatter<SpritePivot?>
    {
        public SpritePivot? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            reader.ReadArrayHeader();

            return new SpritePivot
            {
                isUndefined = reader.ReadBoolean(),
                usingFactor = reader.ReadBoolean(),
                coordX = reader.ReadDouble(),
                coordY = reader.ReadDouble(),
                centerFactorX = reader.ReadDouble(),
                centerFactorY = reader.ReadDouble(),
            };
        }

        public void Serialize(ref MessagePackWriter writer, SpritePivot? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(6);
            writer.Write(value.isUndefined);
            writer.Write(value.usingFactor);
            writer.Write(value.coordX);
            writer.Write(value.coordY);
            writer.Write(value.centerFactorX);
            writer.Write(value.centerFactorY);
        }
    }
}