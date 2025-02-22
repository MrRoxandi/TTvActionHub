using TwitchController.Services.Http.Events;

namespace TwitchController.Services.Http
{
    internal static class Mapper
    {
        public static void Map(Service service)
        {
            service.Map("/", async () => 
            {
                return @"<!DOCTYPE html>
                        <html>
                            <head>
                                <title>Dynamic Content</title>
                            </head>
                        <body>

                        <div id='content'>
                          Loading...
                        </div>

                        <script>
                          function updateContent() {
                            fetch('http://localhost:8888/route/')
                              .then(response => {
                                if (!response.ok) {
                                  throw new Error(`HTTP error! status: ${response.status}`);
                                }
                                return response.text();
                              })
                              .then(html => {
                                if (html != '0') {
                                    document.getElementById('content').innerHTML = html;
                                    // TODO: FIX THIS please
                                    eval(html.match(/<script>(.*)<\/script>/s)[1]);
                                }
                              })
                              .catch(error => {
                                console.error('Error fetching content:', error);
                              });
                          }

                          // Initial update
                          updateContent();

                          // Update every 1 seconds (1000 milliseconds)
                          const interval = 1000; 
                          setInterval(updateContent, interval);
                        </script>

                        </body>
                        </html>";
            });


            service.Map("/route/", async () => 
            {
                IEvent res;
                if (!Services.Http.Repository.Events.TryDequeue(out res))
                {
                    if (Repository.LastEvent != null)
                    {
                        return Repository.LastEvent.Dispatch();
                    }
                    return "0";
                }

                Repository.LastEvent = res;
                return res.Dispatch();
            });

            // TODO(cactuzss): /animation/begin and /animation/end
            // чтобы можно было запускать страницы с продолжительными анимациями
            // и блокировать смену страницы до тех пор пока анимация не закончится.
            // А ещё нужно ставить див в 0 после анимации
        }
    }
}
