# HTTP File Maker - C# Port

A C#/.NET port of the HTTP File Maker tool for generating HTTP request files in various formats (REST Client, HTTPie, cURL, etc.) from command-line arguments or OpenAPI/Swagger specifications.

## Requirements

- .NET 10.0 SDK or later

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run -- [arguments]
```

Or build and run the executable:

```bash
dotnet build -c Release
./bin/Release/net10.0/HttpFileMaker [arguments]
```

## Usage

### Single Request Generation

```bash
# Generate a GET request
dotnet run -- GET https://api.example.com/users

# Generate a POST request with JSON body
dotnet run -- POST https://api.example.com/users \
  --header "Content-Type: application/json" \
  --body '{"name": "John", "email": "john@example.com"}'

# Generate a file in REST Client format
dotnet run -- GET https://api.example.com/users \
  --format rest-client \
  --output requests.http
```

### OpenAPI/Swagger Specification Parsing

```bash
# Generate HTTP file from OpenAPI YAML specification
dotnet run -- --from-openapi ../../_docs/sharelink-api-spec.yaml --output api.http
```

## Features

- Generate HTTP request files from command-line arguments
- Parse OpenAPI/Swagger YAML files and generate HTTP request files for all endpoints
- Support for multiple output formats (REST Client, HTTPie, cURL)
- Easy-to-use CLI interface
- Configurable request methods, headers, and body content
- Automatic extraction of authentication headers, request bodies, and query parameters from OpenAPI specs

## Dependencies

- **YamlDotNet** - For parsing YAML files
- **System.CommandLine** - For command-line argument parsing

## License

MIT




