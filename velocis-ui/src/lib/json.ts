export async function json<T>(input: RequestInfo, init?: RequestInit) {
  const response = await fetch(input, init);
  if (!response.ok) {
    throw new Error("fetch failed.");
  }

  const data = await response.json();
  return data as Promise<T>;
}
