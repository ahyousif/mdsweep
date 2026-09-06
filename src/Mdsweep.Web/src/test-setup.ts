class TestResizeObserver {
  observe(): void {}
  disconnect(): void {}
  unobserve(): void {}
}

globalThis.ResizeObserver ??= TestResizeObserver;
