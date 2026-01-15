# HTTP File Maker

A Python tool for generating HTTP request files in various formats (REST Client, HTTPie, cURL, etc.) from command-line arguments or OpenAPI/Swagger specifications.

## Features

- Generate HTTP request files from command-line arguments
- **Parse OpenAPI/Swagger YAML files** and generate HTTP request files for all endpoints
- Support for multiple output formats (REST Client, HTTPie, cURL)
- Easy-to-use CLI interface
- Configurable request methods, headers, and body content
- Automatic extraction of authentication headers, request bodies, and query parameters from OpenAPI specs

## Installation

### From PyPI (when published)

```bash
pip install http-file-maker
```

### From source

```bash
# Clone the repository
git clone https://github.com/packetsdontlie/http-file-maker.git
cd http-file-maker

# Install in development mode
pip install -e .

# Or install dependencies only
pip install -r requirements.txt
```

## Usage

After installation, you can use the `http-file-maker` command:

```bash
http-file-maker --help
```

Or run directly from source:

```bash
python src/http_file_maker.py --help
```

## Examples

### Single Request Generation

```bash
# Generate a GET request
http-file-maker GET https://api.example.com/users

# Generate a POST request with JSON body
http-file-maker POST https://api.example.com/users \
  --header "Content-Type: application/json" \
  --body '{"name": "John", "email": "john@example.com"}'

# Generate a file in REST Client format
http-file-maker GET https://api.example.com/users \
  --format rest-client \
  --output requests.http
```

### OpenAPI/Swagger Specification Parsing

```bash
# Generate HTTP file from OpenAPI YAML specification
http-file-maker --from-openapi _docs/sharelink-api-spec.yaml --output api.http

# This will generate a complete .http file with all endpoints from the spec,
# including authentication headers, request bodies, and query parameters
```

## Development

```bash
# Install development dependencies
pip install -r requirements.txt

# Run tests
pytest
```

## Ports

This project has been ported to other languages:

### C# / .NET

A C# port is available in `ports/c-sharp/`. It provides the same functionality as the Python version.

**Requirements:**
- .NET 10.0 SDK or later

**Building and Running:**
```bash
cd ports/c-sharp
dotnet build
dotnet run -- --help
```

**Example:**
```bash
dotnet run -- GET https://api.example.com/users
dotnet run -- --from-openapi ../../_docs/sharelink-api-spec.yaml --output api.http
```

See `ports/c-sharp/README.md` for more details.

## License

MIT

