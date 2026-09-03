import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthSessionService } from '@app/core/auth/auth-session.service';
import { vi } from 'vitest';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  let fixture: ComponentFixture<AppShell>;
  let signOut: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    signOut = vi.fn().mockResolvedValue(undefined);

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: { signOut },
        },
      ],
    });

    fixture = TestBed.createComponent(AppShell);

    fixture.componentRef.setInput('session', {
      appUserId: 'd449d57a-8f51-4a2a-9624-d6d474aaa6e7',
      displayName: 'Synthetic Dispatcher',
      tenantId: 'acme-transport',
      roles: ['Dispatcher', 'Administrator'],
    });
  });

  it('renders role-neutral authenticated application chrome', () => {
    fixture.detectChanges();

    const page = fixture.nativeElement.textContent as string;

    expect(page).toContain('MDSweep');
    expect(page).toContain('Trips');
    expect(page).toContain('Synthetic Dispatcher');
    expect(page).not.toContain('d449d57a-8f51-4a2a-9624-d6d474aaa6e7');
    expect(page).not.toContain('Workspace');
    expect(page).not.toContain('Import manifest');
    expect(page).not.toContain('Appearance');
    expect(page).not.toContain('Account');

    expect(
      fixture.nativeElement.querySelector('button[hlmSidebarRail][aria-label="Toggle navigation"]'),
    ).not.toBeNull();

    expect(fixture.nativeElement.querySelector('[aria-label="Open user menu"]')).not.toBeNull();
  });

  it('shows an actionable error when sign out fails', async () => {
    signOut.mockRejectedValueOnce(new Error('network failed'));

    fixture.detectChanges();

    await fixture.componentInstance.signOut();
    fixture.detectChanges();

    expect(fixture.componentInstance.signOutError()).toBe('Could not sign out. Try again.');

    expect(fixture.nativeElement.textContent).toContain('Could not sign out. Try again.');
  });
});
