import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { routeTree } from './routeTree.gen';
import { createRouter, RouterProvider } from '@tanstack/react-router';
// import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
// import { AuthProvider } from './contexts/auth-context';

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  scrollRestoration: true,
});

declare module '@tanstack/react-router' {
  export interface Register {
    router: typeof router;
  }
}

// // 1. Vytvoř instanci klienta mimo komponentu
// const queryClient = new QueryClient({
//   defaultOptions: {
//     queries: {
//       staleTime: 1000 * 60 * 5, // Data jsou "čerstvá" 5 minut
//       retry: 1, // Při chybě zkusit jen jeden restart
//     },
//   },
// });

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* <QueryClientProvider client={queryClient}> */}
      {/* <AuthProvider> */}
        <RouterProvider router={router} />
      {/* </AuthProvider> */}
    {/* </QueryClientProvider> */}
  </StrictMode>
);
