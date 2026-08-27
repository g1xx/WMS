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
                    suggestedLocations: [
                        { locationBarcode: 'mp1000101a', currentQuantity: 3, isInCurrentSector: true, distinctSkuCount: 1, maxDistinctSkus: 3 },
                    ],
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

        // STEP 1: location — scanning an address NOT in suggestedLocations
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
                    suggestedLocations: [
                        { locationBarcode: 'mp1000101a', currentQuantity: 3, isInCurrentSector: true, distinctSkuCount: 1, maxDistinctSkus: 3 },
                    ],
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

    test('relocation from the putaway Esc menu returns to the same item and step', async ({ page }) => {
        await primeAuthToken(page);

        const putawayTask = {
            id: 'putaway-2',
            containerBarcode: 'TOTE-2',
            sector: 'mp1',
            status: 'InProgress',
            items: [
                {
                    id: 'pitem-1', productName: 'Widget', productSku: 'SKU-1',
                    expectedQuantity: 5, putAwayQuantity: 0, missingQuantity: 0,
                    suggestedLocations: [
                        { locationBarcode: 'mp1000101a', currentQuantity: 3, isInCurrentSector: true, distinctSkuCount: 1, maxDistinctSkus: 3 },
                    ],
                },
            ],
        };

        let carried: { productSku: string; productName: string; physicalQuantity: number; reservedQuantity: number; availableQuantity: number }[] = [];
        const relocationState = () => ({
            transitBarcode: 'TRANSIT-admin', carriedItems: carried, canExit: carried.length === 0,
        });

        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/validate-container`, (route) =>
            fulfillJson(route, { isValid: true, containerSector: 'mp1' }));
        await page.route(`${API_BASE}/PutawayTask/start`, (route) => fulfillJson(route, putawayTask));
        await page.route(`${API_BASE}/Relocation/state**`, (route) => fulfillJson(route, relocationState()));
        await page.route(`${API_BASE}/Relocation/location/**`, (route) => fulfillJson(route, {
            locationBarcode: 'mp1000505e',
            items: [{ productSku: 'SKU-9', productName: 'Other', physicalQuantity: 4, reservedQuantity: 0, availableQuantity: 4 }],
        }));
        await page.route(`${API_BASE}/Relocation/take`, (route) => {
            const body = route.request().postDataJSON();
            carried = [{ productSku: body.productSku, productName: 'Other', physicalQuantity: body.quantity, reservedQuantity: 0, availableQuantity: body.quantity }];
            return fulfillJson(route, relocationState());
        });
        await page.route(`${API_BASE}/Relocation/putaway`, (route) => {
            carried = [];
            return fulfillJson(route, relocationState());
        });

        await page.goto('/');
        await page.getByRole('button', { name: 'Start Putaway' }).click();
        await page.getByPlaceholder('Sector (e.g. mp1, mr1)').fill('mp1');
        await page.getByRole('button', { name: 'Confirm Sector' }).click();
        await page.getByPlaceholder('Scan Container Barcode').fill('TOTE-2');
        await page.getByRole('button', { name: 'Scan Container' }).click();

        // Advance INTO step 2: lock in a suggested location so no confirm dialog fires.
        await page.getByPlaceholder('Location barcode...').fill('mp1000101a');
        await page.getByRole('button', { name: 'Confirm location' }).click();
        await expect(page.getByPlaceholder('Product SKU...')).toBeVisible();
        // Half-entered work that must survive the round trip.
        await page.getByPlaceholder('Product SKU...').fill('SKU-1');

        // Esc menu -> Relokacja
        await page.keyboard.press('Escape');
        await page.getByRole('button', { name: '📦 Relokacja' }).click();
        await expect(page.getByRole('heading', { name: 'Relokacja' })).toBeVisible();

        // Take stock, then try to go back while still carrying it.
        await page.getByPlaceholder('Scan source location').fill('mp1000505e');
        await page.getByRole('button', { name: 'Check location' }).click();
        await page.getByRole('button', { name: /SKU-9/ }).click();
        await page.getByRole('button', { name: 'Confirm' }).click();
        await expect(page.getByText('4 pcs.')).toBeVisible();

        await page.keyboard.press('Escape');
        // Blocked: returning to putaway carrying stock would let the worker finish the
        // task and leave by putaway's own exit, stranding it.
        await expect(page.getByRole('button', { name: 'Back to putaway' })).toHaveCount(0);
        await expect(page.getByText('You are carrying stock — put it away before leaving.')).toBeVisible();

        // Put it away, then return.
        await page.getByRole('button', { name: /Start putting away/ }).click();
        await page.getByPlaceholder('Scan target location').fill('mp1000606f');
        await page.getByRole('button', { name: 'Next' }).click();
        page.once('dialog', (dialog) => void dialog.accept());
        await page.getByRole('button', { name: 'Confirm' }).click();
        await expect(page.getByText('nothing')).toBeVisible();

        await page.keyboard.press('Escape');
        await page.getByRole('button', { name: 'Back to putaway' }).click();

        // Back at the SAME item and the SAME step — location still locked in and the
        // half-typed SKU still there, not reset to the location prompt.
        await expect(page.getByRole('heading', { name: 'Container: TOTE-2' })).toBeVisible();
        await expect(page.getByPlaceholder('Product SKU...')).toHaveValue('SKU-1');
        // The locked-in location specifically, not the same barcode in the suggestions list.
        await expect(page.getByRole('strong').filter({ hasText: 'mp1000101a' })).toBeVisible();
        await expect(page.getByPlaceholder('Location barcode...')).toHaveCount(0);
    });
});
