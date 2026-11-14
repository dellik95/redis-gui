import { env } from 'node:process';

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS?.split(';')[0] || 'https://localhost:61034';

export default {
  '/api/**': { target, secure: false, changeOrigin: true },
  '/hub/**': { target, secure: false, changeOrigin: true, ws: true }
};
