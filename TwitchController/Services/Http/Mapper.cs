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

                        <div id=""content"">
                          Loading...
                        </div>

                        <script>
                          function updateContent() {
                            fetch('http://localhost:8888/hello/')
                              .then(response => {
                                if (!response.ok) {
                                  throw new Error(`HTTP error! status: ${response.status}`);
                                }
                                return response.text();
                              })
                              .then(html => {
                                document.getElementById('content').innerHTML = html;
                              })
                              .catch(error => {
                                console.error('Error fetching content:', error);
                                document.getElementById('content').innerHTML = '<p>Error loading content.</p>';
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


            service.Map("/hello/", async () => 
            {
                Storage.XD = !Storage.XD;
                if (Storage.XD)
                    return @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta charset='utf8'>
                                </head>
                                <body>
                                    <h2 id='test'> YES </h2>
                                </body>
                            </html>";

                return @"<!DOCTYPE html>
                        <html>
                            <head>
                                <meta charset='utf8'>
                            </head>
                            <body>
                                <h2 id='test'> NO </h2>
                            </body>
                        </html>";
            });
        }
    }
}
