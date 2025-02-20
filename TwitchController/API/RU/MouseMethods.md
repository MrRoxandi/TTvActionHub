## Mouse API methods

## 1. `MouseButtons`:

Доступные кнопки мыши:

- `Left` (Левая кнопка)
- `Right` (Правая кнопка)
- `Middle` (Средняя кнопка/колесо)

### Пример использования в `config.lua`:

```lua
local Mouse = import('TwitchController', 'TwitchController.Hardware').Mouse -- Импорт интерфейса
local button = Mouse.MouseButton.Left -- Получение кнопки "Левая"
```

## 2. Основные методы:

| Метод                                                 | Описание                                                  |
| ----------------------------------------------------- | --------------------------------------------------------- |
| `MoveAsync(int x, int y)`                             | Перемещает курсор на координаты (x, y)                    |
| `ClickAsync(MouseButton button)`                      | Нажимает указанную кнопку                                 |
| `HoldAsync(MouseButton button, int timeDelay = 1000)` | Удерживает кнопку в течение указанного времени (мс)       |
| `PressAsync(MouseButton button)`                      | Нажимает кнопку (не отпуская)                             |
| `ReleaseAsync(MouseButton button)`                    | Отпускает кнопку                                          |
| `ScrollAsync(int delta)`                              | Прокручивает колесико (дельта > 0 вверх, дельта < 0 вниз) |

### Примеры использования в `config.lua`:

**Перемещение курсора:**

```lua
Mouse.MoveAsync(500, 300) -- Переместить курсор в (500, 300)
```

**Клик левой кнопкой:**

```lua
local button = Mouse.MouseButton.Left
Mouse.ClickAsync(button)
```

**Удержание правой кнопки на 2 секунды:**

```lua
local button = Mouse.MouseButton.Right
Mouse.HoldAsync(button, 2000)
```

**Прокрутка колеса:**

```lua
Mouse.ScrollAsync(120)  -- Прокрутка вверх
Mouse.ScrollAsync(-120) -- Прокрутка вниз
```

**Нажатие и отпускание кнопки:**

```lua
local button = Mouse.MouseButton.Middle
Mouse.PressAsync(button)  -- Нажать среднюю кнопку
-- ... выполнить действия ...
Mouse.ReleaseAsync(button) -- Отпустить
```

## 3. Особенности:

- Для `ScrollAsync` значение `delta` обычно кратно 120 (стандартный шаг прокрутки).
- Методы `HoldAsync`, `PressAsync`, `ReleaseAsync` работают с любой из кнопок: `Left`, `Right`, `Middle`.
- Координаты в `MoveAsync` соответствуют разрешению экрана (например, 1920x1080).
