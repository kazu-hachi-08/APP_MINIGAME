using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// セーブ用の小さな JSON 読み書き。Battle は UnityEngine を参照しない（JsonUtility が使えない）ので自前で持つ。
    /// 読むと オブジェクト = Dictionary&lt;string, object&gt; / 配列 = List&lt;object&gt; / 数 = double / 文字列 / bool / null になる。
    /// 壊れた文字列は FormatException を投げる（呼ぶ側で空のデータに戻す）
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new FormatException("JSON が空です");

            var reader = new Reader(json);
            object value = reader.ReadValue();
            reader.SkipSpaces();
            if (!reader.IsEnd) throw new FormatException("JSON の後ろに余計な文字があります");
            return value;
        }

        public static string Quote(string text)
        {
            var builder = new StringBuilder("\"");
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') builder.Append('\\').Append(c);
                else if (c < ' ') builder.Append("\\u").Append(((int)c).ToString("x4"));
                else builder.Append(c);
            }
            return builder.Append('"').ToString();
        }

        public static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private class Reader
        {
            private readonly string _text;
            private int _pos;

            public Reader(string text) => _text = text;

            public bool IsEnd => _pos >= _text.Length;

            public void SkipSpaces()
            {
                while (!IsEnd && char.IsWhiteSpace(_text[_pos])) _pos++;
            }

            public object ReadValue()
            {
                SkipSpaces();
                if (IsEnd) throw new FormatException("JSON が途中で終わっています");

                char c = _text[_pos];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (TryReadWord("true")) return true;
                if (TryReadWord("false")) return false;
                if (TryReadWord("null")) return null;
                return ReadNumber();
            }

            private Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>();
                _pos++;
                if (TryConsume('}')) return result;
                do
                {
                    SkipSpaces();
                    string key = ReadString();
                    Expect(':');
                    result[key] = ReadValue();
                } while (TryConsume(','));
                Expect('}');
                return result;
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                _pos++;
                if (TryConsume(']')) return result;
                do
                {
                    result.Add(ReadValue());
                } while (TryConsume(','));
                Expect(']');
                return result;
            }

            private string ReadString()
            {
                if (IsEnd || _text[_pos] != '"') throw new FormatException("文字列が必要です");

                var builder = new StringBuilder();
                _pos++;
                while (!IsEnd)
                {
                    char c = _text[_pos++];
                    if (c == '"') return builder.ToString();
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }
                    if (IsEnd) break;
                    char escaped = _text[_pos++];
                    if (escaped == 'u') builder.Append(ReadUnicode());
                    else builder.Append(Unescape(escaped));
                }
                throw new FormatException("文字列が閉じていません");
            }

            private char ReadUnicode()
            {
                const int hexLength = 4;
                if (_pos + hexLength > _text.Length) throw new FormatException("\\u の後ろが足りません");

                string hex = _text.Substring(_pos, hexLength);
                _pos += hexLength;
                return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            private static char Unescape(char c)
            {
                switch (c)
                {
                    case 'n': return '\n';
                    case 't': return '\t';
                    case 'r': return '\r';
                    case 'b': return '\b';
                    case 'f': return '\f';
                    default: return c;
                }
            }

            private double ReadNumber()
            {
                int start = _pos;
                while (!IsEnd && "+-0123456789.eE".IndexOf(_text[_pos]) >= 0) _pos++;
                string token = _text.Substring(start, _pos - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    throw new FormatException($"数として読めません: {token}");
                }
                return value;
            }

            private bool TryReadWord(string word)
            {
                if (string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0) return false;
                _pos += word.Length;
                return true;
            }

            private bool TryConsume(char c)
            {
                SkipSpaces();
                if (IsEnd || _text[_pos] != c) return false;
                _pos++;
                return true;
            }

            private void Expect(char c)
            {
                if (!TryConsume(c)) throw new FormatException($"'{c}' が必要です");
            }
        }
    }
}
