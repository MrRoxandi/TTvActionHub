namespace TTvActionHub.LuaTools.Stuff
{
    public static class Funcs
    {
        private static readonly Random rng = new();

        public static int RandomNumber(int? min, int? max)
        {
            ArgumentNullException.ThrowIfNull(min, nameof(min));
            ArgumentNullException.ThrowIfNull(max, nameof(max));
            return rng.Next(min.Value, max.Value + 1);
        }

        public static double RandomDouble(double? min, double? max)
        {
            ArgumentNullException.ThrowIfNull(min, nameof(min));
            ArgumentNullException.ThrowIfNull(max, nameof(max));
            return rng.NextDouble() * (max.Value - min.Value) + min.Value;
        }

        public static string RandomElement(IEnumerable<string>? elements)
        {
            ArgumentNullException.ThrowIfNull(elements, nameof(elements));
            if(!elements.Any()) return string.Empty;
            return elements.ElementAt(rng.Next(elements.Count()));
        }

        public static List<string> Shuffle(IEnumerable<string>? elements)
        {
            if(elements is IEnumerable<string> elems) 
            {
                if (!elems.Any()) return [];
                var span = new Span<string>([.. elems]);
                rng.Shuffle(span);
                return span.ToArray().ToList();
            }
            else throw new ArgumentNullException(nameof(elements));
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string([.. Enumerable.Repeat(chars, length).Select(s => s[rng.Next(s.Length)])]);
        }

        public static void Delay(int? delay)
        {
            ArgumentNullException.ThrowIfNull(delay, nameof(delay));
            Thread.Sleep(delay!.Value);
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
            if (!elements.Any()) return string.Empty;
            return string.Join(sep, elements);
        }

    }
}