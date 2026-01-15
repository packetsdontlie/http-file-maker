using System.Text.Json;
using YamlDotNet.Serialization;

namespace HttpFileMaker;

/// <summary>
/// Parse OpenAPI/Swagger YAML files and extract endpoint information.
/// </summary>
public class OpenApiParser
{
    private readonly Dictionary<string, object?> _spec;
    private readonly string _baseUrl;

    public OpenApiParser(string yamlFile)
    {
        var deserializer = new DeserializerBuilder()
            .Build();

        var yamlContent = File.ReadAllText(yamlFile);
        _spec = deserializer.Deserialize<Dictionary<string, object?>>(yamlContent) 
            ?? throw new InvalidOperationException("Failed to parse YAML file");
        
        _baseUrl = BuildBaseUrl();
    }

    public string BaseUrl => _baseUrl;

    private string BuildBaseUrl()
    {
        var schemes = GetValue<List<object>>(_spec, "schemes") ?? new List<object> { "https" };
        var scheme = schemes[0].ToString() ?? "https";
        var host = GetValue<string>(_spec, "host") ?? "";
        var basePath = GetValue<string>(_spec, "basePath") ?? "";

        return $"{scheme}://{host}{basePath}";
    }

    private Dictionary<string, string> GetSecurityHeaders(Dictionary<string, object?> operation)
    {
        var headers = new Dictionary<string, string>();

        // Check operation-level security
        var security = GetValue<List<object>>(operation, "security");
        if (security == null || security.Count == 0)
        {
            // Check global security
            security = GetValue<List<object>>(_spec, "security");
        }

        var securityDefs = GetValue<Dictionary<string, object?>>(_spec, "securityDefinitions") 
            ?? new Dictionary<string, object?>();

        if (security != null)
        {
            foreach (var secObj in security)
            {
                if (secObj is Dictionary<object, object?> sec)
                {
                    foreach (var (secName, _) in sec)
                    {
                        var secNameStr = secName.ToString() ?? "";
                        if (securityDefs.TryGetValue(secNameStr, out var secDefObj) && secDefObj is Dictionary<string, object?> secDef)
                        {
                            var type = GetValue<string>(secDef, "type");
                            var location = GetValue<string>(secDef, "in");
                            if (type == "apiKey" && location == "header")
                            {
                                var headerName = GetValue<string>(secDef, "name") ?? "";
                                var description = GetValue<string>(secDef, "description") ?? "";
                                var tokenValue = description.Contains("Bearer", StringComparison.OrdinalIgnoreCase) 
                                    ? "Bearer <your-token>" 
                                    : "<your-token>";
                                headers[headerName] = tokenValue;
                            }
                        }
                    }
                }
            }
        }

        return headers;
    }

    private string? GetRequestBody(Dictionary<string, object?> operation)
    {
        var parameters = GetValue<List<object>>(operation, "parameters") ?? new List<object>();

        foreach (var paramObj in parameters)
        {
            if (paramObj is Dictionary<string, object?> param)
            {
                var paramIn = GetValue<string>(param, "in");
                if (paramIn == "body")
                {
                    var schema = GetValue<Dictionary<string, object?>>(param, "schema") 
                        ?? new Dictionary<string, object?>();

                    // Try to get example from schema
                    if (schema.TryGetValue("example", out var example))
                    {
                        return JsonSerializer.Serialize(example, new JsonSerializerOptions { WriteIndented = true });
                    }

                    // Try to get example from definition reference
                    var refValue = GetValue<string>(schema, "$ref");
                    if (!string.IsNullOrEmpty(refValue) && refValue.StartsWith("#/definitions/"))
                    {
                        var defName = refValue.Split('/').Last();
                        var definitions = GetValue<Dictionary<string, object?>>(_spec, "definitions") 
                            ?? new Dictionary<string, object?>();
                        
                        if (definitions.TryGetValue(defName, out var defObj) && defObj is Dictionary<string, object?> definition)
                        {
                            if (definition.TryGetValue("example", out var defExample))
                            {
                                return JsonSerializer.Serialize(defExample, new JsonSerializerOptions { WriteIndented = true });
                            }

                            // Generate example from properties
                            if (definition.TryGetValue("properties", out var defPropsObj) && defPropsObj is Dictionary<string, object?> defProperties)
                            {
                                var defExample = GenerateExampleFromProperties(defProperties, definition);
                                return JsonSerializer.Serialize(defExample, new JsonSerializerOptions { WriteIndented = true });
                            }
                        }
                    }
                    // Handle inline schema
                    else if (GetValue<string>(schema, "type") == "object")
                    {
                        if (schema.TryGetValue("properties", out var inlinePropsObj) && inlinePropsObj is Dictionary<string, object?> inlineProperties)
                        {
                            var inlineExample = GenerateExampleFromProperties(inlineProperties, schema);
                            return JsonSerializer.Serialize(inlineExample, new JsonSerializerOptions { WriteIndented = true });
                        }
                    }
                }
            }
        }

        return null;
    }

    private Dictionary<string, object> GenerateExampleFromProperties(
        Dictionary<string, object?> properties, 
        Dictionary<string, object?> definition)
    {
        var example = new Dictionary<string, object>();
        var required = GetValue<List<object>>(definition, "required") ?? new List<object>();
        var requiredStrings = required.Select(r => r.ToString() ?? "").ToList();

        foreach (var (propName, propSchemaObj) in properties)
        {
            if (propSchemaObj is not Dictionary<string, object?> propSchema)
                continue;

            // Use example if provided
            if (propSchema.TryGetValue("example", out var propExample))
            {
                example[propName] = propExample ?? "";
                continue;
            }

            var propType = GetValue<string>(propSchema, "type") ?? "string";

            switch (propType)
            {
                case "string":
                    var format = GetValue<string>(propSchema, "format");
                    example[propName] = format switch
                    {
                        "email" => "user@example.com",
                        "date-time" => "2024-12-31T23:59:59Z",
                        _ => $"<{propName}>"
                    };
                    break;
                case "integer":
                    example[propName] = GetValue<int?>(propSchema, "default") ?? 0;
                    break;
                case "boolean":
                    example[propName] = GetValue<bool?>(propSchema, "default") ?? false;
                    break;
                case "array":
                    var items = GetValue<Dictionary<string, object?>>(propSchema, "items");
                    if (items != null && items.TryGetValue("$ref", out var itemRef) && itemRef is string itemRefStr)
                    {
                        var itemDefName = itemRefStr.Split('/').Last();
                        var definitions = GetValue<Dictionary<string, object?>>(_spec, "definitions") ?? new Dictionary<string, object?>();
                        if (definitions.TryGetValue(itemDefName, out var itemDefObj) && itemDefObj is Dictionary<string, object?> itemDef)
                        {
                            if (itemDef.TryGetValue("properties", out var itemPropsObj) && itemPropsObj is Dictionary<string, object?> itemProps)
                            {
                                var itemExample = GenerateExampleFromProperties(itemProps, itemDef);
                                example[propName] = new[] { itemExample };
                            }
                            else
                            {
                                example[propName] = Array.Empty<object>();
                            }
                        }
                        else
                        {
                            example[propName] = Array.Empty<object>();
                        }
                    }
                    else
                    {
                        example[propName] = Array.Empty<object>();
                    }
                    break;
                case "object":
                    example[propName] = new Dictionary<string, object>();
                    break;
            }

            // Only include required fields or fields we've added
            if (!requiredStrings.Contains(propName) && !example.ContainsKey(propName))
                continue;
        }

        return example;
    }

    private string BuildUrl(string path, Dictionary<string, object?> operation)
    {
        // In OpenAPI 2.0, paths are appended to basePath
        var url = path.StartsWith('/') 
            ? _baseUrl.TrimEnd('/') + path 
            : _baseUrl.TrimEnd('/') + "/" + path;

        // Replace path parameters with examples
        var parameters = GetValue<List<object>>(operation, "parameters") ?? new List<object>();
        foreach (var paramObj in parameters)
        {
            if (paramObj is Dictionary<string, object?> param)
            {
                var paramIn = GetValue<string>(param, "in");
                if (paramIn == "path")
                {
                    var paramName = GetValue<string>(param, "name") ?? "";
                    var example = GetValue<string>(param, "example") ?? $"<{paramName}>";
                    url = url.Replace($"{{{paramName}}}", example);
                }
            }
        }

        // Add query parameters
        var queryParams = new List<string>();
        foreach (var paramObj in parameters)
        {
            if (paramObj is Dictionary<string, object?> param)
            {
                var paramIn = GetValue<string>(param, "in");
                if (paramIn == "query")
                {
                    var paramName = GetValue<string>(param, "name") ?? "";
                    var example = GetValue<string>(param, "example") 
                        ?? GetValue<string>(param, "default") 
                        ?? $"<{paramName}>";
                    queryParams.Add($"{paramName}={example}");
                }
            }
        }

        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return url;
    }

    public List<Endpoint> ExtractEndpoints()
    {
        var endpoints = new List<Endpoint>();
        var paths = GetValue<Dictionary<string, object?>>(_spec, "paths") 
            ?? new Dictionary<string, object?>();

        foreach (var (path, pathItemObj) in paths)
        {
            if (pathItemObj is not Dictionary<string, object?> pathItem)
                continue;

            // Get common parameters for this path
            var commonParams = GetValue<List<object>>(pathItem, "parameters") ?? new List<object>();

            foreach (var (method, operationObj) in pathItem)
            {
                if (!new[] { "get", "post", "put", "patch", "delete", "head", "options" }.Contains(method.ToLowerInvariant()))
                    continue;

                if (operationObj is not Dictionary<string, object?> operation)
                    continue;

                // Merge common parameters with operation parameters
                var opParams = GetValue<List<object>>(operation, "parameters") ?? new List<object>();
                var allParams = new List<object>(commonParams);
                allParams.AddRange(opParams);
                operation["parameters"] = allParams;

                // Build URL
                var url = BuildUrl(path, operation);

                // Get headers
                var headers = GetSecurityHeaders(operation);

                // Add Content-Type if there's a body
                var requestBody = GetRequestBody(operation);
                if (!string.IsNullOrEmpty(requestBody))
                {
                    headers["Content-Type"] = "application/json";
                }

                // Get summary/description
                var summary = GetValue<string>(operation, "summary") ?? "";
                var description = GetValue<string>(operation, "description") ?? "";
                var operationId = GetValue<string>(operation, "operationId") ?? "";
                var tags = GetValue<List<object>>(operation, "tags") ?? new List<object>();

                endpoints.Add(new Endpoint
                {
                    Method = method.ToUpperInvariant(),
                    Url = url,
                    Headers = headers,
                    Body = requestBody,
                    Summary = summary,
                    Description = description,
                    OperationId = operationId,
                    Path = path,
                    Tags = tags.Select(t => t.ToString() ?? "").ToList()
                });
            }
        }

        return endpoints;
    }

    private static T? GetValue<T>(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value == null)
            return default;

        if (value is T directValue)
            return directValue;

        // Handle nested dictionaries
        if (typeof(T) == typeof(Dictionary<string, object?>) && value is Dictionary<object, object?> nestedDict)
        {
            var result = new Dictionary<string, object?>();
            foreach (var (k, v) in nestedDict)
            {
                result[k.ToString() ?? ""] = v;
            }
            return (T)(object)result;
        }

        // Handle lists
        if (typeof(T).IsGenericType && 
            typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            var result = Activator.CreateInstance(listType) as System.Collections.IList;
            if (result != null && value is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    result.Add(item);
                }
                return (T)result!;
            }
        }

        // Try to convert
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}

public class Endpoint
{
    public string Method { get; set; } = "";
    public string Url { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string OperationId { get; set; } = "";
    public string Path { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}

