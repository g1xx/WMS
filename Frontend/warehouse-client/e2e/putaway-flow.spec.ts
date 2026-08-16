import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson, nextDialogMessage, primeAuthToken } from './fixtures/mockApi';

test.describe('Putaway flow', () => {
    test('strict 2-step flow: non-suggested location warns, wrong SKU is rejected, correct scan completes the task', async ({ page }) => {
        await primeAuthToken(page);

        const putawayTask = {
            id: 'putaway-1',
            containerBarcode: 'TOTE-1',
            sector: 'mp1',
            status: 'InProgress',
            items: [
                {
                    id: 'pitem-1',
                    productName: 'Widget',
                    productSku: 'SKU-1',
                    expectedQuantity: 5,
                    putAwayQuantity: 0,
                    missingQuantity: 0,
                    // Deliberately does NOT include the location the worker scans
                    // below, to exercise the soft-validation confirm dialog.
                    suggestedLocationBarcodes: ['mp1000101a'],
                },
            ],
        };

        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/validate-container`, (route) =>
            fulfillJson(route, { isValid: true, containerSector: 'mp1' }));
        await page.route(`${API_BASE}/PutawayTask/start`, (route) => fulfillJson(route, putawayTask));

        await page.route(`${API_BASE}/PutawayTask/*/confirm-item`, (route) =>
            fulfillJson(route, {
                ...putawayTask,
                status: 'Completed',
                items: [{ ...putawayTask.items[0], putAwayQuantity: 5 }],
            }));

        await page.goto('/');

        // MENU -> Start Putaway -> no saved sector -> SectorSelect
        await page.getByRole('button', { name: 'Start Putaway' }).click();
        await page.getByPlaceholder('Sector (e.g. mp1, mr1)').fill('mp1');
        await page.getByRole('button', { name: 'Confirm Sector' }).click();

        // Container scan
        await page.getByPlaceholder('Scan Container Barcode').fill('TOTE-1');
        await page.getByRole('button', { name: 'Scan Container' }).click();

        // STEP 1: location — scanning an address NOT in suggestedLocationBarcodes
        // must warn before locking it in.
        await expect(page.getByRole('heading', { name: /Container:/ })).toBeVisible();
        await page.getByPlaceholder('Location barcode...').fill('mp1999901a');

        const locationDialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm location' }).click();
        expect(await locationDialogMessage).toContain('рекомендованных');

        // STEP 2: product + quantity, now shown together
        await expect(page.getByText('mp1999901a')).toBeVisible();
        await expect(page.locator('input[type="number"]')).toHaveValue('5');

        // Wrong SKU is rejected client-side, before any network call.
        await page.getByPlaceholder('Product SKU...').fill('WRONG-SKU');
        await page.getByRole('button', { name: 'Confirm putaway' }).click();
        await expect(page.getByText('Wrong item! Expected: SKU-1')).toBeVisible();

        // Correct SKU completes the (only) line, finishing the task.
        await page.getByPlaceholder('Product SKU...').fill('SKU-1');
        await page.getByRole('button', { name: 'Confirm putaway' }).click();

        await expect(page.getByText('Putaway of container TOTE-1 finished.')).toBeVisible();
        await page.getByRole('button', { name: 'Return to Main Menu' }).click();
        await expect(page.getByRole('button', { name: 'Start Picking' })).toBeVisible();
    });

    test('report-missing during putaway: 403 fallback alerts, then a good badge attaches the elevated token', async ({ page }) => {
        await primeAuthToken(page);

        const putawayTask = {
            id: 'putaway-2',
            containerBarcode: 'TOTE-2',
            sector: 'mp1',
            status: 'InProgress',
            items: [
                {
                    id: 'pitem-1',
                    productName: 'Widget',
                    productSku: 'SKU-1',
                    expectedQuantity: 5,
                    putAwayQuantity: 0,
                    missingQuantity: 0,
                    suggestedLocationBarcodes: ['mp1000101a'],
                },
            ],
        };

        let reported = false;
        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, reported ? null : putawayTask));

        // Jump straight into the in-progress task via resume-on-load, same as the
        // picking supervisor-override spec — this test is about the override
        // submenu, not container scan/start.
        await page.goto('/');
        await expect(page.getByRole('heading', { name: /Container:/ })).toBeVisible();

        await page.getByRole('button', { name: /Report Missing/ }).click();
        await expect(page.getByPlaceholder('Supervisor barcode...')).toBeVisible();

        await page.route(`${API_BASE}/Auth/supervisor-override`, (route) =>
            fulfillJson(route, { message: 'Invalid badge.' }, 403));

        await page.getByPlaceholder('Supervisor barcode...').fill('BADGE-BAD');
        const badgeDialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm' }).click();
        expect(await badgeDialogMessage).toBe('Supervisor authorization failed: Invalid badge or missing permissions.');

        // Submenu closes regardless of outcome (the flow layer swallows the
        // mutation error) — back to the location step.
        await expect(page.getByPlaceholder('Location barcode...')).toBeVisible();

        await page.unroute(`${API_BASE}/Auth/supervisor-override`);
        await page.route(`${API_BASE}/Auth/supervisor-override`, (route) =>
            fulfillJson(route, { token: 'elevated-jwt' }));

        let capturedAuthHeader: string | null = null;
        await page.route(`${API_BASE}/PutawayTask/*/report-missing`, async (route) => {
            reported = true;
            capturedAuthHeader = await route.request().headerValue('authorization');
            await fulfillJson(route, {
                ...putawayTask,
                status: 'Completed',
                items: [{ ...putawayTask.items[0], missingQuantity: 5 }],
            });
        });

        await page.getByRole('button', { name: /Report Missing/ }).click();
        await page.getByPlaceholder('Supervisor barcode...').fill('BADGE-GOOD');
        await page.getByRole('button', { name: 'Confirm' }).click();

        await expect(page.getByText('Putaway of container TOTE-2 finished.')).toBeVisible();
        expect(capturedAuthHeader).toBe('Bearer elevated-jwt');
    });
});
