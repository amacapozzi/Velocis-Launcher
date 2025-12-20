import { Elysia } from "elysia";
import { libraryRouter } from "./modules/library/controller";

const app = new Elysia()
  .get("/", () => "Hello Elysia")
  .listen(3000)
  .use(libraryRouter);

console.log(
  `🦊 Elysia is running at ${app.server?.hostname}:${app.server?.port}`
);
