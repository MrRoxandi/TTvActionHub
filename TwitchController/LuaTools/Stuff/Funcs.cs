using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchController.LuaTools.Stuff
{
    public static class Funcs
    {
        private static readonly Random rng = new();

        public static int RandomNumber(int? min, int? max)
        {
            if(min is int a && max is int b)
                return rng.Next(a, b + 1);
            else {
                if (min is null) throw new ArgumentNullException(nameof(min));
                else throw new ArgumentNullException(nameof(max));
            }
        }

        public static double RandomDouble(double? min, double? max)
        {
            if (min is double a && max is double b)
                return rng.NextDouble() * (b - a) + a;
            else
            {
                if (min is null) throw new ArgumentNullException(nameof(min));
                else throw new ArgumentNullException(nameof(max));
            }
        }

        public static async Task<int> RandomNumberAsync(int? min, int? max)
        {
            return await Task.Run(() => RandomNumber(min, max));
        }

        public static async Task<double> RandomDoubleAsync(double? min, double? max)
        {
            return await Task.Run(() => RandomDouble(min, max));
        }

        public static async Task<string> RandomElementAsync(IEnumerable<string>? collection)
        {   
            if(collection is IEnumerable<string> col)
            {
                if (!col.Any()) return await Task.Run(() => string.Empty);
                return await Task.Run(() => col.ElementAt(rng.Next(col.Count())));
            } 
            else throw new ArgumentNullException(nameof(collection));
        }

        public static async Task<List<string>> ShuffleAsync(IEnumerable<string>? collection)
        {
            if (collection is IEnumerable<string> col)
            {
                if (!collection.Any()) return await Task.Run(() => new List<string>());
                return await Task.Run(() =>
                {
                    Span<string> span = new(collection.ToArray());
                    rng.Shuffle(span);
                    return span.ToArray().ToList();
                });
            } 
            else throw new ArgumentNullException(nameof(collection));
        }

        public static async Task<string> RandomStringAsync(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return await Task.Run(() => new string(Enumerable.Repeat(chars, length)
                .Select(s => s[rng.Next(s.Length)]).ToArray()));
        }

        public static async Task DelayAsync(int? delay)
        {
            if(delay is int d)
                await Task.Delay(d);
            else throw new ArgumentNullException(nameof(delay));
        }

        public struct Point(int x, int y)
        {
            public int X { get; set; } = x;
            public int Y { get; set; } = y;
        }

        public static async Task<Point> RandomPositionAsync(int? minX, int? maxX, int? minY, int? maxY)
        {
            return await Task.Run(() => new Point(RandomNumber(minX, maxX), RandomNumber(minY, maxY)));
        }

        public static async Task<string> CollectionToStringAsync(IEnumerable<string>? collection, string sep = " ")
        {
            if(collection is IEnumerable<string> list)
            {
                if(!list.Any()) return await Task.Run(() => string.Empty);
                else return await Task.Run(() => string.Join(sep, list));
            } 
            else throw new ArgumentNullException(nameof(collection));
        }
    }
}