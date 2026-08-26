import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson, primeAuthToken } from './fixtures/mockApi';

// Relocation as two ordinary stock movements through a per-worker transit location.
// These mocks model the server contract: /take moves shelf -> transit, /putaway moves
// transit -> shelf, and /state reports what's carried plus whether exit is allowed.
test.describe('Relocation flow', () => {
    test('take from a shelf, split across two targets, then exit once empty', async ({ page }) => {
        await primeAuthToken(page);

        let carried: { productSku: string; productName: string; physicalQuantity: number; reservedQuantity: number; availableQuantity: number }[] = [];
        const state = () => ({
            transitBarcode: 'TRANSIT-admin',
            carriedItems: carried,
            canExit: carried.length === 0,
        });

        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/Relocation/state**`, (route) => fulfillJson(route, state()));

        await page.route(`${API_BASE}/Relocation/location/**`, (route) => fulfillJson(route, {
            locationBarcode: 'mp1000101a',
            items: [{
                productSku: 'SKU-1', productName: 'Widget',
                // 10 on the shelf, 4 reserved for a pick task -> only 6 may be relocated.
                physicalQuantity: 10, reservedQuantity: 4, availableQuantity: 6,
            }],
        }));

        await page.route(`${API_BASE}/Relocation/take`, async (route) => {
            const body = route.request().postDataJSON();
            carried = [{
                productSku: body.productSku, productName: 'Widget',
                physicalQuantity: body.quantity, reservedQuantity: 0, availableQuantity: body.quantity,
            }];
            return fulfillJson(route, state());
        });

        await page.route(`${API_BASE}/Relocation/putaway`, async (route) => {
            const body = route.request().postDataJSON();
            const left = carried[0].availableQuantity - body.quantity;
            carried = left > 0
                ? [{ ...carried[0], physicalQuantity: left, availableQuantity: left }]
                : [];
            return fulfillJson(route, state());
        });

        await page.goto('/');
        await page.getByRole('button', { name: 'Relokacja' }).click();

        // Source scan -> the shelf's contents, with the reserved units called out.
        await page.getByPlaceholder('Scan source location').fill('mp1000101a');
        await page.getByRole('button', { name: 'Check location' }).click();
        await expect(page.getByText('6 movable · 4 reserved for picking')).toBeVisible();

        // Quantity defaults to what may actually move (6), not the physical 10.
        await page.getByRole('button', { name: /SKU-1/ }).click();
        await expect(page.locator('input[type="number"]')).toHaveValue('6');
        await page.getByRole('button', { name: 'Confirm' }).click();

        // "6 pcs." only appears in the carried summary; "SKU-1" alone would also match the
        // shelf listing button behind it.
        await expect(page.getByText('6 pcs.')).toBeVisible();

        // Esc menu -> start putting away.
        await page.keyboard.press('Escape');
        await page.getByRole('button', { name: /Start putting away/ }).click();

        // First target takes 4 of 6; the remaining 2 stay carried and the flow asks again
        // for the SAME sku rather than moving on.
        await page.getByPlaceholder('Scan target location').fill('mp1000202b');
        await page.getByRole('button', { name: 'Next' }).click();
        await expect(page.locator('input[type="number"]')).toHaveValue('6');
        await page.locator('input[type="number"]').fill('4');
        await page.getByRole('button', { name: 'Confirm' }).click();

        await expect(page.getByText('SKU-1 (2 left)')).toBeVisible();

        // Second target takes the remaining 2 -> relocation complete.
        page.once('dialog', (dialog) => {
            expect(dialog.message()).toContain('Relocation complete');
            void dialog.accept();
        });
        await page.getByPlaceholder('Scan target location').fill('mp1000303c');
        await page.getByRole('button', { name: 'Next' }).click();
        await page.getByRole('button', { name: 'Confirm' }).click();

        // Wait for the completion alert to be handled and the flow to reset before
        // pressing Escape — a keypress sent while the modal is still up is swallowed by
        // the dialog rather than reaching the window listener.
        await expect(page.getByText('nothing')).toBeVisible();

        // Exit is only offered now that nothing is carried.
        await page.keyboard.press('Escape');
        await expect(page.getByRole('button', { name: 'Exit relocation' })).toBeVisible();
        await page.getByRole('button', { name: 'Exit relocation' }).click();
        await expect(page.getByRole('button', { name: 'Start Picking' })).toBeVisible();
    });

    test('exit is withheld while stock is still carried', async ({ page }) => {
        await primeAuthToken(page);

        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/Relocation/state**`, (route) => fulfillJson(route, {
            transitBarcode: 'TRANSIT-admin',
            carriedItems: [{
                productSku: 'SKU-9', productName: 'Leftover',
                physicalQuantity: 3, reservedQuantity: 0, availableQuantity: 3,
            }],
            canExit: false,
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Relokacja' }).click();
        await page.keyboard.press('Escape');

        // A worker must not walk away holding stock, and the menu says why rather than
        // showing a dead button.
        await expect(page.getByRole('button', { name: 'Exit relocation' })).toHaveCount(0);
        await expect(page.getByText('You are carrying stock — put it away before leaving.')).toBeVisible();
    });
});
