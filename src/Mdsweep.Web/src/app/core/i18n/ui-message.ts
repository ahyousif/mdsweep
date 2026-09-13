import { inject, Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export interface UiMessage {
  key: string;
  params?: Record<string, string | number>;
}

export type UiFeedback = UiMessage | UiMessage[];

@Pipe({ name: 'uiMessage', pure: false })
export class UiMessagePipe implements PipeTransform {
  private readonly translate = inject(TranslateService);
  transform(message: UiFeedback | null | undefined): string {
    if (!message) return '';
    const messages = Array.isArray(message) ? message : [message];
    return messages.map((item) => this.translateMessage(item)).join(' ');
  }

  private translateMessage(message: UiMessage): string {
    const value = this.translate.instant(message.key, message.params);
    return value === message.key ? this.translate.instant('errors.requestFailed') : value;
  }
}
