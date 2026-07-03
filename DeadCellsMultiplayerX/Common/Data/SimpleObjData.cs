using dc;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class SimpleObjData
    {
        private enum SFieldKind
        {
            Unknown,
            Int,
            Bool,
            Double,
            String
        }
        private record class SFieldInfo(string Name, PropertyInfo Field, SFieldKind Kind);

        private static readonly Dictionary<System.Type, List<SFieldInfo>> types = [];

        public Dictionary<string, int> IntValues { get; set; } = [];
        public Dictionary<string, bool> BoolValues { get; set; } = [];
        public Dictionary<string, double> DoubleValues { get; set; } = [];
        public Dictionary<string, string> StringValues { get; set; } = [];

        public void Serialize(object? obj, System.Type? type)
        {

            IntValues.Clear();
            DoubleValues.Clear();
            BoolValues.Clear();

            if (obj == null)
            {
                return;
            }

            var fields = GetFields(type ?? obj.GetType());
            foreach (var v in fields)
            {
                if (v.Field == null) continue;

                object? val;
                try
                {
                    val = v.Field.GetValue(obj);
                }
                catch
                {
                    continue;
                }

                if (val == null)
                {
                    continue;
                }

                if (v.Kind == SFieldKind.Int)
                {
                    IntValues[v.Name] = (int)val;
                }
                else if (v.Kind == SFieldKind.Bool)
                {
                    BoolValues[v.Name] = (bool)val;
                }
                else if (v.Kind == SFieldKind.Double)
                {
                    DoubleValues[v.Name] = (double)val;
                }
                else if (v.Kind == SFieldKind.String)
                {
                    StringValues[v.Name] = (string)val;
                }
            }
        }

        private static T? GetValue<T>(Dictionary<string, T> dict, string name) where T : struct
        {
            if (dict.TryGetValue(name, out var val))
            {
                return val;
            }
            return default;
        }

        public void Deserialize(object? obj, System.Type? type)
        {
            if (obj == null)
            {
                return;
            }
            var fields = GetFields(type ?? obj.GetType());
            foreach (var v in fields)
            {
                object? val;

                if (v.Kind == SFieldKind.Int)
                {
                    val = GetValue(IntValues, v.Name);
                }
                else if (v.Kind == SFieldKind.Bool)
                {
                    val = GetValue(BoolValues, v.Name);
                }
                else if (v.Kind == SFieldKind.Double)
                {
                    val = GetValue(DoubleValues, v.Name);
                }
                else if (v.Kind == SFieldKind.String)
                {
                    val = StringValues.TryGetValue(v.Name, out var stringVal) ? stringVal : null;
                }
                else
                {
                    continue;
                }

                v.Field.SetValue(obj, val);
            }
        }

        private static List<SFieldInfo> GetFields(System.Type type)
        {
            if (!types.TryGetValue(type, out var fields))
            {
                fields = [];
                var fs = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                foreach (var v in fs)
                {
                    SFieldKind kind;
                    if (v.PropertyType == typeof(int) || v.PropertyType == typeof(int?))
                    {
                        kind = SFieldKind.Int;
                    }
                    else if (v.PropertyType == typeof(bool) || v.PropertyType == typeof(bool?))
                    {
                        kind = SFieldKind.Bool;
                    }
                    else if (v.PropertyType == typeof(double) || v.PropertyType == typeof(double?))
                    {
                        kind = SFieldKind.Double;
                    }
                    else
                    {
                        continue;
                    }

                    fields.Add(new(v.Name, v, kind));
                }
                types[type] = fields;
            }
            return fields;
        }
    }
}
