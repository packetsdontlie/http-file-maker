using System.CommandLine;
using HttpFileMaker;

var rootCommand = new RootCommand("Generate HTTP request files in various formats.");

var fromOpenApiOption = new Option<string?>(
    aliases: new[] { "--from-openapi" },
    description: "Generate HTTP file from OpenAPI/Swagger YAML file")
{
    Arity = ArgumentArity.ExactlyOne
};

var methodArgument = new Argument<string?>(
    name: "method",
    description: "HTTP method (GET, POST, PUT, DELETE, etc.)");

var urlArgument = new Argument<string?>(
    name: "url",
    description: "Target URL");

var headerOption = new Option<List<string>>(
    aliases: new[] { "-H", "--header" },
    description: "HTTP header (can be used multiple times)")
{
    AllowMultipleArgumentsPerToken = true
};

var bodyOption = new Option<string?>(
    aliases: new[] { "-b", "--body" },
    description: "Request body content");

var formatOption = new Option<string>(
    aliases: new[] { "-f", "--format" },
    getDefaultValue: () => "rest-client",
    description: "Output format (default: rest-client)");

formatOption.AddCompletions(HttpFileMaker.HttpFileMaker.Formats);

var outputOption = new Option<string?>(
    aliases: new[] { "-o", "--output" },
    description: "Output file path (default: stdout)");

rootCommand.AddOption(fromOpenApiOption);
rootCommand.AddArgument(methodArgument);
rootCommand.AddArgument(urlArgument);
rootCommand.AddOption(headerOption);
rootCommand.AddOption(bodyOption);
rootCommand.AddOption(formatOption);
rootCommand.AddOption(outputOption);

rootCommand.SetHandler(async (context) =>
{
    var fromOpenApi = context.ParseResult.GetValueForOption(fromOpenApiOption);
    var method = context.ParseResult.GetValueForArgument(methodArgument);
    var url = context.ParseResult.GetValueForArgument(urlArgument);
    var headers = context.ParseResult.GetValueForOption(headerOption) ?? new List<string>();
    var body = context.ParseResult.GetValueForOption(bodyOption);
    var format = context.ParseResult.GetValueForOption(formatOption) ?? "rest-client";
    var output = context.ParseResult.GetValueForOption(outputOption);

    var cancellationToken = context.GetCancellationToken();

    // Handle OpenAPI file parsing
    if (!string.IsNullOrEmpty(fromOpenApi))
    {
        try
        {
            var content = GenerateHttpFileFromOpenApi(fromOpenApi, output);
            if (string.IsNullOrEmpty(output))
            {
                await Console.Out.WriteLineAsync(content);
            }
            else
            {
                await Console.Error.WriteLineAsync($"HTTP request file written to: {output}");
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error parsing OpenAPI file: {ex.Message}");
            context.ExitCode = 1;
        }
        return;
    }

    // Validate required arguments for single request mode
    if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(url))
    {
        await Console.Error.WriteLineAsync("Error: method and url are required unless using --from-openapi");
        context.ExitCode = 1;
        return;
    }

    // Parse headers
    var headerDict = ParseHeaders(headers);

    // Generate HTTP file
    try
    {
        var maker = new HttpFileMaker.HttpFileMaker(method, url, headerDict, body, format);
        var content = maker.Generate();

        // Write to file or stdout
        if (!string.IsNullOrEmpty(output))
        {
            await File.WriteAllTextAsync(output, content, cancellationToken);
            await Console.Error.WriteLineAsync($"HTTP request file written to: {output}");
        }
        else
        {
            await Console.Out.WriteLineAsync(content);
        }
    }
    catch (Exception ex)
    {
        await Console.Error.WriteLineAsync($"Error: {ex.Message}");
        context.ExitCode = 1;
    }
});

return await rootCommand.InvokeAsync(args);

static Dictionary<string, string> ParseHeaders(List<string> headerStrings)
{
    var headers = new Dictionary<string, string>();
    foreach (var headerStr in headerStrings)
    {
        var colonIndex = headerStr.IndexOf(':');
        if (colonIndex > 0)
        {
            var key = headerStr.Substring(0, colonIndex).Trim();
            var value = headerStr.Substring(colonIndex + 1).Trim();
            headers[key] = value;
        }
        else
        {
            Console.Error.WriteLine($"Warning: Invalid header format '{headerStr}', skipping.");
        }
    }
    return headers;
}

static string GenerateHttpFileFromOpenApi(string yamlFile, string? outputFile)
{
    var parser = new OpenApiParser(yamlFile);
    var endpoints = parser.ExtractEndpoints();

    // Group by tags
    var byTag = endpoints
        .GroupBy(e => e.Tags.Count > 0 ? e.Tags[0] : "Other")
        .OrderBy(g => g.Key)
        .ToDictionary(g => g.Key, g => g.ToList());

    var lines = new List<string>
    {
        "### Generated from OpenAPI/Swagger specification",
        $"### Base URL: {parser.BaseUrl}",
        ""
    };

    // Generate requests grouped by tag
    foreach (var (tag, tagEndpoints) in byTag)
    {
        lines.Add($"### {tag}");
        lines.Add("");

        foreach (var endpoint in tagEndpoints)
        {
            // Add comment with summary
            if (!string.IsNullOrEmpty(endpoint.Summary))
            {
                lines.Add($"### {endpoint.Summary}");
            }
            if (!string.IsNullOrEmpty(endpoint.Description))
            {
                // Take first line of description
                var descLine = endpoint.Description.Split('\n')[0].Trim();
                if (!string.IsNullOrEmpty(descLine) && descLine != endpoint.Summary)
                {
                    lines.Add($"### {descLine}");
                }
            }

            // Generate request
            var requestLines = new List<string> { $"{endpoint.Method} {endpoint.Url}" };

            // Add headers
            foreach (var (key, value) in endpoint.Headers)
            {
                requestLines.Add($"{key}: {value}");
            }

            // Add body
            if (!string.IsNullOrEmpty(endpoint.Body))
            {
                requestLines.Add("");
                requestLines.Add(endpoint.Body);
            }

            lines.AddRange(requestLines);
            lines.Add("");
            lines.Add("");
        }
    }

    var content = string.Join("\n", lines);

    if (!string.IsNullOrEmpty(outputFile))
    {
        File.WriteAllText(outputFile, content);
    }

    return content;
}

