# Документация для модуля 'Audio'

Этот модуль предоставляет функции для воспроизведения звуков из файлов на диске и по URL, а также для управления громкостью звука.

## Доступные методы

| Функция                        | Описание                                                                                                                  |
|--------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| `PlaySound(string uri)`        | Асинхронно воспроизводит звук по указанному URL-адресу или локальному файлу полученному из `uri`.                         |
| `SetVolume(float volume)`      | Устанавливает громкость звука. Значение `volume` должно быть в диапазоне от 0.0 (тишина) до 1.0 (максимальная громкость). |
| `GetVolume()`                  | Возвращает текущую громкость звука. Значение находится в диапазоне от 0.0 до 1.0.                                         |
| `IncreaseVolume(float volume)` | Увеличивает громкость звука на указанное значение `volume`. Громкость не может превышать 1.0.                             |
| `DecreaseVolume(float volume)` | Уменьшает громкость звука на указанное значение `volume`. Громкость не может быть меньше 0.0.                             |
| `SkipSound()`                  | Прерывает текущее воспроизведение звука.                                                                                  |

Пример использования методов в файле конфигурации

```lua
-- Воспроизведение звука из файла на диске
Audio.PlaySound("C:/Sounds/mysound.mp3")

-- Воспроизведение звука по URL
Audio.PlaySound("https://example.com/audio/sound.ogg")

-- Установка громкости
Audio.SetVolume(0.5) -- Устанавливаем громкость на 50%

-- Получение текущей громкости
local currentVolume = Audio.GetVolume()
print("Текущая громкость: " .. currentVolume)

-- Увеличение громкости на 10%
Audio.IncreaseVolume(0.1)

-- Уменьшение громкости на 20%
Audio.DecreaseVolume(0.2)

-- Прерывание воспроизведения звука
Audio.SkipSound()
```
