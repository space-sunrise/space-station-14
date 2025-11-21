#!/usr/bin/env python3
"""
Скрипт для поиска неиспользуемых текстур в Resources/Textures.

Текстуры считаются используемыми если они:
1. Упоминаются в YAML файлах (прототипах)
2. Упоминаются в C# коде (hardcoded пути)
3. Являются частью RSI (meta.json или PNG в .rsi директории)

Использование:
    python3 Tools/find_unused_textures.py
    python3 Tools/find_unused_textures.py --output unused_textures.json
"""

import os
import re
import json
import sys
from pathlib import Path
from typing import Set, Dict, List
from collections import defaultdict

# Корневая директория проекта
PROJECT_ROOT = Path(__file__).parent.parent.absolute()
TEXTURES_DIR = PROJECT_ROOT / "Resources" / "Textures"
PROTOTYPES_DIR = PROJECT_ROOT / "Resources" / "Prototypes"
CONTENT_DIRS = [
    PROJECT_ROOT / "Content.Client",
    PROJECT_ROOT / "Content.Server",
    PROJECT_ROOT / "Content.Shared",
]


def find_yaml_sprite_references() -> Set[str]:
    """Находит все ссылки на спрайты в YAML файлах."""
    references = set()
    
    # Паттерны для поиска спрайтов в YAML
    # Поддерживаем как простые пути, так и пути в кавычках
    patterns = [
        re.compile(r'sprite:\s*(["\']?)([^\s#\n"\']+\.rsi)\1', re.IGNORECASE),
        re.compile(r'sprite:\s*(["\']?)([^\s#\n"\']+)\1', re.IGNORECASE),
        re.compile(r'texture:\s*(["\']?)([^\s#\n"\']+)\1', re.IGNORECASE),
        re.compile(r'path:\s*(["\']?)([^\s#\n"\']+\.(?:png|rsi))\1', re.IGNORECASE),
    ]
    
    # Также ищем в Maps (они могут содержать спрайты)
    search_dirs = [PROTOTYPES_DIR]
    maps_dir = PROJECT_ROOT / "Resources" / "Maps"
    if maps_dir.exists():
        search_dirs.append(maps_dir)
    
    for search_dir in search_dirs:
        if not search_dir.exists():
            continue
            
        for yaml_file in search_dir.rglob("*.yml"):
            try:
                with open(yaml_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                for pattern in patterns:
                    matches = pattern.findall(content)
                    for match in matches:
                        # Для паттернов с группами захвата берём последнюю группу (сам путь)
                        path = match[-1] if isinstance(match, tuple) else match
                        # Очищаем путь от кавычек и лишних символов
                        path = path.strip('"\'')
                        if path and not path.startswith('#'):
                            references.add(path)
            except Exception as e:
                print(f"Предупреждение: не удалось прочитать {yaml_file}: {e}", file=sys.stderr)
    
    return references


def find_csharp_texture_references() -> Set[str]:
    """Находит все ссылки на текстуры в C# коде."""
    references = set()
    
    # Паттерны для поиска текстур в C# коде
    # Поддерживаем как абсолютные пути (/Textures/), так и относительные
    patterns = [
        re.compile(r'["\'](?:/Textures/)?([^"\']*?\.(?:rsi|png|svg|jpg|jpeg))["\']', re.IGNORECASE),
        re.compile(r'new\s+ResPath\s*\(\s*["\']([^"\']+)["\']', re.IGNORECASE),
        re.compile(r'SpriteSpecifier\.[^(]+\([^"\']*["\']([^"\']+)["\']', re.IGNORECASE),
    ]
    
    for content_dir in CONTENT_DIRS:
        if not content_dir.exists():
            continue
            
        for cs_file in content_dir.rglob("*.cs"):
            try:
                with open(cs_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                for pattern in patterns:
                    matches = pattern.findall(content)
                    for match in matches:
                        # Убираем начальный /Textures/ если есть
                        if match.startswith('/Textures/'):
                            match = match[10:]
                        references.add(match)
            except Exception as e:
                print(f"Предупреждение: не удалось прочитать {cs_file}: {e}", file=sys.stderr)
    
    return references


def normalize_path(path: str) -> str:
    """Нормализует путь к текстуре."""
    # Убираем начальные слэши и Textures/
    path = path.lstrip('/')
    if path.startswith('Textures/'):
        path = path[9:]
    return path


def get_all_texture_files() -> Dict[str, List[Path]]:
    """Получает все файлы текстур, сгруппированные по RSI директориям."""
    texture_files = defaultdict(list)
    
    if not TEXTURES_DIR.exists():
        return texture_files
    
    # Находим все RSI директории
    for rsi_dir in TEXTURES_DIR.rglob("*.rsi"):
        if rsi_dir.is_dir():
            rsi_path = str(rsi_dir.relative_to(TEXTURES_DIR))
            
            # Добавляем meta.json
            meta_json = rsi_dir / "meta.json"
            if meta_json.exists():
                texture_files[rsi_path].append(meta_json)
            
            # Добавляем все PNG файлы в этой RSI
            for png_file in rsi_dir.glob("*.png"):
                texture_files[rsi_path].append(png_file)
    
    # Находим одиночные PNG/SVG файлы (не в RSI директориях)
    for texture_file in TEXTURES_DIR.rglob("*"):
        if texture_file.is_file() and texture_file.suffix.lower() in ['.png', '.svg', '.jpg', '.jpeg']:
            # Проверяем, что файл не в RSI директории
            if '.rsi' not in str(texture_file.relative_to(TEXTURES_DIR)):
                rel_path = str(texture_file.relative_to(TEXTURES_DIR))
                texture_files[rel_path].append(texture_file)
    
    return texture_files


def is_texture_referenced(texture_path: str, references: Set[str]) -> bool:
    """Проверяет, упоминается ли текстура в коде или прототипах."""
    # Нормализуем путь
    normalized = normalize_path(texture_path)
    
    # Проверяем прямое совпадение
    if normalized in references:
        return True
    
    # Для RSI проверяем совпадение пути без учета регистра
    for ref in references:
        ref_normalized = normalize_path(ref)
        if ref_normalized.lower() == normalized.lower():
            return True
        
        # Проверяем частичные совпадения для RSI
        if normalized.endswith('.rsi'):
            if ref_normalized.endswith(normalized) or normalized.endswith(ref_normalized):
                return True
    
    return False


def calculate_directory_size(file_paths: List[Path]) -> int:
    """Вычисляет общий размер файлов в байтах."""
    total_size = 0
    for file_path in file_paths:
        try:
            total_size += file_path.stat().st_size
        except Exception:
            pass
    return total_size


def format_size(size_bytes: int) -> str:
    """Форматирует размер в человекочитаемый вид."""
    for unit in ['B', 'KB', 'MB', 'GB']:
        if size_bytes < 1024.0:
            return f"{size_bytes:.2f} {unit}"
        size_bytes /= 1024.0
    return f"{size_bytes:.2f} TB"


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='Поиск неиспользуемых текстур')
    parser.add_argument('--output', '-o', 
                       default='Resources/.unused_textures.json',
                       help='Путь к выходному JSON файлу (по умолчанию: Resources/.unused_textures.json)')
    parser.add_argument('--verbose', '-v', action='store_true', help='Подробный вывод')
    args = parser.parse_args()
    
    print("Сканирование ссылок на спрайты в YAML файлах...")
    yaml_refs = find_yaml_sprite_references()
    print(f"Найдено {len(yaml_refs)} ссылок в YAML")
    
    print("\nСканирование ссылок на текстуры в C# коде...")
    csharp_refs = find_csharp_texture_references()
    print(f"Найдено {len(csharp_refs)} ссылок в C#")
    
    # Объединяем все ссылки
    all_refs = yaml_refs | csharp_refs
    
    if args.verbose:
        print("\nПримеры найденных ссылок:")
        for ref in list(all_refs)[:10]:
            print(f"  - {ref}")
    
    print("\nСканирование файлов текстур...")
    texture_files = get_all_texture_files()
    print(f"Найдено {len(texture_files)} текстурных групп")
    
    # Определяем неиспользуемые текстуры
    unused_textures = {}
    used_textures = {}
    
    for texture_path, files in texture_files.items():
        if is_texture_referenced(texture_path, all_refs):
            used_textures[texture_path] = files
        else:
            unused_textures[texture_path] = files
    
    # Вычисляем статистику
    total_unused_size = sum(calculate_directory_size(files) for files in unused_textures.values())
    total_used_size = sum(calculate_directory_size(files) for files in used_textures.values())
    total_size = total_unused_size + total_used_size
    
    # Выводим результаты
    print("\n" + "="*80)
    print("СТАТИСТИКА НЕИСПОЛЬЗУЕМЫХ ТЕКСТУР")
    print("="*80)
    print(f"Всего текстурных групп: {len(texture_files)}")
    print(f"Используемых: {len(used_textures)}")
    print(f"Неиспользуемых: {len(unused_textures)}")
    print(f"\nРазмер используемых текстур: {format_size(total_used_size)}")
    print(f"Размер неиспользуемых текстур: {format_size(total_unused_size)}")
    print(f"Общий размер текстур: {format_size(total_size)}")
    
    if total_size > 0:
        percentage = (total_unused_size / total_size) * 100
        print(f"\nПроцент неиспользуемых текстур: {percentage:.2f}%")
    
    if unused_textures:
        print("\n" + "="*80)
        print("СПИСОК НЕИСПОЛЬЗУЕМЫХ ТЕКСТУР")
        print("="*80)
        
        # Сортируем по размеру (от большего к меньшему)
        sorted_unused = sorted(
            unused_textures.items(),
            key=lambda x: calculate_directory_size(x[1]),
            reverse=True
        )
        
        for texture_path, files in sorted_unused[:50]:  # Показываем топ-50
            size = calculate_directory_size(files)
            file_count = len(files)
            print(f"{texture_path:70s} | {format_size(size):>10s} | {file_count:>3d} файлов")
        
        if len(sorted_unused) > 50:
            remaining = len(sorted_unused) - 50
            remaining_size = sum(calculate_directory_size(files) for _, files in sorted_unused[50:])
            print(f"\n... и еще {remaining} текстур ({format_size(remaining_size)})")
    
    # Сохраняем в JSON
    output_data = {
            'summary': {
                'total_texture_groups': len(texture_files),
                'used_texture_groups': len(used_textures),
                'unused_texture_groups': len(unused_textures),
                'total_size_bytes': total_size,
                'used_size_bytes': total_used_size,
                'unused_size_bytes': total_unused_size,
                'unused_percentage': (total_unused_size / total_size * 100) if total_size > 0 else 0,
            },
            'unused_textures': [
                {
                    'path': path,
                    'size_bytes': calculate_directory_size(files),
                    'file_count': len(files),
                    'files': [str(f.relative_to(TEXTURES_DIR)) for f in files]
                }
                for path, files in sorted(
                    unused_textures.items(),
                    key=lambda x: calculate_directory_size(x[1]),
                    reverse=True
                )
            ]
        }
    
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, indent=2, ensure_ascii=False)
    
    print(f"\n\nРезультаты сохранены в {args.output}")


if __name__ == '__main__':
    main()
