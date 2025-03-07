import os
from PIL import Image

def convert_to_grayscale_with_alpha(folder="."):
  """
  Конвертирует все изображения в указанной папке (по умолчанию, текущая папка)
  в черно-белые, сохраняя прозрачность (альфа-канал).

  Args:
    folder: Путь к папке, содержащей изображения.
  """
  for filename in os.listdir(folder):
    if filename.endswith((".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff")):
      try:
        filepath = os.path.join(folder, filename)
        img = Image.open(filepath)

        # Конвертируем в RGBA, чтобы убедиться, что альфа-канал присутствует.
        img = img.convert("RGBA")

        # Создаем черно-белое изображение с альфа-каналом
        img_gray = img.convert("LA")  # "LA" означает Grayscale с альфа-каналом

        # Если исходное изображение было без прозрачности, мы можем убрать альфа канал, чтобы не было артефактов
        if img.mode != "RGBA":
            img_gray = img_gray.convert("L")


        img_gray.save(filepath)  # Заменяем исходный файл черно-белым

        print(f"Успешно конвертировано: {filename}")
      except Exception as e:
        print(f"Ошибка при конвертации {filename}: {e}")

if __name__ == "__main__":
  convert_to_grayscale_with_alpha()
  print("Конвертация завершена.")
