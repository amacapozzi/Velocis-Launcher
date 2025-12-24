import { auth } from "@/auth";
import { defineMiddleware } from "astro:middleware";

export const onRequest = defineMiddleware(async (context, next) => {
  const session = await auth.api.getSession({
    headers: context.request.headers,
  });

  const pathname = context.url.pathname;

  if (pathname === "/auth/login" && session) {
    return context.redirect("/", 302);
  }

  if (pathname === "/" && !session) {
    return context.redirect("/auth/login", 302);
  }

  return next();
});
