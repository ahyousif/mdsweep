import { uiText } from './ui-text';

describe('uiText', () => {
  it('uses the domain language for the manifest import workflow', () => {
    expect(uiText.uploadTitle).toBe('Upload MTM Manifest');
    expect(uiText.uploadHelp).toContain('CSV or Excel');
    expect(uiText.chooseManifest).toBe('Choose Manifest');
    expect(uiText.locallyOverridden).toBe('provider schedules protected');
  });
});
