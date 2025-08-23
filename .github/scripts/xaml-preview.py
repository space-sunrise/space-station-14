#!/usr/bin/env python3
"""
XAML Preview Generator
Processes XAML files and generates formatted previews for PR comments.
Enhanced version with visual hierarchy and better analysis.
"""

import os
import sys
import xml.etree.ElementTree as ET
import argparse
from pathlib import Path
from typing import List, Dict, Optional, Any
from dataclasses import dataclass
import re


@dataclass
class ControlInfo:
    """Information about a UI control parsed from XAML."""
    name: str
    element_type: str
    attributes: Dict[str, str]
    children: List['ControlInfo']
    text_content: Optional[str] = None
    namespace: Optional[str] = None

    def has_layout_properties(self) -> bool:
        """Check if control has layout-related properties."""
        layout_props = {'Width', 'Height', 'MinWidth', 'MinHeight', 'MaxWidth', 'MaxHeight', 
                       'HorizontalAlignment', 'VerticalAlignment', 'Margin', 'Padding',
                       'HorizontalExpand', 'VerticalExpand'}
        return any(prop in self.attributes for prop in layout_props)

    def get_size_info(self) -> str:
        """Get size and layout information as a formatted string."""
        size_parts = []
        if 'Width' in self.attributes:
            size_parts.append(f"W:{self.attributes['Width']}")
        if 'Height' in self.attributes:
            size_parts.append(f"H:{self.attributes['Height']}")
        if 'MinWidth' in self.attributes:
            size_parts.append(f"MinW:{self.attributes['MinWidth']}")
        if 'MinHeight' in self.attributes:
            size_parts.append(f"MinH:{self.attributes['MinHeight']}")
        
        align_parts = []
        if 'HorizontalAlignment' in self.attributes:
            align_parts.append(f"HA:{self.attributes['HorizontalAlignment']}")
        if 'VerticalAlignment' in self.attributes:
            align_parts.append(f"VA:{self.attributes['VerticalAlignment']}")
        
        all_parts = size_parts + align_parts
        return " | ".join(all_parts) if all_parts else ""


def parse_xaml_structure(file_path: str) -> Dict[str, Any]:
    """Parse XAML file and extract detailed structural information."""
    try:
        tree = ET.parse(file_path)
        root = tree.getroot()
        
        # Extract basic information
        info = {
            'root_element': root.tag,
            'attributes': dict(root.attrib),
            'children_count': len(list(root)),
            'has_content': bool(root.text and root.text.strip()),
            'namespaces': {},
            'controls': [],
            'file_size': os.path.getsize(file_path),
            'line_count': 0,
            'structure': None,
            'layout_analysis': {},
            'ui_complexity': 'Simple'
        }
        
        # Count lines
        with open(file_path, 'r', encoding='utf-8') as f:
            info['line_count'] = len(f.readlines())
        
        # Extract namespaces
        for key, value in root.attrib.items():
            if key.startswith('xmlns'):
                namespace_name = key.split(':', 1)[1] if ':' in key else 'default'
                info['namespaces'][namespace_name] = value
        
        # Parse structure recursively
        info['structure'] = parse_control_recursive(root)
        
        # Analyze layout complexity
        info['layout_analysis'] = analyze_layout_complexity(info['structure'])
        
        # Determine UI complexity
        total_controls = count_controls(info['structure'])
        if total_controls > 20:
            info['ui_complexity'] = 'Complex'
        elif total_controls > 10:
            info['ui_complexity'] = 'Moderate'
        
        # Extract all unique control types
        extract_control_types(info['structure'], info['controls'])
        
        return info
        
    except ET.ParseError as e:
        return {
            'error': f'XML Parse Error: {str(e)}',
            'file_size': os.path.getsize(file_path) if os.path.exists(file_path) else 0,
            'line_count': 0
        }
    except Exception as e:
        return {
            'error': f'Error: {str(e)}',
            'file_size': os.path.getsize(file_path) if os.path.exists(file_path) else 0,
            'line_count': 0
        }


def parse_control_recursive(element: ET.Element) -> ControlInfo:
    """Recursively parse XML element into ControlInfo structure."""
    # Clean up element name
    name = element.tag.split('}')[-1] if '}' in element.tag else element.tag
    namespace = element.tag.split('}')[0][1:] if '}' in element.tag else None
    
    control = ControlInfo(
        name=name,
        element_type=name,
        attributes=dict(element.attrib),
        children=[],
        text_content=element.text.strip() if element.text and element.text.strip() else None,
        namespace=namespace
    )
    
    # Parse child elements
    for child in element:
        control.children.append(parse_control_recursive(child))
    
    return control


def extract_control_types(control: ControlInfo, types_list: List[str]):
    """Extract all unique control types from the structure."""
    if control.element_type not in types_list:
        types_list.append(control.element_type)
    
    for child in control.children:
        extract_control_types(child, types_list)


def count_controls(control: ControlInfo) -> int:
    """Count total number of controls in the structure."""
    return 1 + sum(count_controls(child) for child in control.children)


def analyze_layout_complexity(structure: ControlInfo) -> Dict[str, Any]:
    """Analyze the layout complexity and patterns."""
    analysis = {
        'total_controls': count_controls(structure),
        'max_depth': get_max_depth(structure),
        'container_types': [],
        'has_complex_layout': False,
        'layout_patterns': []
    }
    
    # Find container types
    find_containers(structure, analysis['container_types'])
    
    # Check for complex layout patterns
    if analysis['max_depth'] > 5:
        analysis['has_complex_layout'] = True
        analysis['layout_patterns'].append('Deep nesting detected')
    
    if len(analysis['container_types']) > 3:
        analysis['layout_patterns'].append('Multiple container types')
    
    return analysis


def get_max_depth(control: ControlInfo, current_depth: int = 0) -> int:
    """Get maximum nesting depth of the control structure."""
    if not control.children:
        return current_depth
    
    return max(get_max_depth(child, current_depth + 1) for child in control.children)


def find_containers(control: ControlInfo, container_list: List[str]):
    """Find all container control types."""
    container_types = {'BoxContainer', 'SplitContainer', 'ScrollContainer', 'GridContainer', 
                      'TabContainer', 'VBoxContainer', 'HBoxContainer', 'Control', 'Panel'}
    
    if control.element_type in container_types and control.element_type not in container_list:
        container_list.append(control.element_type)
    
    for child in control.children:
        find_containers(child, container_list)


def generate_structure_diagram(control: ControlInfo, indent: int = 0, max_depth: int = 6) -> str:
    """Generate a visual ASCII diagram of the UI structure."""
    if indent > max_depth:
        return "  " * indent + "... (truncated)\n"
    
    # Create the visual representation
    prefix = "  " * indent
    
    # Choose appropriate icon for control type
    icon = get_control_icon(control.element_type)
    
    # Build the line
    line = f"{prefix}{icon} {control.element_type}"
    
    # Add important attributes
    important_attrs = []
    if 'Name' in control.attributes:
        important_attrs.append(f"Name=\"{control.attributes['Name']}\"")
    if 'Text' in control.attributes:
        text = control.attributes['Text'][:20] + "..." if len(control.attributes['Text']) > 20 else control.attributes['Text']
        important_attrs.append(f"Text=\"{text}\"")
    
    # Add size information
    size_info = control.get_size_info()
    if size_info:
        important_attrs.append(f"[{size_info}]")
    
    if important_attrs:
        line += f" ({', '.join(important_attrs)})"
    
    line += "\n"
    
    # Add children
    result = line
    for child in control.children:
        result += generate_structure_diagram(child, indent + 1, max_depth)
    
    return result


def get_control_icon(control_type: str) -> str:
    """Get an appropriate icon/symbol for the control type."""
    icons = {
        'Window': '🪟',
        'FancyWindow': '🪟',
        'BoxContainer': '📦',
        'VBoxContainer': '📦',
        'HBoxContainer': '📦',
        'SplitContainer': '📂',
        'ScrollContainer': '📜',
        'GridContainer': '⊞',
        'TabContainer': '📑',
        'Button': '🔘',
        'Label': '🏷️',
        'TextEdit': '📝',
        'LineEdit': '📝',
        'RichTextLabel': '📄',
        'Panel': '▢',
        'Control': '▢',
        'Separator': '━',
        'VSeparator': '│',
        'HSeparator': '─',
        'ProgressBar': '█',
        'CheckBox': '☐',
        'OptionButton': '◯',
        'ItemList': '📋',
        'Tree': '🌳',
        'TextureRect': '🖼️',
        'NinePatchRect': '🖼️',
    }
    return icons.get(control_type, '▢')


def format_file_info(file_path: str, info: Dict, change_type: str) -> str:
    """Format file information for display with enhanced visual preview."""
    icon_map = {
        'added': '✨',
        'modified': '📝',
        'removed': '🗑️'
    }
    
    icon = icon_map.get(change_type, '📄')
    relative_path = file_path
    
    if 'error' in info:
        return f"## {icon} {change_type.title()}: `{relative_path}`\n\n⚠️ **Error processing file:** {info['error']}\n\n"
    
    # File size formatting
    size = info['file_size']
    if size > 1024 * 1024:
        size_str = f"{size / (1024 * 1024):.1f} MB"
    elif size > 1024:
        size_str = f"{size / 1024:.1f} KB"
    else:
        size_str = f"{size} bytes"
    
    # Build summary with enhanced information
    summary_parts = []
    
    # Clean up root element name for better readability
    root_element = info.get('root_element', 'Unknown')
    if '}' in root_element:
        root_element = root_element.split('}')[-1]
    summary_parts.append(f"**Root Element:** `{root_element}`")
    
    # Enhanced control information
    layout_analysis = info.get('layout_analysis', {})
    total_controls = layout_analysis.get('total_controls', len(info.get('controls', [])))
    summary_parts.append(f"**UI Complexity:** {info.get('ui_complexity', 'Unknown')} ({total_controls} controls)")
    
    # Layout depth information
    max_depth = layout_analysis.get('max_depth', 0)
    if max_depth > 0:
        summary_parts.append(f"**Nesting Depth:** {max_depth} levels")
    
    summary_parts.append(f"**File Size:** {size_str} ({info['line_count']} lines)")
    
    # Container information
    container_types = layout_analysis.get('container_types', [])
    if container_types:
        container_list = ', '.join(f"`{ctrl}`" for ctrl in container_types[:5])
        if len(container_types) > 5:
            container_list += f" and {len(container_types) - 5} more"
        summary_parts.append(f"**Layout Containers:** {container_list}")
    
    # Control types used
    if info.get('controls'):
        controls_list = ', '.join(f"`{ctrl}`" for ctrl in info['controls'][:8])
        if len(info['controls']) > 8:
            controls_list += f" and {len(info['controls']) - 8} more"
        summary_parts.append(f"**UI Controls:** {controls_list}")
    
    # Layout patterns
    layout_patterns = layout_analysis.get('layout_patterns', [])
    if layout_patterns:
        summary_parts.append(f"**Layout Notes:** {', '.join(layout_patterns)}")
    
    # Show interesting namespaces
    namespaces = info.get('namespaces', {})
    if namespaces and len(namespaces) > 1:
        ns_list = []
        for ns, uri in namespaces.items():
            if ns != 'default' and 'spacestation14.io' not in uri:
                ns_list.append(f"`{ns}`")
        if ns_list:
            summary_parts.append(f"**Custom Namespaces:** {', '.join(ns_list[:3])}")
    
    summary = '\n'.join(f"- {part}" for part in summary_parts)
    
    # Check if mockup image exists
    mockup_section = ""
    if change_type in ['added', 'modified']:
        mockup_filename = os.path.basename(file_path).replace('.xaml', '_mockup.png')
        mockup_path = f"xaml-previews/{mockup_filename}"
        if os.path.exists(mockup_path):
            # Get the workflow run URL for artifact download
            run_id = os.environ.get('GITHUB_RUN_ID', 'unknown')
            repo = os.environ.get('GITHUB_REPOSITORY', 'space-sunrise/sunrise-station')
            
            mockup_section = f"""
### 🖼️ Visual Mockup

> **Note:** This is a simplified visual representation showing the basic layout structure. 
> The actual UI may look different with proper styling and content.

📎 **Download mockup image:** [`{mockup_filename}`](https://github.com/{repo}/actions/runs/{run_id}/artifacts) (Look for "xaml-previews" artifact)

*Mockup generated automatically - shows basic UI layout and structure*
"""
    
    # Generate structure diagram
    structure_diagram = ""
    if 'structure' in info and info['structure']:
        structure_diagram = f"""
### 🎨 UI Structure Preview

```
{generate_structure_diagram(info['structure']).rstrip()}
```
"""
    
    # Read file content for preview (reduced size)
    content_preview = ""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            preview_lines = lines[:20]  # Show first 20 lines
            content_preview = ''.join(preview_lines)
            if len(lines) > 20:
                content_preview += f"\n... (showing first 20 of {len(lines)} lines)"
    except Exception as e:
        content_preview = f"Error reading file: {str(e)}"
    
    return f"""## {icon} {change_type.title()}: `{relative_path}`

{summary}
{mockup_section}
{structure_diagram}
<details><summary>📋 Click to view XAML source</summary>

```xml
{content_preview}
```

</details>

"""


def process_xaml_files(modified_files: List[str], added_files: List[str], removed_files: List[str]) -> str:
    """Process all XAML files and generate enhanced preview content."""
    
    # Filter to only include XAML files
    modified_xaml = [f for f in modified_files if f.endswith('.xaml')]
    added_xaml = [f for f in added_files if f.endswith('.xaml')]
    removed_xaml = [f for f in removed_files if f.endswith('.xaml')]
    
    preview_content = "# 🎨 XAML Preview Bot\n\n"
    
    total_files = len(modified_xaml) + len(added_xaml) + len(removed_xaml)
    
    if total_files == 0:
        return preview_content + "No XAML files were changed in this PR.\n"
    
    # Enhanced summary
    summary_parts = []
    if added_xaml:
        summary_parts.append(f"**{len(added_xaml)} added**")
    if modified_xaml:
        summary_parts.append(f"**{len(modified_xaml)} modified**")
    if removed_xaml:
        summary_parts.append(f"**{len(removed_xaml)} removed**")
    
    preview_content += f"Found **{total_files}** XAML file(s) changed: {', '.join(summary_parts)}\n\n"
    
    # Add quick navigation if there are many files
    if total_files > 3:
        preview_content += "### Quick Navigation\n"
        all_files = [(f, 'added') for f in added_xaml] + [(f, 'modified') for f in modified_xaml] + [(f, 'removed') for f in removed_xaml]
        for file_path, change_type in all_files:
            icon = {'added': '✨', 'modified': '📝', 'removed': '🗑️'}[change_type]
            file_name = os.path.basename(file_path)
            preview_content += f"- {icon} [{file_name}](#{change_type}-{file_name.lower().replace('.', '').replace(' ', '-')})\n"
        preview_content += "\n---\n\n"
    
    # Process added files
    for file_path in added_xaml:
        if os.path.exists(file_path):
            info = parse_xaml_structure(file_path)
            preview_content += format_file_info(file_path, info, 'added')
        else:
            preview_content += f"## ✨ Added: `{file_path}`\n\n⚠️ **File not found in current checkout**\n\n"
    
    # Process modified files
    for file_path in modified_xaml:
        if os.path.exists(file_path):
            info = parse_xaml_structure(file_path)
            preview_content += format_file_info(file_path, info, 'modified')
        else:
            preview_content += f"## 📝 Modified: `{file_path}`\n\n⚠️ **File not found in current checkout**\n\n"
    
    # Process removed files
    for file_path in removed_xaml:
        preview_content += f"## 🗑️ Removed: `{file_path}`\n\n*This XAML file was deleted from the codebase.*\n\n"
    
    # Add enhanced footer
    preview_content += "\n---\n\n"
    preview_content += "### 🤖 About This Preview\n\n"
    preview_content += "This enhanced preview shows the UI structure and layout analysis of your XAML changes. "
    preview_content += "The structure diagram uses icons to represent different control types and shows the hierarchy "
    preview_content += "to help you understand the layout without building locally.\n\n"
    
    # Check if any mockups were generated
    mockup_count = 0
    if os.path.exists('xaml-previews'):
        mockup_count = len([f for f in os.listdir('xaml-previews') if f.endswith('.png')])
    
    if mockup_count > 0:
        run_id = os.environ.get('GITHUB_RUN_ID', 'unknown')
        repo = os.environ.get('GITHUB_REPOSITORY', 'space-sunrise/sunrise-station')
        preview_content += f"**📎 {mockup_count} visual mockup(s) generated** - "
        preview_content += f"Download from [workflow artifacts](https://github.com/{repo}/actions/runs/{run_id}/artifacts) "
        preview_content += "(look for 'xaml-previews' artifact)\n\n"
    
    preview_content += "*Preview automatically generated by XAML Preview Bot*"
    
    return preview_content


def main():
    parser = argparse.ArgumentParser(description='Generate XAML file previews')
    parser.add_argument('--modified', default='', help='Space-separated list of modified files')
    parser.add_argument('--added', default='', help='Space-separated list of added files')
    parser.add_argument('--removed', default='', help='Space-separated list of removed files')
    
    args = parser.parse_args()
    
    # Parse file lists
    modified_files = [f.strip() for f in args.modified.split() if f.strip()]
    added_files = [f.strip() for f in args.added.split() if f.strip()]
    removed_files = [f.strip() for f in args.removed.split() if f.strip()]
    
    # Generate preview content
    preview_content = process_xaml_files(modified_files, added_files, removed_files)
    
    # Output for GitHub Actions
    print("PREVIEW_CONTENT<<EOF")
    print(preview_content)
    print("EOF")


if __name__ == '__main__':
    main()