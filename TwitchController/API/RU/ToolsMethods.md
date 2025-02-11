## Tools API methods

## 1. `RandomNumber`:

Генерация случайных чисел.

- `RandomNumber(int min, int max)`: Возвращает случайное целое число в диапазоне [`min`, `max`].
- `RandomDouble(double min, double max)`: Возвращает случайное число с плавающей точкой в диапазоне [`min`, `max`].
- `RandomNumberAsync(int min, int max, int delay = 0)`: Асинхронно возвращает случайное целое число в диапазоне [`min`, `max`], с возможной задержкой.
- `RandomDoubleAsync(double min, double max)`: Асинхронно возвращает случайное число с плавающей точкой.

### Примеры использования в `config.lua`:

```lua
local num = Tools.RandomNumber(1, 100) -- Генерация случайного числа от 1 до 99
local numAsync = Tools.RandomNumberAsync(1, 100, 500) -- Асинхронная генерация с задержкой 500 мс
```

## 2. `RandomElement`:

Выбор случайного элемента из списка.

- `RandomElementAsync<T>(IEnumerable<T> collection)`: Асинхронно выбирает случайный элемент из списка.

### Пример:

```lua
local list = {"apple", "banana", "cherry"}
local fruit = Tools.RandomElementAsync(list)
```

## 3. `Shuffle`:

Перемешивание списка.

- `ShuffleAsync<T>(IEnumerable<T> collection)`: Асинхронно перемешивает элементы списка.

### Пример:

```lua
local list = {1, 2, 3, 4, 5}
local shuffled = Tools.ShuffleAsync(list)
```

## 4. `RandomString`:

Генерация случайной строки.

- `RandomStringAsync(int length)`: Асинхронно создает случайную строку заданной длины из букв и цифр.

### Пример:

```lua
local randomStr = Tools.RandomStringAsync(10) -- Строка из 10 случайных символов
```

## 5. `RandomDelay`:

Создание случайной задержки перед выполнением следующего действия.

- `RandomDelayAsync(int minMs, int maxMs)`: Асинхронно ожидает случайное количество миллисекунд в заданном диапазоне.

### Пример:

```lua
Tools.RandomDelayAsync(500, 2000) -- Задержка от 500 до 2000 мс
```

## 7. `RandomPosition`:

Генерация случайных координат в пределах прямоугольной области.

- `RandomPositionAsync(int minX, int maxX, int minY, int maxY)`: Асинхронно возвращает случайные координаты в пределах заданных границ.

### Пример:

```lua
local x, y = Tools.RandomPositionAsync(0, 1920, 0, 1080) -- Координаты в пределах экрана 1920x1080
```
