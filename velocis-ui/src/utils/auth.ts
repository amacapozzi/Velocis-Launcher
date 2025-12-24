import { AuthService, type User } from "@/services/auth-service";

export async function getCurrentUser(): Promise<User | null> {
  try {
    const response = await AuthService.getCurrentUser();
    return response.data.user;
  } catch (error) {
    return null;
  }
}

