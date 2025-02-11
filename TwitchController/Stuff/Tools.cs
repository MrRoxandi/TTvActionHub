using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchController.Stuff
{
    public static class Tools
    {
        private static readonly Random rng = new Random();

        // Существующий метод
        public static int RandomNumber(int min, int max)
        {
            return rng.Next(min, max + 1);
        }

        public static double RandomDouble(double min, double max)
        {
            return rng.NextDouble() * (max - min) + min;
        }

        // Асинхронный вариант RandomNumber с возможной задержкой
        public static async Task<int> RandomNumberAsync(int min, int max, int delay = 0)
        {
            if (delay > 0)
                await Task.Delay(delay);
            return rng.Next(min, max);
        }

        // Случайное число с плавающей точкой
        public static async Task<double> RandomDoubleAsync(double min, double max)
        {
            return await Task.Run(() => min + (rng.NextDouble() * (max - min)));
        }

        // Случайный элемент из списка
        public static async Task<T> RandomElementAsync<T>(IEnumerable<T> collection)
        {
            return await Task.Run(() => collection.ElementAt(rng.Next(collection.Count())));
        }

        // Перемешивание списка
        public static async Task<List<T>> ShuffleAsync<T>(IEnumerable<T> collection)
        {
            return await Task.Run(() =>
            {
                var list = collection.ToList();
                int n = list.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
                return list;
            });
        }

        // Генерация случайной строки
        public static async Task<string> RandomStringAsync(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return await Task.Run(() => new string(Enumerable.Repeat(chars, length)
                .Select(s => s[rng.Next(s.Length)]).ToArray()));
        }

        // Случайная задержка между выполнением действий
        public static async Task RandomDelayAsync(int minMs, int maxMs)
        {
            await Task.Delay(RandomNumber(minMs, maxMs));
        }

        
        // Генерация случайных координат в пределах прямоугольника
        public static async Task<(int x, int y)> RandomPositionAsync(int minX, int maxX, int minY, int maxY)
        {
            return await Task.Run(() => (
                RandomNumber(minX, maxX),
                RandomNumber(minY, maxY)
            ));
        }
    }
}