import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson, primeAuthToken } from './fixtures/mockApi';

// A manual stock correction that would eat into stock already reserved for an
// allocated order must come back as a clean 409 asking for confirmation, never a
// raw crash or a silent reservation loss. This proves the admin screen surfaces
// that conflict and can actually get the correction submitted on the retry.
test.describe('Inventory admin: adjust stock', () => {
    const product = {
        id: 'product-1',
        name: 'Widget',
        sku: 'SKU-1',
        stocks: [{ productId: 'product-1', locationBarcode: 'LOC-1', quantity: 10 }],
    };

    // The Product select, the location/reason inputs, and the quantity-delta number
    // input have no id/htmlFor or unique placeholder of their own (the Create Product
    // form below reuses the same placeholders and also has several number inputs), so
    // every field here is scoped to the "Adjust Stock" section specifically.
    const adjustSection = (page: import('@playwright/test').Page) =>
        page.locator('section').filter({ hasText: 'Adjust Stock' });

    test('reservation-impact conflict shows a warning, and confirming resubmits successfully', async ({ page }) => {
        await primeAuthToken(page);

        await page.route(`${API_BASE}/Products`, (route) => fulfillJson(route, [product]));

        let confirmedCall: Record<string, unknown> | null = null;
        await page.route(`${API_BASE}/Inventory/adjust-stock`, async (route) => {
            const body = route.request().postDataJSON() as { confirmReservationImpact?: boolean };
            if (!body.confirmReservationImpact) {
                await fulfillJson(
                    route,
                    'This adjustment would take physical quantity to 4, below the 10 unit(s) already reserved here for allocated orders — 6 unit(s) of reservation would be lost, and the affected order(s) aren\'t tracked per stock row, so they can\'t be re-shortaged automatically. Investigate which order(s) this affects, then resubmit with confirmation if the correction should proceed.',
                    409,
                );
                return;
            }
            confirmedCall = body;
            await fulfillJson(route, { newPhysicalQuantity: 4, reservedQuantityReduced: 6 });
        });

        await page.goto('/admin/inventory');
        await expect(page.getByRole('heading', { name: 'Admin: Inventory' })).toBeVisible();

        const section = adjustSection(page);
        await section.locator('select').selectOption('product-1');
        await section.getByPlaceholder('e.g. mp101010101a').fill('LOC-1');
        await section.locator('input[type="number"]').fill('-6');
        await section.getByPlaceholder('e.g. cycle count correction').fill('cycle count found shrinkage');

        // First attempt: no confirmation sent, backend conflicts.
        await section.getByRole('button', { name: 'Apply Adjustment' }).click();
        await expect(page.getByText(/already reserved here for allocated orders/)).toBeVisible();
        await expect(page.getByRole('button', { name: 'Confirm and Apply Anyway' })).toBeVisible();

        // Confirm: resubmits the same values with confirmReservationImpact: true.
        await page.getByRole('button', { name: 'Confirm and Apply Anyway' }).click();
        await expect(page.getByText(/New physical quantity: 4/)).toBeVisible();
        await expect(page.getByText(/released 6 reserved unit\(s\)/)).toBeVisible();

        expect(confirmedCall).toEqual({
            productId: 'product-1',
            locationBarcode: 'LOC-1',
            quantityDelta: -6,
            reason: 'cycle count found shrinkage',
            confirmReservationImpact: true,
        });
    });

    test('editing a field after a conflict clears the confirmation prompt', async ({ page }) => {
        await primeAuthToken(page);

        await page.route(`${API_BASE}/Products`, (route) => fulfillJson(route, [product]));
        await page.route(`${API_BASE}/Inventory/adjust-stock`, (route) =>
            fulfillJson(route, 'Reservation impact.', 409));

        await page.goto('/admin/inventory');
        await expect(page.getByRole('heading', { name: 'Admin: Inventory' })).toBeVisible();

        const section = adjustSection(page);
        await section.locator('select').selectOption('product-1');
        await section.getByPlaceholder('e.g. mp101010101a').fill('LOC-1');
        await section.locator('input[type="number"]').fill('-6');
        await section.getByPlaceholder('e.g. cycle count correction').fill('cycle count');

        await section.getByRole('button', { name: 'Apply Adjustment' }).click();
        await expect(page.getByRole('button', { name: 'Confirm and Apply Anyway' })).toBeVisible();

        // Changing the delta after seeing the warning must not let a stale confirmation
        // silently apply to different numbers — the prompt should disappear.
        await section.locator('input[type="number"]').fill('-3');
        await expect(page.getByRole('button', { name: 'Confirm and Apply Anyway' })).not.toBeVisible();
    });
});
