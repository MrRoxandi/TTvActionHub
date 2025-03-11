## Документация для модуля Storage в `TTvActionHub.LuaTools.Stuff`

Этот модуль предоставляет доступ к внутреннему хранилищу данных, позволяя скриптам сохранять и извлекать информацию.

### Функции

| Функция                                        | Описание                                                                                                                         | Возвращаемое значение |
| ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- | --------------------- |
| `Contains(string name)`                        | Проверяет, существует ли значение с указанным именем в хранилище.                                                                | `bool`                |
| `ContainsAsync(string name)`                   | Асинхронно проверяет, существует ли значение с указанным именем в хранилище.                                                     | `bool`                |
| `InsertValueAsync<T>(string name, T value)`    | Асинхронно добавляет или обновляет значение указанного типа `T` в хранилище.                                                     | `void`                |
| `InsertValue<T>(string name, T value)`         | Добавляет или обновляет значение указанного типа `T` в хранилище.                                                                | `void`                |
| `GetValueAsync<T>(string name)`                | Асинхронно получает значение указанного типа `T` из хранилища. Возвращает `null`, если значение не найдено или тип не совпадает. | `T?`                  |
| `GetValue<T>(string name)`                     | Получает значение указанного типа `T` из хранилища. Возвращает `null`, если значение не найдено или тип не совпадает.            | `T?`                  |
| `RemoveValueAsync(string name)`                | Асинхронно удаляет значение с указанным именем из хранилища.                                                                     | `bool`                |
| `RemoveValue(string name)`                     | Удаляет значение с указанным именем из хранилища.                                                                                | `bool`                |
| `InsertIntAsync(string name, int value)`       | Асинхронно добавляет или обновляет целочисленное значение в хранилище.                                                           | `void`                |
| `InsertInt(string name, int value)`            | Добавляет или обновляет целочисленное значение в хранилище.                                                                      | `void`                |
| `GetIntAsync(string name)`                     | Асинхронно получает целочисленное значение из хранилища. Возвращает `null`, если значение не найдено.                            | `int?`                |
| `GetInt(string name)`                          | Получает целочисленное значение из хранилища. Возвращает `null`, если значение не найдено.                                       | `int?`                |
| `InsertCharAsync(string name, char value)`     | Асинхронно добавляет или обновляет символ в хранилище.                                                                           | `void`                |
| `InsertChar(string name, char value)`          | Добавляет или обновляет символ в хранилище.                                                                                      | `void`                |
| `GetCharAsync(string name)`                    | Асинхронно получает символ из хранилища. Возвращает `null`, если значение не найдено.                                            | `char?`               |
| `GetChar(string name)`                         | Получает символ из хранилища. Возвращает `null`, если значение не найдено.                                                       | `char?`               |
| `InsertBoolAsync(string name, bool value)`     | Асинхронно добавляет или обновляет логическое значение в хранилище.                                                              | `void`                |
| `InsertBool(string name, bool value)`          | Добавляет или обновляет логическое значение в хранилище.                                                                         | `void`                |
| `GetBoolAsync(string name)`                    | Асинхронно получает логическое значение из хранилища. Возвращает `null`, если значение не найдено.                               | `bool?`               |
| `GetBool(string name)`                         | Получает логическое значение из хранилища. Возвращает `null`, если значение не найдено.                                          | `bool?`               |
| `InsertStringAsync(string name, string value)` | Асинхронно добавляет или обновляет строковое значение в хранилище.                                                               | `void`                |
| `InsertString(string name, string value)`      | Добавляет или обновляет строковое значение в хранилище.                                                                          | `void`                |
| `GetStringAsync(string name)`                  | Асинхронно получает строковое значение из хранилища. Возвращает `null`, если значение не найдено.                                | `string?`             |
| `GetString(string name)`                       | Получает строковое значение из хранилища. Возвращает `null`, если значение не найдено.                                           | `string?`             |
| `InsertDoubleAsync(string name, double value)` | Асинхронно добавляет или обновляет значение с плавающей точкой двойной точности в хранилище.                                     | `void`                |
| `InsertDouble(string name, double value)`      | Добавляет или обновляет значение с плавающей точкой двойной точности в хранилище.                                                | `void`                |
| `GetDoubleAsync(string name)`                  | Асинхронно получает значение с плавающей точкой двойной точности из хранилища. Возвращает `null`, если значение не найдено.      | `double?`             |
| `GetDouble(string name)`                       | Получает значение с плавающей точкой двойной точности из хранилища. Возвращает `null`, если значение не найдено.                 | `double?`             |

**Примечания:**

- Все функции, заканчивающиеся на `Async`, выполняются асинхронно, не блокируя основной поток программы. Рекомендуется использовать асинхронные версии функций для повышения производительности.
- Тип `T` в функциях `InsertValueAsync<T>`, `InsertValue<T>`, `GetValueAsync<T>`, и `GetValue<T>` должен быть указан явно. Поддерживаются типы `int`, `char`, `bool`, `string`, и `double`. Для других типов данных используйте `InsertValueAsync` и `GetValueAsync` с явным указанием типа.
- Перед использованием любой функции убедитесь, что сервис хранилища инициализирован. Это обычно делается автоматически программой, но стоит иметь это в виду.
- Для получения результата от асинхронных функций, необходимо использовать `.Result`. Например: `local myVariable = Storage.GetStringAsync("myVariable").Result`

### Пример использования в `config.lua`

```lua
local Storage = import('TTvActionHub', 'TTvActionHub.LuaTools.Stuff').Storage

-- Сохранение строкового значения
Storage.InsertStringAsync("myVariable", "Hello, World!")

-- Получение строкового значения
local myVariable = Storage.GetString("myVariable")
if myVariable then
  print("Значение myVariable: " .. myVariable)
else
  print("Переменная myVariable не найдена")
end

-- Сохранение целочисленного значения
Storage.InsertInt("myNumber", 42)

-- Получение целочисленного значения
local myNumber = Storage.GetInt("myNumber")
if myNumber then
  print("Значение myNumber: " .. myNumber)
else
  print("Переменная myNumber не найдена")
end

-- Проверка наличия значения
local hasValue = Storage.Contains("myVariable")
print("Переменная myVariable существует: " .. tostring(hasValue))

-- Удаление значения
Storage.RemoveValueAsync("myVariable")

-- Проверка наличия значения после удаления
local hasValue = Storage.Contains("myVariable")
print("Переменная myVariable существует после удаления: " .. tostring(hasValue))

-- Асинхронное получение строкового значения
local myVariableAsync = Storage.GetStringAsync("myVariable").Result
if myVariableAsync then
    print("Асинхронное значение myVariable: " .. myVariableAsync)
else
    print("Асинхронная переменная myVariable не найдена")
end
```
