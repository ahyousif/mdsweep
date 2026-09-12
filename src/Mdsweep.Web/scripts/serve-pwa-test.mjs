import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../dist/web/browser/', import.meta.url));
const types = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.webmanifest': 'application/manifest+json',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.svg': 'image/svg+xml',
};
createServer(async (request, response) => {
  const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
  // Synthetic, loopback-only responses let the real service worker own fetches.
  if (pathname === '/api/auth/session') {
    response.writeHead(200, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
    response.end(
      JSON.stringify({
        userId: 'synthetic-user',
        displayName: 'Synthetic User',
        email: 'synthetic@example.test',
        activeTenant: { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: ['Administrator'] },
        availableTenants: [
          { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: ['Administrator'] },
        ],
      }),
    );
    return;
  }
  if (pathname === '/api/users') {
    response.writeHead(200, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
    response.end('[]');
    return;
  }
  const file = resolve(root, '.' + (extname(pathname) ? pathname : '/index.html'));
  if (!file.startsWith(root.endsWith(sep) ? root : root + sep) || pathname.startsWith('/api/')) {
    response.writeHead(404).end();
    return;
  }
  try {
    response.writeHead(200, {
      'Content-Type': types[extname(file)] ?? 'application/octet-stream',
      'Cache-Control': 'no-cache',
    });
    response.end(await readFile(file));
  } catch {
    response.writeHead(404).end();
  }
}).listen(4218, '127.0.0.1');
