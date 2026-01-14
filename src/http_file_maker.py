#!/usr/bin/env python3
"""
HTTP File Maker - A tool for generating HTTP request files.
"""

import argparse
import sys
from typing import Dict, List, Optional

try:
    from .openapi_parser import generate_http_file_from_openapi
except ImportError:
    from openapi_parser import generate_http_file_from_openapi


class HTTPFileMaker:
    """Generates HTTP request files in various formats."""
    
    FORMATS = ['rest-client', 'httpie', 'curl']
    
    def __init__(self, method: str, url: str, headers: Optional[Dict[str, str]] = None,
                 body: Optional[str] = None, format_type: str = 'rest-client'):
        """
        Initialize HTTP File Maker.
        
        Args:
            method: HTTP method (GET, POST, PUT, DELETE, etc.)
            url: Target URL
            headers: Dictionary of HTTP headers
            body: Request body content
            format_type: Output format ('rest-client', 'httpie', 'curl')
        """
        self.method = method.upper()
        self.url = url
        self.headers = headers or {}
        self.body = body
        self.format_type = format_type
        
        if format_type not in self.FORMATS:
            raise ValueError(f"Format must be one of: {', '.join(self.FORMATS)}")
    
    def generate_rest_client(self) -> str:
        """Generate REST Client format (.http file format)."""
        lines = [f"{self.method} {self.url}"]
        
        # Add headers
        for key, value in self.headers.items():
            lines.append(f"{key}: {value}")
        
        # Add body if present
        if self.body:
            lines.append("")  # Empty line before body
            lines.append(self.body)
        
        return "\n".join(lines)
    
    def generate_httpie(self) -> str:
        """Generate HTTPie format."""
        lines = [f"http {self.method} {self.url}"]
        
        # Add headers
        for key, value in self.headers.items():
            lines.append(f"  {key}:{value}")
        
        # Add body if present
        if self.body:
            lines.append(f"  <<< '{self.body}'")
        
        return " \\\n".join(lines)
    
    def generate_curl(self) -> str:
        """Generate cURL format."""
        parts = [f"curl -X {self.method}"]
        
        # Add headers
        for key, value in self.headers.items():
            parts.append(f"-H '{key}: {value}'")
        
        # Add body if present
        if self.body:
            parts.append(f"-d '{self.body}'")
        
        parts.append(f"'{self.url}'")
        
        return " \\\n  ".join(parts)
    
    def generate(self) -> str:
        """Generate HTTP request file content based on format."""
        if self.format_type == 'rest-client':
            return self.generate_rest_client()
        elif self.format_type == 'httpie':
            return self.generate_httpie()
        elif self.format_type == 'curl':
            return self.generate_curl()
        else:
            raise ValueError(f"Unknown format: {self.format_type}")


def parse_headers(header_strings: List[str]) -> Dict[str, str]:
    """Parse header strings into a dictionary."""
    headers = {}
    for header_str in header_strings:
        if ':' in header_str:
            key, value = header_str.split(':', 1)
            headers[key.strip()] = value.strip()
        else:
            print(f"Warning: Invalid header format '{header_str}', skipping.", file=sys.stderr)
    return headers


def main():
    """Main CLI entry point."""
    parser = argparse.ArgumentParser(
        description='Generate HTTP request files in various formats.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s GET https://api.example.com/users
  %(prog)s POST https://api.example.com/users --header "Content-Type: application/json" --body '{"name": "John"}'
  %(prog)s GET https://api.example.com/users --format curl --output request.sh
  %(prog)s --from-openapi spec.yaml --output api.http
        """
    )
    
    parser.add_argument('--from-openapi', dest='openapi_file', metavar='FILE',
                       help='Generate HTTP file from OpenAPI/Swagger YAML file')
    
    parser.add_argument('method', nargs='?', help='HTTP method (GET, POST, PUT, DELETE, etc.)')
    parser.add_argument('url', nargs='?', help='Target URL')
    parser.add_argument('-H', '--header', dest='headers', action='append', default=[],
                       help='HTTP header (can be used multiple times)')
    parser.add_argument('-b', '--body', help='Request body content')
    parser.add_argument('-f', '--format', choices=HTTPFileMaker.FORMATS, default='rest-client',
                       help='Output format (default: rest-client)')
    parser.add_argument('-o', '--output', help='Output file path (default: stdout)')
    
    args = parser.parse_args()
    
    # Handle OpenAPI file parsing
    if args.openapi_file:
        try:
            content = generate_http_file_from_openapi(args.openapi_file, args.output)
            if not args.output:
                print(content)
            else:
                print(f"HTTP request file written to: {args.output}", file=sys.stderr)
        except Exception as e:
            print(f"Error parsing OpenAPI file: {e}", file=sys.stderr)
            sys.exit(1)
        return
    
    # Validate required arguments for single request mode
    if not args.method or not args.url:
        parser.error("method and url are required unless using --from-openapi")
    
    # Parse headers
    headers = parse_headers(args.headers)
    
    # Generate HTTP file
    try:
        maker = HTTPFileMaker(
            method=args.method,
            url=args.url,
            headers=headers,
            body=args.body,
            format_type=args.format
        )
        
        content = maker.generate()
        
        # Write to file or stdout
        if args.output:
            with open(args.output, 'w') as f:
                f.write(content)
            print(f"HTTP request file written to: {args.output}", file=sys.stderr)
        else:
            print(content)
    
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == '__main__':
    main()

