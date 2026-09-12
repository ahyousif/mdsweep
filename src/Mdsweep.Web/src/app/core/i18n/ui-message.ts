import { inject, Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export interface UiMessage {
  key: string;
  params?: Record<string, string | number>;
  details?: UiMessage[];
}

@Pipe({ name: 'uiMessage', pure: false })
export class UiMessagePipe implements PipeTransform {
  private readonly translate = inject(TranslateService);
  transform(message: UiMessage | null | undefined): string {
    if (!message) return '';
    if (message.details?.length)
      return message.details.map((item) => this.transform(item)).join(' ');
    const value = this.translate.instant(message.key, message.params);
    return value === message.key ? this.translate.instant('errors.requestFailed') : value;
  }
}
