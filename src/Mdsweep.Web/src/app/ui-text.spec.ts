import { uiText } from './ui-text';

describe('uiText', () => {
  it('uses plain language for the trip import workflow', () => {
    expect(uiText.uploadTitle).toBe('Import Trips');
    expect(uiText.uploadHelp).toContain('trip file');
    expect(uiText.chooseManifest).toBe('Choose File');
    expect(uiText.locallyOverridden).toBe('provider schedules protected');
  });
});
