using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace SIGTool.Configs
{
    /// <summary>
    /// 零外部依赖轻量级 JSON 解析与反射序列化器（仅依赖基础 mscorlib/System，确保 VS 默认 WinForms 工程无需手动添加任何额外引用即可编译）
    /// </summary>
    public static class SimpleJsonSerializer
    {
        public static string Serialize(object obj, bool pretty = true)
        {
            var sb = new StringBuilder(2048);
            WriteValue(sb, obj, pretty ? 0 : -1);
            return sb.ToString();
        }

        public static T Deserialize<T>(string json) where T : class, new()
        {
            object rootNode = new JsonParser(json).ParseValue();
            return ConvertNode(rootNode, typeof(T)) as T;
        }

        private static void WriteValue(StringBuilder sb, object val, int indent)
        {
            if (val == null)
            {
                sb.Append("null");
                return;
            }

            Type t = val.GetType();
            if (val is string)
            {
                WriteEscapedString(sb, (string)val);
                return;
            }
            if (val is bool)
            {
                sb.Append(((bool)val) ? "true" : "false");
                return;
            }
            if (t.IsEnum)
            {
                WriteEscapedString(sb, val.ToString());
                return;
            }
            if (val is int || val is long || val is short || val is byte)
            {
                sb.Append(Convert.ToString(val, CultureInfo.InvariantCulture));
                return;
            }
            if (val is double || val is float || val is decimal)
            {
                double d = Convert.ToDouble(val, CultureInfo.InvariantCulture);
                if (double.IsNaN(d) || double.IsInfinity(d)) d = 0.0;
                sb.Append(d.ToString("0.####", CultureInfo.InvariantCulture));
                return;
            }

            IDictionary dict = val as IDictionary;
            if (dict != null)
            {
                sb.Append("{");
                bool first = true;
                foreach (DictionaryEntry entry in dict)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    if (indent >= 0)
                    {
                        sb.AppendLine();
                        sb.Append(' ', (indent + 1) * 2);
                    }
                    WriteEscapedString(sb, Convert.ToString(entry.Key));
                    sb.Append(indent >= 0 ? ": " : ":");
                    WriteValue(sb, entry.Value, indent >= 0 ? indent + 1 : -1);
                }
                if (!first && indent >= 0)
                {
                    sb.AppendLine();
                    sb.Append(' ', indent * 2);
                }
                sb.Append("}");
                return;
            }

            IEnumerable list = val as IEnumerable;
            if (list != null)
            {
                sb.Append("[");
                bool first = true;
                foreach (object item in list)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    if (indent >= 0)
                    {
                        sb.AppendLine();
                        sb.Append(' ', (indent + 1) * 2);
                    }
                    WriteValue(sb, item, indent >= 0 ? indent + 1 : -1);
                }
                if (!first && indent >= 0)
                {
                    sb.AppendLine();
                    sb.Append(' ', indent * 2);
                }
                sb.Append("]");
                return;
            }

            // 普通 POCO 对象：序列化全部可读可写公共实例属性
            sb.Append("{");
            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            bool firstProp = true;
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];
                if (!p.CanRead || !p.CanWrite || p.GetIndexParameters().Length > 0) continue;

                if (!firstProp) sb.Append(",");
                firstProp = false;
                if (indent >= 0)
                {
                    sb.AppendLine();
                    sb.Append(' ', (indent + 1) * 2);
                }
                WriteEscapedString(sb, p.Name);
                sb.Append(indent >= 0 ? ": " : ":");
                WriteValue(sb, p.GetValue(val, null), indent >= 0 ? indent + 1 : -1);
            }
            if (!firstProp && indent >= 0)
            {
                sb.AppendLine();
                sb.Append(' ', indent * 2);
            }
            sb.Append("}");
        }

        private static void WriteEscapedString(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (!string.IsNullOrEmpty(s))
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < ' ')
                                sb.AppendFormat("\\u{0:x4}", (int)c);
                            else
                                sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        private static object ConvertNode(object node, Type targetType)
        {
            if (node == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType == typeof(string)) return Convert.ToString(node, CultureInfo.InvariantCulture);
            if (targetType == typeof(int)) return Convert.ToInt32(Math.Round(Convert.ToDouble(node, CultureInfo.InvariantCulture)));
            if (targetType == typeof(long)) return Convert.ToInt64(Math.Round(Convert.ToDouble(node, CultureInfo.InvariantCulture)));
            if (targetType == typeof(double)) return Convert.ToDouble(node, CultureInfo.InvariantCulture);
            if (targetType == typeof(float)) return (float)Convert.ToDouble(node, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return Convert.ToBoolean(node, CultureInfo.InvariantCulture);

            if (targetType.IsEnum)
            {
                if (node is string)
                {
                    try { return Enum.Parse(targetType, (string)node, true); }
                    catch { return Activator.CreateInstance(targetType); }
                }
                return Enum.ToObject(targetType, Convert.ToInt32(node, CultureInfo.InvariantCulture));
            }

            if (targetType.IsGenericType)
            {
                Type genDef = targetType.GetGenericTypeDefinition();
                if (genDef == typeof(List<>))
                {
                    Type elemType = targetType.GetGenericArguments()[0];
                    IList listInst = (IList)Activator.CreateInstance(targetType);
                    List<object> rawList = node as List<object>;
                    if (rawList != null)
                    {
                        for (int i = 0; i < rawList.Count; i++)
                        {
                            listInst.Add(ConvertNode(rawList[i], elemType));
                        }
                    }
                    return listInst;
                }
                if (genDef == typeof(Dictionary<,>))
                {
                    Type valType = targetType.GetGenericArguments()[1];
                    IDictionary dictInst = (IDictionary)Activator.CreateInstance(targetType);
                    Dictionary<string, object> rawObj = node as Dictionary<string, object>;
                    if (rawObj != null)
                    {
                        foreach (var kv in rawObj)
                        {
                            dictInst[kv.Key] = ConvertNode(kv.Value, valType);
                        }
                    }
                    return dictInst;
                }
            }

            Dictionary<string, object> map = node as Dictionary<string, object>;
            if (map != null)
            {
                object instance = Activator.CreateInstance(targetType);
                PropertyInfo[] props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfo p = props[i];
                    if (!p.CanWrite || p.GetIndexParameters().Length > 0) continue;
                    object rawVal;
                    if (map.TryGetValue(p.Name, out rawVal))
                    {
                        p.SetValue(instance, ConvertNode(rawVal, p.PropertyType), null);
                    }
                }
                return instance;
            }

            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        private sealed class JsonParser
        {
            private readonly string _s;
            private int _pos;

            public JsonParser(string s)
            {
                _s = s ?? "";
                _pos = 0;
            }

            public object ParseValue()
            {
                SkipWhitespaceAndComments();
                if (_pos >= _s.Length) return null;

                char c = _s[_pos];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == 't' || c == 'f') return ParseBool();
                if (c == 'n') return ParseNull();
                return ParseNumber();
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                _pos++; // '{'
                SkipWhitespaceAndComments();
                if (_pos < _s.Length && _s[_pos] == '}')
                {
                    _pos++;
                    return dict;
                }

                while (_pos < _s.Length)
                {
                    SkipWhitespaceAndComments();
                    if (_pos < _s.Length && _s[_pos] == '}') { _pos++; break; }
                    string key = ParseString();
                    SkipWhitespaceAndComments();
                    if (_pos < _s.Length && _s[_pos] == ':') _pos++;
                    object val = ParseValue();
                    dict[key] = val;
                    SkipWhitespaceAndComments();
                    if (_pos < _s.Length && _s[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    if (_pos < _s.Length && _s[_pos] == '}')
                    {
                        _pos++;
                        break;
                    }
                    break;
                }
                return dict;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _pos++; // '['
                SkipWhitespaceAndComments();
                if (_pos < _s.Length && _s[_pos] == ']')
                {
                    _pos++;
                    return list;
                }

                while (_pos < _s.Length)
                {
                    SkipWhitespaceAndComments();
                    if (_pos < _s.Length && _s[_pos] == ']') { _pos++; break; }
                    list.Add(ParseValue());
                    SkipWhitespaceAndComments();
                    if (_pos < _s.Length && _s[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    if (_pos < _s.Length && _s[_pos] == ']')
                    {
                        _pos++;
                        break;
                    }
                    break;
                }
                return list;
            }

            private string ParseString()
            {
                if (_s[_pos] != '"') return "";
                _pos++;
                var sb = new StringBuilder();
                while (_pos < _s.Length)
                {
                    char c = _s[_pos++];
                    if (c == '"') break;
                    if (c == '\\' && _pos < _s.Length)
                    {
                        char esc = _s[_pos++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 <= _s.Length)
                                {
                                    int hex;
                                    if (int.TryParse(_s.Substring(_pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex))
                                        sb.Append((char)hex);
                                    _pos += 4;
                                }
                                break;
                            default: sb.Append(esc); break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }

            private bool ParseBool()
            {
                if (_s.Substring(_pos).StartsWith("true", StringComparison.OrdinalIgnoreCase))
                {
                    _pos += 4;
                    return true;
                }
                _pos = Math.Min(_s.Length, _pos + 5);
                return false;
            }

            private object ParseNull()
            {
                _pos = Math.Min(_s.Length, _pos + 4);
                return null;
            }

            private double ParseNumber()
            {
                int start = _pos;
                while (_pos < _s.Length)
                {
                    char c = _s[_pos];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E')
                        _pos++;
                    else
                        break;
                }
                string token = _s.Substring(start, _pos - start);
                double num;
                return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out num) ? num : 0.0;
            }

            private void SkipWhitespaceAndComments()
            {
                while (_pos < _s.Length)
                {
                    char c = _s[_pos];
                    if (char.IsWhiteSpace(c))
                    {
                        _pos++;
                        continue;
                    }
                    if (c == '/' && _pos + 1 < _s.Length)
                    {
                        if (_s[_pos + 1] == '/')
                        {
                            _pos += 2;
                            while (_pos < _s.Length && _s[_pos] != '\n') _pos++;
                            continue;
                        }
                        if (_s[_pos + 1] == '*')
                        {
                            _pos += 2;
                            while (_pos + 1 < _s.Length && !(_s[_pos] == '*' && _s[_pos + 1] == '/')) _pos++;
                            _pos += 2;
                            continue;
                        }
                    }
                    break;
                }
            }
        }
    }
}
