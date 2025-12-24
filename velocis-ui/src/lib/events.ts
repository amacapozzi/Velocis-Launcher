import type { EventType, EventData, BaseEventPayload } from "@/types/events";

export class Event<T extends BaseEventPayload = BaseEventPayload> {
  private type: EventType;
  private payload?: T;
  private additionalData: Record<string, unknown>;

  constructor(
    type: EventType,
    payload?: T,
    additionalData?: Record<string, unknown>
  ) {
    this.type = type;
    this.payload = payload;
    this.additionalData = additionalData || {};
  }

  send(): void {
    if (typeof window === "undefined") {
      console.warn("window is not available. Event not sent:", this.type);
      return;
    }

    const external = (
      window as Window & {
        external?: { sendMessage?: (message: string) => void };
      }
    ).external;

    if (!external?.sendMessage) {
      console.warn(
        "window.external.sendMessage is not available. Event not sent:",
        this.type
      );
      return;
    }

    const eventData: EventData<T> = {
      type: this.type,
      ...(this.payload && { payload: this.payload }),
      ...this.additionalData,
    };

    try {
      const message = JSON.stringify(eventData);
      external.sendMessage(message);
    } catch (error) {
      console.error("Failed to send event:", error);
      throw error;
    }
  }

  getData(): EventData<T> {
    return {
      type: this.type,
      ...(this.payload && { payload: this.payload }),
      ...this.additionalData,
    };
  }
}

export function createEvent<T extends BaseEventPayload = BaseEventPayload>(
  type: EventType,
  payload?: T,
  additionalData?: Record<string, unknown>
): Event<T> {
  return new Event(type, payload, additionalData);
}
