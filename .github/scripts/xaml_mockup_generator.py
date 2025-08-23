#!/usr/bin/env python3
"""
Simple XAML visual mockup generator using Pillow.
Creates basic visual representations of XAML layouts.
"""

from PIL import Image, ImageDraw, ImageFont
from typing import Dict, List, Tuple, Optional
import os
import sys

# Add the script directory to path to import xaml-preview
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
try:
    from xaml_preview import ControlInfo, parse_xaml_structure
except ImportError:
    # If we can't import, we'll redefine the needed parts
    import xml.etree.ElementTree as ET
    from dataclasses import dataclass
    
    @dataclass
    class ControlInfo:
        name: str
        element_type: str
        attributes: Dict[str, str]
        children: List['ControlInfo']
        text_content: Optional[str] = None
        namespace: Optional[str] = None
    
    def parse_xaml_structure(file_path: str) -> Dict:
        # Simplified version for standalone use
        try:
            tree = ET.parse(file_path)
            root = tree.getroot()
            
            def parse_control_recursive(element: ET.Element) -> ControlInfo:
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
                
                for child in element:
                    control.children.append(parse_control_recursive(child))
                
                return control
            
            return {'structure': parse_control_recursive(root)}
        except Exception as e:
            return {'error': str(e)}


class XamlMockupGenerator:
    def __init__(self, width: int = 800, height: int = 600):
        self.width = width
        self.height = height
        self.colors = {
            'background': '#2e3440',
            'panel': '#3b4252',
            'button': '#5e81ac',
            'text': '#eceff4',
            'accent': '#88c0d0',
            'border': '#4c566a',
            'highlight': '#8fbcbb'
        }
        
    def generate_mockup(self, xaml_file: str, output_file: str) -> bool:
        """Generate a visual mockup of the XAML file."""
        try:
            # Parse the XAML structure
            info = parse_xaml_structure(xaml_file)
            if 'error' in info:
                print(f"Error parsing XAML: {info['error']}")
                return False
            
            structure = info.get('structure')
            if not structure:
                print("No structure found in XAML")
                return False
            
            # Create image
            image = Image.new('RGB', (self.width, self.height), self.colors['background'])
            draw = ImageDraw.Draw(image)
            
            # Try to load a font
            try:
                font = ImageFont.truetype("DejaVuSans.ttf", 12)
                title_font = ImageFont.truetype("DejaVuSans-Bold.ttf", 16)
            except:
                try:
                    font = ImageFont.truetype("arial.ttf", 12)
                    title_font = ImageFont.truetype("arialbd.ttf", 16)
                except:
                    font = ImageFont.load_default()
                    title_font = ImageFont.load_default()
            
            # Draw title
            title = f"XAML Mockup: {os.path.basename(xaml_file)}"
            draw.text((10, 10), title, fill=self.colors['text'], font=title_font)
            
            # Draw the UI structure
            self._draw_control(draw, structure, 10, 40, self.width - 20, self.height - 50, font)
            
            # Save the image
            image.save(output_file, 'PNG')
            return True
            
        except Exception as e:
            print(f"Error generating mockup: {e}")
            return False
    
    def _draw_control(self, draw: ImageDraw.ImageDraw, control: ControlInfo, 
                     x: int, y: int, width: int, height: int, font: ImageFont.ImageFont,
                     level: int = 0) -> int:
        """Draw a control and its children, returns the Y position after drawing."""
        if level > 10:  # Prevent infinite recursion
            return y
        
        # Determine control type and style
        control_style = self._get_control_style(control.element_type)
        
        # Calculate dimensions
        control_height = control_style['height']
        padding = max(2, 8 - level)
        
        # Draw the control background
        if control_style['draw_background']:
            draw.rectangle([x, y, x + width, y + control_height], 
                         fill=control_style['background'], 
                         outline=control_style['border'])
        
        # Draw control label
        label_text = self._get_control_label(control)
        if label_text:
            text_color = control_style.get('text_color', self.colors['text'])
            # Limit text length to fit
            max_chars = max(10, (width - 20) // 8)
            if len(label_text) > max_chars:
                label_text = label_text[:max_chars - 3] + "..."
            draw.text((x + padding, y + padding), label_text, fill=text_color, font=font)
        
        # Draw children for container controls
        current_y = y + control_height + padding
        available_height = height - (current_y - y)
        
        if control.children and available_height > 20:
            child_height = max(20, available_height // max(1, len(control.children)))
            
            for i, child in enumerate(control.children):
                if current_y >= y + height - 10:  # Stop if we're out of space
                    break
                    
                child_y = current_y
                remaining_height = y + height - child_y
                child_actual_height = min(child_height, remaining_height)
                
                if child_actual_height > 10:
                    current_y = self._draw_control(draw, child, 
                                                 x + padding * 2, child_y,
                                                 width - padding * 4, child_actual_height,
                                                 font, level + 1)
                    current_y += padding
        
        return max(current_y, y + control_height)
    
    def _get_control_style(self, control_type: str) -> Dict:
        """Get visual style for a control type."""
        styles = {
            'Window': {
                'height': 25,
                'background': self.colors['panel'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'FancyWindow': {
                'height': 25,
                'background': self.colors['panel'],
                'border': self.colors['accent'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'Button': {
                'height': 20,
                'background': self.colors['button'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'Label': {
                'height': 15,
                'background': None,
                'border': None,
                'draw_background': False,
                'text_color': self.colors['text']
            },
            'TextEdit': {
                'height': 18,
                'background': '#4c566a',
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'LineEdit': {
                'height': 18,
                'background': '#4c566a',
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'BoxContainer': {
                'height': 15,
                'background': None,
                'border': '#434c5e',
                'draw_background': False,
                'text_color': self.colors['highlight']
            },
            'ScrollContainer': {
                'height': 15,
                'background': '#3b4252',
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['highlight']
            }
        }
        
        # Default style
        default_style = {
            'height': 15,
            'background': None,
            'border': '#434c5e',
            'draw_background': False,
            'text_color': self.colors['text']
        }
        
        return styles.get(control_type, default_style)
    
    def _get_control_label(self, control: ControlInfo) -> str:
        """Get display label for a control."""
        # Priority: Name > Text > Type
        if 'Name' in control.attributes:
            return f"{control.element_type}: {control.attributes['Name']}"
        elif 'Text' in control.attributes:
            text = control.attributes['Text']
            # Clean up localization keys
            if text.startswith('{Loc'):
                text = text.replace('{Loc ', '').replace("'", '').replace('}', '')
            return f"{control.element_type}: {text}"
        else:
            return control.element_type


def main():
    if len(sys.argv) < 2:
        print("Usage: python3 xaml_mockup_generator.py <xaml-file> [output-file]")
        return
    
    xaml_file = sys.argv[1]
    output_file = sys.argv[2] if len(sys.argv) > 2 else xaml_file.replace('.xaml', '_mockup.png')
    
    if not os.path.exists(xaml_file):
        print(f"XAML file not found: {xaml_file}")
        return
    
    generator = XamlMockupGenerator()
    if generator.generate_mockup(xaml_file, output_file):
        print(f"Mockup generated: {output_file}")
    else:
        print("Failed to generate mockup")


if __name__ == '__main__':
    main()