const http = require('http');
const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..', 'html');
const port = Number(process.env.PORT) || 47831;

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
};

const server = http.createServer((req, res) => {
  const url = decodeURIComponent((req.url || '/').split('?')[0]);
  if (url === '/favicon.ico') {
    res.writeHead(204);
    res.end();
    return;
  }
  const relative = url === '/' ? 'index.html' : url.replace(/^\/+/, '');
  const file = path.normalize(path.join(root, relative));

  if (!file.startsWith(root)) {
    res.writeHead(403);
    res.end('Forbidden');
    return;
  }

  fs.readFile(file, (error, data) => {
    if (error) {
      res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Not found');
      return;
    }
    res.writeHead(200, {
      'Content-Type': types[path.extname(file)] || 'application/octet-stream',
      'Cache-Control': 'no-store',
    });
    res.end(data);
  });
});

server.listen(port, '0.0.0.0', () => {
  console.log(`AFK bot preview: http://127.0.0.1:${port}`);
});
