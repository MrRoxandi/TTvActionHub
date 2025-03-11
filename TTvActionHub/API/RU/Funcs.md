## Документация для модуля Funcs в `TTvActionHub.LuaTools.Stuff`

Этот модуль предоставляет набор полезных функций для выполнения различных операций, таких как генерация случайных чисел, выбор случайного элемента из коллекции, перемешивание коллекции и создание случайных строк.

### Функции

| Функция                                                                      | Описание                                                                                                                                                                                                       | Возвращаемое значение |
| ---------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------- |
| `RandomNumber(int? min, int? max)`                                           | Генерирует случайное целое число в диапазоне от `min` до `max` (включительно). Оба аргумента `min` и `max` **обязательны**.                                                                                    | `int`                 |
| `RandomDouble(double? min, double? max)`                                     | Генерирует случайное число с плавающей точкой в диапазоне от `min` до `max`. Оба аргумента `min` и `max` **обязательны**.                                                                                      | `double`              |
| `RandomNumberAsync(int? min, int? max)`                                      | Асинхронно генерирует случайное целое число в диапазоне от `min` до `max` (включительно). Оба аргумента `min` и `max` **обязательны**.                                                                         | `int`                 |
| `RandomDoubleAsync(double? min, double? max)`                                | Асинхронно генерирует случайное число с плавающей точкой в диапазоне от `min` до `max`. Оба аргумента `min` и `max` **обязательны**.                                                                           | `double`              |
| `RandomElementAsync(IEnumerable<string>? collection)`                        | Асинхронно выбирает случайный элемент из переданной коллекции строк `collection`. Если коллекция пуста, возвращает пустую строку. Аргумент `collection` **обязателен**.                                        | `string`              |
| `ShuffleAsync(IEnumerable<string>? collection)`                              | Асинхронно перемешивает переданную коллекцию строк `collection` и возвращает новую перемешанную коллекцию в виде списка. Если коллекция пуста, возвращает пустой список. Аргумент `collection` **обязателен**. | `List<string>`        |
| `RandomStringAsync(int length)`                                              | Асинхронно генерирует случайную строку указанной длины `length`, состоящую из букв (в верхнем и нижнем регистре) и цифр.                                                                                       | `string`              |
| `DelayAsync(int? delay)`                                                     | Асинхронно приостанавливает выполнение на указанное количество миллисекунд `delay`. Аргумент `delay` **обязателен**.                                                                                           | `void`                |
| `RandomPositionAsync(int? minX, int? maxX, int? minY, int? maxY)`            | Асинхронно генерирует случайную позицию (Point) со случайными координатами X и Y в указанных диапазонах. Все аргументы **обязательны**.                                                                        | `Funcs.Point`         |
| `CollectionToStringAsync(IEnumerable<string>? collection, string sep = " ")` | Асинхронно преобразует коллекцию строк `collection` в одну строку, разделяя элементы указанным разделителем `sep` (по умолчанию пробел). Аргумент `collection` **обязателен**.                                 | `string`              |

### Типы

#### `Point`

Структура, представляющая точку с координатами X и Y.

| Свойство | Тип   | Описание     |
| -------- | ----- | ------------ |
| `X`      | `int` | Координата X |
| `Y`      | `int` | Координата Y |

**Примечания:**

- Все функции, заканчивающиеся на `Async`, выполняются асинхронно, не блокируя основной поток программы.
- Перед использованием любой функции убедитесь, что все обязательные аргументы указаны. В противном случае будет выброшено исключение `ArgumentNullException`.
- Для работы с `RandomPositionAsync` необходимо указывать все четыре параметра: `minX`, `maxX`, `minY`, `maxY`.
- Для получения результата от асинхронных функций, необходимо использовать `.Result`. Например: `local randomNumber = Funcs.RandomNumberAsync(1, 100).Result`

### Пример использования в `config.lua`

```lua
local Funcs = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Funcs

-- Генерация случайного числа от 1 до 100
local randomNumber = Funcs.RandomNumber(1, 100)
print("Случайное число: " .. randomNumber)

-- Генерация случайного числа с плавающей точкой от 0.0 до 1.0
local randomDouble = Funcs.RandomDouble(0.0, 1.0)
print("Случайное число с плавающей точкой: " .. randomDouble)

-- Выбор случайного элемента из списка
local myList = {"apple", "banana", "cherry"}
local randomElement = Funcs.RandomElementAsync(myList).Result
print("Случайный элемент: " .. randomElement)


-- Перемешивание списка
local myList = {"apple", "banana", "cherry"}
local shuffledList = Funcs.ShuffleAsync(myList).Result
print("Перемешанный список:")
for i, element in ipairs(shuffledList) do
    print(i .. ": " .. element)
end


-- Генерация случайной строки длиной 10 символов
local randomString = Funcs.RandomStringAsync(10).Result
print("Случайная строка: " .. randomString)


-- Приостановка выполнения на 1 секунду
Funcs.DelayAsync(1000).Result

-- Получение случайной позиции
local pos = Funcs.RandomPositionAsync(0, 100, 0, 100).Result
print("Случайная позиция: X=" .. pos.X .. ", Y=" .. pos.Y)


-- Преобразование списка в строку с разделителем
local myList = {"apple", "banana", "cherry"}
local myString = Funcs.CollectionToStringAsync(myList, ", ").Result
print("Строка: " .. myString)
```
