import { TestBed } from '@angular/core/testing';
import { provideLocalization } from '@app/core/i18n/localization.providers';
import { LanguageService } from '@app/core/i18n/language.service';
import { Trip } from '../trips-types';
import TripDetail from './trip-detail';

function setup() {
  TestBed.configureTestingModule({ providers: [provideLocalization()] });
  const fixture = TestBed.createComponent(TripDetail);
  fixture.componentRef.setInput('trip', {
    passengerFirstName: 'Synthetic',
    passengerLastName: 'Passenger',
    direction: 'From',
    brokerStatus: 'VALID',
    manualPickupTime: null,
    calculatedPickupTime: null,
    returnPickupTime: '09:30:00',
    scheduledPickupTime: '09:30:00',
    estimatedTravelMinutes: 0,
    estimatedDistanceMeters: null,
    pickup: { address: '100 Sample & Test St', city: 'Phoenix', state: 'AZ', zip: '85001' },
    dropoff: { address: '200 Synthetic Way', city: 'Mesa', state: 'AZ', zip: '85201' },
  } as Trip);
  fixture.detectChanges();
  return fixture;
}

describe('Trip detail planning actions', () => {
  it('builds encoded directions from both complete addresses with safe external-link behavior', () => {
    const fixture = setup();
    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    const url = new URL(link.href);
    expect(url.origin).toBe('https://www.google.com');
    expect(url.pathname).toBe('/maps/dir/');
    expect(url.searchParams.get('origin')).toBe('100 Sample & Test St, Phoenix, AZ, 85001');
    expect(url.searchParams.get('destination')).toBe('200 Synthetic Way, Mesa, AZ, 85201');
    expect(url.searchParams.get('api')).toBe('1');
    expect(link.target).toBe('_blank');
    expect(link.rel).toBe('noopener noreferrer');
  });

  it('derives effective source and updates it when language or query data changes', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    expect(component.scheduledPickupSource()).toBe('Broker supplied');
    fixture.componentRef.setInput('trip', { ...component.trip(), manualPickupTime: '09:00:00' });
    fixture.detectChanges();
    expect(component.scheduledPickupSource()).toBe('Manual override');
    TestBed.inject(LanguageService).setLanguage('ar');
    fixture.detectChanges();
    expect(component.scheduledPickupSource()).toBe('تعديل يدوي');
    TestBed.inject(LanguageService).setLanguage('en');
    fixture.componentRef.setInput('trip', {
      ...component.trip(),
      manualPickupTime: null,
      calculatedPickupTime: '09:00:00',
    });
    fixture.detectChanges();
    expect(component.scheduledPickupSource()).toBe('Calculated');
    fixture.componentRef.setInput('trip', {
      ...component.trip(),
      calculatedPickupTime: null,
      returnPickupTime: null,
      scheduledPickupTime: null,
      isWillCall: true,
    });
    fixture.detectChanges();
    expect(component.scheduledPickupSource()).toBe('Will call');
  });
});
