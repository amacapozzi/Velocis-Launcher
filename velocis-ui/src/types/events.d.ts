export type EventType =
  | "close"
  | "setTitle"
  | "setSize"
  | "setPosition"
  | "minimize"
  | "maximize"
  | "navigate"
  | "log"
  | "alert"
  | string;

export interface BaseEventPayload {
  [key: string]: unknown;
}

export interface EventData<T extends BaseEventPayload = BaseEventPayload> {
  type: EventType;
  payload?: T;
  [key: string]: unknown;
}

declare global {
  interface Window {
    external?: {
      sendMessage?: (message: string) => void;
    };
  }
}

export {};
