import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson, primeAuthToken } from './fixtures/mockApi';

test.describe('Informacja o... lookup', () => {
    test.beforeEach(async ({ page }) => {
        await primeAuthToken(page);
        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));
    });

    test('Towar shows quantities, keeps empty home slots, and reports carried stock separately', async ({ page }) => {
        await page.route(`${API_BASE}/Info/product/**`, (route) => fulfillJson(route, {
            sku: 'SKU-1', name: 'Widget',
            weightKg: 2.5, lengthCm: 30, widthCm: 20, heightCm: 10, sizeCategory: 'M',
            locations: [
                { locationBarcode: 'mp1000101a', locationType: 'Shelf', physicalQuantity: 10, reservedQuantity: 4, availableQuantity: 6 },
                { locationBarcode: 'mp1000202b', locationType: 'Shelf', physicalQuantity: 0, reservedQuantity: 0, availableQuantity: 0 },
            ],
            carriedByWorkersQuantity: 5,
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByPlaceholder('Scan or type a product SKU').fill('SKU-1');
        await page.getByRole('button', { name: 'Look up' }).click();

        await expect(page.getByRole('heading', { name: 'Widget' })).toBeVisible();
        await expect(page.getByText('30 × 20 × 10 cm')).toBeVisible();

        // Reserved units are called out, so nobody promises stock a pick task already holds.
        await expect(page.getByText('6 available')).toBeVisible();
        await expect(page.getByText('4 reserved')).toBeVisible();

        // A zero row is the SKU's home slot, not noise to be filtered away.
        await expect(page.getByText('empty — home slot')).toBeVisible();

        // Transit stock stays out of the addressable list but is still accounted for.
        await expect(page.getByText('currently carried by workers (in relocation) — no fixed location')).toBeVisible();
        await expect(page.getByText('TRANSIT-')).toHaveCount(0);
    });

    test('Pojemnik says contents are unavailable rather than showing an empty list', async ({ page }) => {
        await page.route(`${API_BASE}/Info/container/**`, (route) => fulfillJson(route, {
            barcode: 'CONT-1', type: 'Tote', status: 'InProgress',
            locationBarcode: 'HZA301', assignedSector: 'mp1',
            linkedTask: { kind: 'Picking', taskId: '11111111-2222-3333-4444-555555555555', status: 'InProgress', sector: 'mp1' },
            contentsAvailable: false,
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByRole('tab', { name: 'Pojemnik' }).click();
        await page.getByPlaceholder('Scan a container barcode').fill('CONT-1');
        await page.getByRole('button', { name: 'Look up' }).click();

        await expect(page.getByRole('heading', { name: 'CONT-1' })).toBeVisible();
        await expect(page.getByText('HZA301')).toBeVisible();
        await expect(page.getByText('Picking')).toBeVisible();

        // The distinction that matters: "we can't tell you" is not "it is empty".
        await expect(page.getByText('Not available yet — this screen cannot list what is inside a container.')).toBeVisible();
    });

    test('Miejsce shows the distinct-SKU count against the effective limit', async ({ page }) => {
        await page.route(`${API_BASE}/Info/location/**`, (route) => fulfillJson(route, {
            barcode: 'mp1000101a', type: 'Shelf', sector: 'p', zoneCode: 'mp1',
            items: [
                { productSku: 'SKU-1', productName: 'Widget', physicalQuantity: 7, reservedQuantity: 2, availableQuantity: 5 },
            ],
            distinctSkuCount: 2,
            maxDistinctSkus: 3,
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByRole('tab', { name: 'Miejsce' }).click();
        await page.getByPlaceholder('Scan a location barcode').fill('mp1000101a');
        await page.getByRole('button', { name: 'Look up' }).click();

        await expect(page.getByText('2 / 3')).toBeVisible();
        await expect(page.getByText('5 available')).toBeVisible();
    });

    test('switching mode clears the previous result', async ({ page }) => {
        await page.route(`${API_BASE}/Info/product/**`, (route) => fulfillJson(route, {
            sku: 'SKU-1', name: 'Widget',
            weightKg: 1, lengthCm: 1, widthCm: 1, heightCm: 1, sizeCategory: 'S',
            locations: [], carriedByWorkersQuantity: 0,
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByPlaceholder('Scan or type a product SKU').fill('SKU-1');
        await page.getByRole('button', { name: 'Look up' }).click();
        await expect(page.getByRole('heading', { name: 'Widget' })).toBeVisible();

        // Stale results from another mode would be actively misleading on a lookup screen.
        await page.getByRole('tab', { name: 'Miejsce' }).click();
        await expect(page.getByRole('heading', { name: 'Widget' })).toHaveCount(0);
    });
});
