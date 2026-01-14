# HTTP File Maker

A Python tool for generating HTTP request files in various formats (REST Client, HTTPie, cURL, etc.).

## Features

- Generate HTTP request files from command-line arguments
- Support for multiple output formats
- Easy-to-use CLI interface
- Configurable request methods, headers, and body content

## Installation

```bash
pip install -r requirements.txt
```

## Usage

```bash
python src/http_file_maker.py --help
```

## Examples

```bash
# Generate a GET request
python src/http_file_maker.py GET https://api.example.com/users

# Generate a POST request with JSON body
python src/http_file_maker.py POST https://api.example.com/users \
  --header "Content-Type: application/json" \
  --body '{"name": "John", "email": "john@example.com"}'

# Generate a file in REST Client format
python src/http_file_maker.py GET https://api.example.com/users \
  --format rest-client \
  --output requests.http
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

