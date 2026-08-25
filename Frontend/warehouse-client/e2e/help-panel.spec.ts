import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson } from './fixtures/mockApi';

// The panel exists for reviewers who arrive with no credentials, so the login screen is
// the case that matters most — it is the one screen they cannot get past without it.
const helpPayload = {
    logins: [
        { username: 'admin', password: 'AdminDemo123!', role: 'Admin', description: 'Full access.' },
        { username: 'erp-feed', password: 'IntegrationDemo123!', role: 'Integration', description: 'Inbound feed only.' },
    ],
    supervisorBadge: {
        barcode: '11111111-2222-3333-4444-555555555555',
        description: 'Paste this at the supervisor badge prompt.',
    },
    availableContainers: ['CONT-0001', 'CONT-0002'],
    conveyorBarcodes: ['HZA301', 'HZA302'],
    shelfLocations: ['mp1000101a'],
    walkthroughs: [{ title: 'Run a pick task end to end', steps: ['Log in as admin.', 'Scan a container.'] }],
};

test.describe('Demo help panel', () => {
    test('is reachable from the login screen and reveals credentials and live barcodes', async ({ page }) => {
        await page.route(`${API_BASE}/Demo/help`, (route) => fulfillJson(route, helpPayload));

        // No token primed on purpose — this is a reviewer arriving cold at the sign-in form.
        await page.goto('/login');

        const toggle = page.getByRole('button', { name: '? Demo help' });
        await expect(toggle).toBeVisible();

        // Collapsed by default: it must not cover the app until asked for.
        await expect(page.getByRole('button', { name: 'AdminDemo123!' })).toBeHidden();

        await toggle.click();

        await expect(page.getByRole('button', { name: 'admin', exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: 'AdminDemo123!' })).toBeVisible();
        await expect(page.getByRole('button', { name: 'erp-feed' })).toBeVisible();

        // The wall a reviewer hits mid-flow, and the reason the badge is served live.
        await expect(page.getByRole('button', { name: '11111111-2222-3333-4444-555555555555' })).toBeVisible();

        await expect(page.getByRole('button', { name: 'CONT-0001' })).toBeVisible();
        await expect(page.getByRole('button', { name: 'HZA302' })).toBeVisible();
        await expect(page.getByText('Run a pick task end to end')).toBeVisible();

        await page.getByRole('button', { name: '✕ Close' }).click();
        await expect(page.getByRole('button', { name: 'AdminDemo123!' })).toBeHidden();
    });

    test('renders nothing at all when the demo endpoint is disabled', async ({ page }) => {
        // What a real deployment sees: DemoSettings off, so /Demo/help 404s. The panel must
        // vanish entirely rather than leave a button that opens an error — this is the
        // difference between "no demo mode here" and "a broken feature shipped to prod".
        await page.route(`${API_BASE}/Demo/help`, (route) => route.fulfill({ status: 404, body: '' }));

        await page.goto('/login');

        await expect(page.getByRole('heading', { name: 'WMS Sign In' })).toBeVisible();
        await expect(page.getByRole('button', { name: '? Demo help' })).toHaveCount(0);
    });
});
