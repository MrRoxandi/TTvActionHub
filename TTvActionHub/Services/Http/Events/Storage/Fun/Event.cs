namespace TTvActionHub.Services.Http.Events.Storage.Fun
{
    internal class Event : IEvent
    {
        private static readonly string content = @"
        <div>
            <div>
              <canvas id='myCanvas' width='200' height='100'></canvas>
            </div>

            <script>
              console.log('FUCK')
              const canvas = document.getElementById('myCanvas');
              const ctx = canvas.getContext('2d');

              function getRandomColor() {
                const r = Math.floor(Math.random() * 256);
                const g = Math.floor(Math.random() * 256);
                const b = Math.floor(Math.random() * 256);
                return `rgb(${r},${g},${b})`;
              }

              function draw() {
                ctx.fillStyle = getRandomColor();
                ctx.fillRect(0, 0, canvas.width, canvas.height);
              }

              setInterval(draw, 100); // Меняет цвет каждые 100 миллисекунд
            </script>
        </div>";

        public string Dispatch()
        {
            return Event.content;
        }
    }
}
