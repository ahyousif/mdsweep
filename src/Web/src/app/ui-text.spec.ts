import { uiText } from './ui-text';

describe('uiText', () => {
  it('uses the domain language for the manifest import workflow', () => {
    expect(uiText.uploadTitle).toBe('Upload MTM Manifest');
    expect(uiText.locallyOverridden).toBe('provider schedules protected');
  });
});
