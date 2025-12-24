import { Elysia } from "elysia";
import { libraryRouter } from "./modules/library/controller";
import { cors } from "@elysiajs/cors";

const app = new Elysia()
  .use(
    cors({
      origin: "http://localhost:4321",
      credentials: true,
    })
  )
  .get("/", () => "Hello Elysia")
  .use(libraryRouter)
  .listen(3000);

console.log(
  `🦊 Elysia is running at ${app.server?.hostname}:${app.server?.port}`
);
