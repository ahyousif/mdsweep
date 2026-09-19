import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '@app/core/api/api-client';

export type VehicleDetails = { displayLabel: string; vin: string; year?: number | null; make?: string | null; model?: string | null };
export type Vehicle = VehicleDetails & { id: string; isActive: boolean };

@Service()
export class VehiclesApi {
  readonly #api = inject(ApiClient);
  list(): Promise<Vehicle[]> {
    return firstValueFrom(this.#api.http.get<Vehicle[]>(this.#api.url('vehicles')));
  }
  create(details: VehicleDetails): Promise<Vehicle> {
    return firstValueFrom(this.#api.http.post<Vehicle>(this.#api.url('vehicles'), details));
  }
  update(id: string, details: VehicleDetails): Promise<void> {
    return firstValueFrom(this.#api.http.put<void>(this.#api.url(`vehicles/${id}`), details));
  }
  setActive(id: string, isActive: boolean): Promise<void> {
    return firstValueFrom(
      this.#api.http.put<void>(this.#api.url(`vehicles/${id}/active`), { isActive }),
    );
  }
}
