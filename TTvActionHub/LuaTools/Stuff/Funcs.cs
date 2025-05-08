namespace TTvActionHub.LuaTools.Stuff
{
    public static class Funcs
    {
        public static int RandomNumber(int? min, int? max)
        {
            ArgumentNullException.ThrowIfNull(min, nameof(min));
            ArgumentNullException.ThrowIfNull(max, nameof(max));
            return Random.Shared.Next(min.Value, max.Value + 1);
        }

        public static double RandomDouble(double? min, double? max)
        {
            ArgumentNullException.ThrowIfNull(min, nameof(min));
            ArgumentNullException.ThrowIfNull(max, nameof(max));
            return Random.Shared.NextDouble() * (max.Value - min.Value) + min.Value;
        }

        public static string RandomElement(IEnumerable<string>? elements)
        {
            ArgumentNullException.ThrowIfNull(elements, nameof(elements));
            var enumerable = elements as string[] ?? elements.ToArray();
            return enumerable.Length == 0 ? string.Empty : enumerable.ElementAt(Random.Shared.Next(enumerable.Length));
        }

        public static List<string> Shuffle(IEnumerable<string>? elements)
        {
            if(elements is not null) 
            {
                var enumerable = elements as string[] ?? elements.ToArray();
                if (enumerable.Length == 0) return [];
                var span = new Span<string>([.. enumerable]);
                Random.Shared.Shuffle(span);
                return span.ToArray().ToList();
            }
            else throw new ArgumentNullException(nameof(elements));
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string([.. Enumerable.Repeat(chars, length).Select(s => s[Random.Shared.Next(s.Length)])]);
        }

        public static void Delay(int? delay)
        {
            ArgumentNullException.ThrowIfNull(delay, nameof(delay));
            Thread.Sleep(delay.Value);
        }

        public struct Point(int x, int y)
        {
            public int X { get; set; } = x;
            public int Y { get; set; } = y;
        }

        public static Point RandomPosition(int? minX, int? maxX, int? minY, int? maxY)
        {
            return new Point(RandomNumber(minX, maxX), RandomNumber(minY, maxY));
        }

        public static string CollectionToString(IEnumerable<string>? elements, string sep = " ")
        {
            ArgumentNullException.ThrowIfNull(elements, nameof(elements));
            var enumerable = elements as string[] ?? elements.ToArray();
            return enumerable.Length == 0 ? string.Empty : string.Join(sep, enumerable);
        }

    }
}