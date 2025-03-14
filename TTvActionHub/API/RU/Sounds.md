## Документация для модуля Sounds в `TTvActionHub.LuaTools.Audio`

Этот модуль предоставляет функции для воспроизведения звуков из файлов на диске и по URL, а также для управления громкостью звука.

### Функции

| Функция                               | Описание                                                                                                                  |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `PlaySoundFromDiscAsync(string path)` | Асинхронно воспроизводит звук из файла, расположенного по указанному пути `path` на диске.                                |
| `PlaySoundFromUrlAsync(string url)`   | Асинхронно воспроизводит звук по указанному URL-адресу `url`.                                                             |
| `SetVolume(float volume)`             | Устанавливает громкость звука. Значение `volume` должно быть в диапазоне от 0.0 (тишина) до 1.0 (максимальная громкость). |
| `GetVolume()`                         | Возвращает текущую громкость звука. Значение находится в диапазоне от 0.0 до 1.0.                                         |
| `IncreeseVolume(float volume)`        | Увеличивает громкость звука на указанное значение `volume`. Громкость не может превышать 1.0.                             |
| `DecreeseVolume(float volume)`        | Уменьшает громкость звука на указанное значение `volume`. Громкость не может быть меньше 0.0.                             |
| `SkipSound()`                         | Прерывает текущее воспроизведение звука.                                                                                  |

**Примечания:**

- Все функции, заканчивающиеся на `Async`, выполняются асинхронно, не блокируя основной поток программы.
- Перед использованием любой из этих функций, убедитесь, что сервис `audio` был инициализирован. Если `audio` равен `null`, будет выброшено исключение.
- Поддерживаемые форматы звуковых файлов зависят от используемой библиотеки NAudio. Обычно это WAV, MP3, Ogg Vorbis и другие.

### Пример использования в `config.lua`

```lua
local Sounds = import('TTvActionHub', 'TTvActionHub.LuaTools.Audio').Sounds

-- Воспроизведение звука из файла на диске
Sounds.PlaySoundFromDiscAsync("C:/Sounds/mysound.mp3")

-- Воспроизведение звука по URL
Sounds.PlaySoundFromUrlAsync("https://example.com/audio/sound.ogg")

-- Установка громкости
Sounds.SetVolume(0.5) -- Устанавливаем громкость на 50%

-- Получение текущей громкости
local currentVolume = Sounds.GetVolume()
print("Текущая громкость: " .. currentVolume)

-- Увеличение громкости на 10%
Sounds.IncreeseVolume(0.1)

-- Уменьшение громкости на 20%
Sounds.DecreeseVolume(0.2)

-- Прерывание воспроизведения звука
Sounds.SkipSound()
```
