namespace WukongBench.Infrastructure.Steam;

/// <summary>
/// Minimal reader for Valve's KeyValues (VDF) format, used by Steam for
/// <c>libraryfolders.vdf</c> and <c>appmanifest_*.acf</c>.
/// </summary>
internal sealed class ValveKeyValues
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ValveKeyValues> _children = new(StringComparer.OrdinalIgnoreCase);

    public string? Value { get; private set; }

    public IEnumerable<KeyValuePair<string, string>> Values => _values;

    public IEnumerable<KeyValuePair<string, ValveKeyValues>> Children => _children;

    public string? this[string key] => _values.TryGetValue(key, out var value) ? value : null;

    public ValveKeyValues? Child(string key) => _children.TryGetValue(key, out var child) ? child : null;

    public IEnumerable<ValveKeyValues> ChildValues() => _children.Values;

    /// <summary>Parses a KeyValues document (single root object).</summary>
    public static ValveKeyValues ParseFile(string path) => Parse(File.ReadAllText(path));

    /// <summary>Parses a KeyValues document from text.</summary>
    public static ValveKeyValues Parse(string text)
    {
        var tokenizer = new Tokenizer(text);
        var root = new ValveKeyValues();

        var token = tokenizer.Next();
        if (token is null)
        {
            return root;
        }

        // Skip the root name ("libraryfolders", "AppState", ...) and read its body.
        if (token.StartsWith('"'))
        {
            token = tokenizer.Next();
        }

        if (token == "{")
        {
            root.ReadBody(tokenizer);
        }

        return root;
    }

    private void ReadBody(Tokenizer tokenizer)
    {
        while (true)
        {
            var token = tokenizer.Next();
            if (token is null || token == "}")
            {
                return;
            }

            if (!token.StartsWith('"'))
            {
                continue;
            }

            var key = Tokenizer.Unquote(token);
            var next = tokenizer.Next();
            if (next is null)
            {
                return;
            }

            if (next == "{")
            {
                var child = new ValveKeyValues();
                child.ReadBody(tokenizer);
                _children[key] = child;
            }
            else if (next.StartsWith('"'))
            {
                _values[key] = Tokenizer.Unquote(next);
            }
        }
    }

    private sealed class Tokenizer
    {
        private readonly string _text;
        private int _position;

        public Tokenizer(string text)
        {
            _text = text;
        }

        public string? Next()
        {
            while (_position < _text.Length)
            {
                var current = _text[_position];

                if (char.IsWhiteSpace(current))
                {
                    _position++;
                    continue;
                }

                if (current == '/' && _position + 1 < _text.Length && _text[_position + 1] == '/')
                {
                    while (_position < _text.Length && _text[_position] != '\n')
                    {
                        _position++;
                    }

                    continue;
                }

                if (current == '{' || current == '}')
                {
                    _position++;
                    return current.ToString();
                }

                if (current == '"')
                {
                    var start = _position;
                    _position++;
                    var escaped = false;
                    while (_position < _text.Length)
                    {
                        var ch = _text[_position];
                        if (escaped)
                        {
                            escaped = false;
                        }
                        else if (ch == '\\')
                        {
                            escaped = true;
                        }
                        else if (ch == '"')
                        {
                            _position++;
                            break;
                        }

                        _position++;
                    }

                    return _text[start.._position];
                }

                _position++;
            }

            return null;
        }

        public static string Unquote(string token)
        {
            if (token.Length < 2 || token[0] != '"')
            {
                return token;
            }

            var body = token[1..^1];
            var builder = new System.Text.StringBuilder(body.Length);
            for (var i = 0; i < body.Length; i++)
            {
                if (body[i] == '\\' && i + 1 < body.Length)
                {
                    i++;
                    builder.Append(body[i] switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        _ => body[i],
                    });
                }
                else
                {
                    builder.Append(body[i]);
                }
            }

            return builder.ToString();
        }
    }
}
