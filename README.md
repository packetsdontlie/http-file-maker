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

```bash
pip install -r requirements.txt
```

## Usage

```bash
python src/http_file_maker.py --help
```

## Examples

### Single Request Generation

```bash
# Generate a GET request
python3 src/http_file_maker.py GET https://api.example.com/users

# Generate a POST request with JSON body
python3 src/http_file_maker.py POST https://api.example.com/users \
  --header "Content-Type: application/json" \
  --body '{"name": "John", "email": "john@example.com"}'

# Generate a file in REST Client format
python3 src/http_file_maker.py GET https://api.example.com/users \
  --format rest-client \
  --output requests.http
```

### OpenAPI/Swagger Specification Parsing

```bash
# Generate HTTP file from OpenAPI YAML specification
python3 src/http_file_maker.py --from-openapi _docs/sharelink-api-spec.yaml --output api.http

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

## License

MIT

