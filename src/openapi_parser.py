#!/usr/bin/env python3
"""
OpenAPI/Swagger YAML parser for generating HTTP request files.
"""

import yaml
from typing import Dict, List, Optional, Any
from urllib.parse import urljoin


class OpenAPIParser:
    """Parse OpenAPI/Swagger YAML files and extract endpoint information."""
    
    def __init__(self, yaml_file: str):
        """
        Initialize OpenAPI parser.
        
        Args:
            yaml_file: Path to OpenAPI/Swagger YAML file
        """
        with open(yaml_file, 'r') as f:
            self.spec = yaml.safe_load(f)
        
        self.base_url = self._build_base_url()
    
    def _build_base_url(self) -> str:
        """Build base URL from spec."""
        scheme = self.spec.get('schemes', ['https'])[0]
        host = self.spec.get('host', '')
        base_path = self.spec.get('basePath', '')
        
        return f"{scheme}://{host}{base_path}"
    
    def _get_security_headers(self, operation: Dict[str, Any]) -> Dict[str, str]:
        """Extract security headers from operation."""
        headers = {}
        
        # Check operation-level security
        security = operation.get('security', [])
        if not security:
            # Check global security
            security = self.spec.get('security', [])
        
        security_defs = self.spec.get('securityDefinitions', {})
        
        for sec in security:
            for sec_name, sec_value in sec.items():
                if sec_name in security_defs:
                    sec_def = security_defs[sec_name]
                    if sec_def.get('type') == 'apiKey' and sec_def.get('in') == 'header':
                        header_name = sec_def.get('name', '')
                        # Use placeholder for token
                        headers[header_name] = f"Bearer <your-token>" if 'Bearer' in sec_def.get('description', '') else "<your-token>"
        
        return headers
    
    def _get_request_body(self, operation: Dict[str, Any]) -> Optional[str]:
        """Extract example request body from operation."""
        import json
        
        parameters = operation.get('parameters', [])
        
        for param in parameters:
            if param.get('in') == 'body':
                schema = param.get('schema', {})
                # Try to get example from schema
                if 'example' in schema:
                    return json.dumps(schema['example'], indent=2)
                # Try to get example from definition reference
                ref = schema.get('$ref', '')
                if ref and ref.startswith('#/definitions/'):
                    def_name = ref.split('/')[-1]
                    definition = self.spec.get('definitions', {}).get(def_name, {})
                    if 'example' in definition:
                        return json.dumps(definition['example'], indent=2)
                    # Generate example from properties with better defaults
                    if 'properties' in definition:
                        example = {}
                        required = definition.get('required', [])
                        
                        for prop_name, prop_schema in definition.get('properties', {}).items():
                            # Use example if provided
                            if 'example' in prop_schema:
                                example[prop_name] = prop_schema['example']
                                continue
                            
                            prop_type = prop_schema.get('type', 'string')
                            if prop_type == 'string':
                                # Check for format
                                if prop_schema.get('format') == 'email':
                                    example[prop_name] = "user@example.com"
                                elif prop_schema.get('format') == 'date-time':
                                    example[prop_name] = "2024-12-31T23:59:59Z"
                                else:
                                    example[prop_name] = f"<{prop_name}>"
                            elif prop_type == 'integer':
                                example[prop_name] = prop_schema.get('default', 0)
                            elif prop_type == 'boolean':
                                example[prop_name] = prop_schema.get('default', False)
                            elif prop_type == 'array':
                                items = prop_schema.get('items', {})
                                if items.get('$ref'):
                                    # Reference to another definition
                                    item_def_name = items['$ref'].split('/')[-1]
                                    item_def = self.spec.get('definitions', {}).get(item_def_name, {})
                                    if 'properties' in item_def:
                                        item_example = {}
                                        for item_prop, item_prop_schema in item_def['properties'].items():
                                            if 'example' in item_prop_schema:
                                                item_example[item_prop] = item_prop_schema['example']
                                            else:
                                                item_example[item_prop] = f"<{item_prop}>"
                                        example[prop_name] = [item_example]
                                    else:
                                        example[prop_name] = []
                                else:
                                    example[prop_name] = []
                            elif prop_type == 'object':
                                example[prop_name] = {}
                            
                            # Only include required fields or fields with defaults
                            if prop_name not in required and prop_name not in example:
                                continue
                        
                        return json.dumps(example, indent=2)
                # Handle inline schema (not a reference)
                elif 'type' in schema and schema.get('type') == 'object':
                    if 'properties' in schema:
                        example = {}
                        required = schema.get('required', [])
                        
                        for prop_name, prop_schema in schema.get('properties', {}).items():
                            # Use example if provided
                            if 'example' in prop_schema:
                                example[prop_name] = prop_schema['example']
                                continue
                            
                            prop_type = prop_schema.get('type', 'string')
                            if prop_type == 'string':
                                if prop_schema.get('format') == 'email':
                                    example[prop_name] = "user@example.com"
                                elif prop_schema.get('format') == 'date-time':
                                    example[prop_name] = "2024-12-31T23:59:59Z"
                                else:
                                    example[prop_name] = f"<{prop_name}>"
                            elif prop_type == 'integer':
                                example[prop_name] = prop_schema.get('default', 0)
                            elif prop_type == 'boolean':
                                example[prop_name] = prop_schema.get('default', False)
                            elif prop_type == 'array':
                                example[prop_name] = []
                            elif prop_type == 'object':
                                example[prop_name] = {}
                            
                            # Include required fields
                            if prop_name in required or prop_name in example:
                                pass  # Already added
                        
                        return json.dumps(example, indent=2)
                        example = {}
                        required = definition.get('required', [])
                        
                        for prop_name, prop_schema in definition.get('properties', {}).items():
                            # Use example if provided
                            if 'example' in prop_schema:
                                example[prop_name] = prop_schema['example']
                                continue
                            
                            prop_type = prop_schema.get('type', 'string')
                            if prop_type == 'string':
                                # Check for format
                                if prop_schema.get('format') == 'email':
                                    example[prop_name] = "user@example.com"
                                elif prop_schema.get('format') == 'date-time':
                                    example[prop_name] = "2024-12-31T23:59:59Z"
                                else:
                                    example[prop_name] = f"<{prop_name}>"
                            elif prop_type == 'integer':
                                example[prop_name] = prop_schema.get('default', 0)
                            elif prop_type == 'boolean':
                                example[prop_name] = prop_schema.get('default', False)
                            elif prop_type == 'array':
                                items = prop_schema.get('items', {})
                                if items.get('$ref'):
                                    # Reference to another definition
                                    item_def_name = items['$ref'].split('/')[-1]
                                    item_def = self.spec.get('definitions', {}).get(item_def_name, {})
                                    if 'properties' in item_def:
                                        item_example = {}
                                        for item_prop, item_prop_schema in item_def['properties'].items():
                                            if 'example' in item_prop_schema:
                                                item_example[item_prop] = item_prop_schema['example']
                                            else:
                                                item_example[item_prop] = f"<{item_prop}>"
                                        example[prop_name] = [item_example]
                                    else:
                                        example[prop_name] = []
                                else:
                                    example[prop_name] = []
                            elif prop_type == 'object':
                                example[prop_name] = {}
                            
                            # Only include required fields or fields with defaults
                            if prop_name not in required and prop_name not in example:
                                continue
                        
                        return json.dumps(example, indent=2)
        
        return None
    
    def _build_url(self, path: str, operation: Dict[str, Any]) -> str:
        """Build full URL with path parameters."""
        # In OpenAPI 2.0, paths are appended to basePath
        # basePath already includes leading /, paths also start with /
        # So we need to join them properly
        if path.startswith('/'):
            # Remove leading slash and append to base_url
            url = self.base_url.rstrip('/') + path
        else:
            url = urljoin(self.base_url.rstrip('/') + '/', path)
        
        # Replace path parameters with examples
        parameters = operation.get('parameters', [])
        for param in parameters:
            if param.get('in') == 'path':
                param_name = param.get('name', '')
                example = param.get('example', f"<{param_name}>")
                url = url.replace(f"{{{param_name}}}", str(example))
        
        # Add query parameters
        query_params = []
        for param in parameters:
            if param.get('in') == 'query':
                param_name = param.get('name', '')
                example = param.get('example', param.get('default', f"<{param_name}>"))
                query_params.append(f"{param_name}={example}")
        
        if query_params:
            url += "?" + "&".join(query_params)
        
        return url
    
    def extract_endpoints(self) -> List[Dict[str, Any]]:
        """Extract all endpoints from the OpenAPI spec."""
        endpoints = []
        paths = self.spec.get('paths', {})
        
        for path, path_item in paths.items():
            # Get common parameters for this path
            common_params = path_item.get('parameters', [])
            
            for method, operation in path_item.items():
                if method not in ['get', 'post', 'put', 'patch', 'delete', 'head', 'options']:
                    continue
                
                # Merge common parameters with operation parameters
                op_params = operation.get('parameters', [])
                all_params = common_params + op_params
                operation['parameters'] = all_params
                
                # Build URL
                url = self._build_url(path, operation)
                
                # Get headers
                headers = self._get_security_headers(operation)
                
                # Add Content-Type if there's a body
                request_body = self._get_request_body(operation)
                if request_body:
                    headers['Content-Type'] = 'application/json'
                
                # Get summary/description
                summary = operation.get('summary', '')
                description = operation.get('description', '')
                operation_id = operation.get('operationId', '')
                
                endpoints.append({
                    'method': method.upper(),
                    'url': url,
                    'headers': headers,
                    'body': request_body,
                    'summary': summary,
                    'description': description,
                    'operation_id': operation_id,
                    'path': path,
                    'tags': operation.get('tags', [])
                })
        
        return endpoints


def generate_http_file_from_openapi(yaml_file: str, output_file: str = None) -> str:
    """
    Generate HTTP request file from OpenAPI/Swagger YAML.
    
    Args:
        yaml_file: Path to OpenAPI YAML file
        output_file: Optional output file path
    
    Returns:
        Generated HTTP file content
    """
    parser = OpenAPIParser(yaml_file)
    endpoints = parser.extract_endpoints()
    
    # Group by tags
    from collections import defaultdict
    by_tag = defaultdict(list)
    for endpoint in endpoints:
        tags = endpoint.get('tags', [''])
        tag = tags[0] if tags else 'Other'
        by_tag[tag].append(endpoint)
    
    lines = []
    lines.append("### Generated from OpenAPI/Swagger specification")
    lines.append(f"### Base URL: {parser.base_url}")
    lines.append("")
    
    # Generate requests grouped by tag
    for tag, tag_endpoints in sorted(by_tag.items()):
        lines.append(f"### {tag}")
        lines.append("")
        
        for endpoint in tag_endpoints:
            # Add comment with summary
            if endpoint['summary']:
                lines.append(f"### {endpoint['summary']}")
            if endpoint['description']:
                # Take first line of description
                desc_line = endpoint['description'].split('\n')[0].strip()
                if desc_line and desc_line != endpoint['summary']:
                    lines.append(f"### {desc_line}")
            
            # Generate request
            request_lines = [f"{endpoint['method']} {endpoint['url']}"]
            
            # Add headers
            for key, value in endpoint['headers'].items():
                request_lines.append(f"{key}: {value}")
            
            # Add body
            if endpoint['body']:
                request_lines.append("")
                request_lines.append(endpoint['body'])
            
            lines.extend(request_lines)
            lines.append("")
            lines.append("")
    
    content = "\n".join(lines)
    
    if output_file:
        with open(output_file, 'w') as f:
            f.write(content)
    
    return content

