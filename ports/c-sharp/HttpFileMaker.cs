using System.Text;

namespace HttpFileMaker;

/// <summary>
/// Generates HTTP request files in various formats.
/// </summary>
public class HttpFileMaker
{
    public static readonly string[] Formats = { "rest-client", "httpie", "curl" };

    private readonly string _method;
    private readonly string _url;
    private readonly Dictionary<string, string> _headers;
    private readonly string? _body;
    private readonly string _formatType;

    public HttpFileMaker(string method, string url, Dictionary<string, string>? headers = null, 
        string? body = null, string formatType = "rest-client")
    {
        _method = method.ToUpperInvariant();
        _url = url;
        _headers = headers ?? new Dictionary<string, string>();
        _body = body;
        _formatType = formatType;

        if (!Formats.Contains(formatType))
        {
            throw new ArgumentException($"Format must be one of: {string.Join(", ", Formats)}");
        }
    }

    public string GenerateRestClient()
    {
        var lines = new List<string> { $"{_method} {_url}" };

        // Add headers
        foreach (var (key, value) in _headers)
        {
            lines.Add($"{key}: {value}");
        }

        // Add body if present
        if (!string.IsNullOrEmpty(_body))
        {
            lines.Add(""); // Empty line before body
            lines.Add(_body);
        }

        return string.Join("\n", lines);
    }

    public string GenerateHttpie()
    {
        var lines = new List<string> { $"http {_method} {_url}" };

        // Add headers
        foreach (var (key, value) in _headers)
        {
            lines.Add($"  {key}:{value}");
        }

        // Add body if present
        if (!string.IsNullOrEmpty(_body))
        {
            lines.Add($"  <<< '{_body}'");
        }

        return string.Join(" \\\n", lines);
    }

    public string GenerateCurl()
    {
        var parts = new List<string> { $"curl -X {_method}" };

        // Add headers
        foreach (var (key, value) in _headers)
        {
            parts.Add($"-H '{key}: {value}'");
        }

        // Add body if present
        if (!string.IsNullOrEmpty(_body))
        {
            parts.Add($"-d '{_body}'");
        }

        parts.Add($"'{_url}'");

        return string.Join(" \\\n  ", parts);
    }

    public string Generate()
    {
        return _formatType switch
        {
            "rest-client" => GenerateRestClient(),
            "httpie" => GenerateHttpie(),
            "curl" => GenerateCurl(),
            _ => throw new ArgumentException($"Unknown format: {_formatType}")
        };
    }
}

