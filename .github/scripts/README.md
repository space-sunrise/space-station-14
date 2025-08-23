# GitHub Workflow Scripts

This directory contains scripts used by GitHub Actions workflows.

## XAML Preview System

### xaml-preview.py

Enhanced Python script that processes XAML files and generates comprehensive previews for pull request comments.

#### Features

- **Enhanced XAML Analysis**: Deep parsing of XAML structure with hierarchy analysis
- **Layout Complexity Assessment**: Analyzes nesting depth, control counts, and layout patterns
- **Visual Structure Diagrams**: ASCII tree diagrams with emoji icons for different control types
- **UI Control Inventory**: Detailed categorization of containers and controls
- **File Metadata**: Shows file size, line count, namespaces, and complexity metrics
- **Change Detection**: Handles added, modified, and removed files with appropriate formatting
- **Error Handling**: Gracefully handles malformed XAML or missing files

### xaml_mockup_generator.py

Visual mockup generator that creates basic layout preview images from XAML files.

#### Features

- **Visual Layout Generation**: Creates PNG mockups showing UI structure
- **Control-Specific Styling**: Different visual styles for buttons, labels, containers
- **Hierarchical Rendering**: Shows nested container relationships
- **Size Intelligence**: Attempts to respect explicit sizing attributes
- **Fallback Support**: Works independently if main preview script fails

### Integration

These scripts work together in the `.github/workflows/xaml-preview.yml` workflow:

1. **Trigger**: Activates on pull requests that modify `.xaml` files
2. **Analysis**: Enhanced XAML structure parsing and complexity analysis
3. **Image Generation**: Creates visual mockups for added/modified files
4. **Preview Generation**: Produces comprehensive markdown previews
5. **Comment Management**: Posts/updates PR comments with complete previews
6. **Artifact Upload**: Makes generated images available as downloadable artifacts

### Usage

```bash
# Enhanced text preview
python3 xaml-preview.py --modified "file1.xaml file2.xaml" --added "file3.xaml" --removed "file4.xaml"

# Visual mockup generation
python3 xaml_mockup_generator.py input.xaml output.png
```

### Enhanced Output Format

The enhanced preview includes:

**For each XAML file:**
- 📊 **Analysis Summary**: Root element, complexity level, nesting depth, file size
- 📎 **Visual Mockup**: Download link to generated PNG layout preview
- 🎨 **Structure Diagram**: ASCII tree with emoji icons showing UI hierarchy
- 📋 **Source Code**: Collapsible section with syntax-highlighted XAML content
- 🔍 **Detailed Metrics**: Container types, control inventory, layout patterns

**Overall Summary:**
- Quick navigation for multiple files
- Change type breakdown (added/modified/removed counts)
- Artifact download information
- Enhanced footer with feature explanation

### Control Icon Legend

The structure diagrams use intuitive emoji icons:
- 🪟 Windows (Window, FancyWindow)
- 📦 Containers (BoxContainer, VBoxContainer, HBoxContainer)  
- 📂 Split Containers
- 📜 Scroll Containers
- 🔘 Buttons
- 🏷️ Labels
- 📝 Text Inputs (TextEdit, LineEdit)
- 📄 Rich Text Labels
- ▢ Generic Controls

This enhanced system provides developers with immediate visual feedback on XAML changes, including both structural analysis and basic visual previews, without requiring local builds.