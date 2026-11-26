import { setupServer } from "msw/node";

/** MSW server shared by the tests; individual tests add handlers with `server.use(...)`. */
export const server = setupServer();
