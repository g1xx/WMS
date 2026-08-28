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

    test('Pojemnik distinguishes "unknown" from "empty"', async ({ page }) => {
        await page.route(`${API_BASE}/Info/container/**`, (route) => fulfillJson(route, {
            barcode: 'CONT-1', type: 'Tote', status: 'Ready',
            locationBarcode: 'HZA301', assignedSector: null,
            linkedTasks: [],
            contentSections: [{ kind: 'Unknown', lines: [], sourceTaskId: null, sector: null, isHistorical: false }],
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByRole('tab', { name: 'Pojemnik' }).click();
        await page.getByPlaceholder('Scan a container barcode').fill('CONT-1');
        await page.getByRole('button', { name: 'Look up' }).click();

        await expect(page.getByRole('heading', { name: 'CONT-1' })).toBeVisible();
        await expect(page.getByText('HZA301')).toBeVisible();
        await expect(page.getByText('Not held by any task.')).toBeVisible();

        // The distinction that matters: "nothing was recorded" is not "there is nothing in it".
        await expect(page.getByText('Contents unknown')).toBeVisible();
        await expect(page.getByText('That is not the same as it being empty.')).toBeVisible();
    });

    test('a dispatched container shows its picked lines as history, with the caveat', async ({ page }) => {
        // The HSOD00015 case: Ready, held by no task, pick task completed at dispatch.
        await page.route(`${API_BASE}/Info/container/**`, (route) => fulfillJson(route, {
            barcode: 'HSOD00015', type: 'Tote', status: 'Ready',
            locationBarcode: 'HZA301', assignedSector: null,
            linkedTasks: [],
            contentSections: [{
                kind: 'AsDispatched',
                lines: [{ productSku: 'SKU-1', productName: 'Widget', quantity: 6 }],
                sourceTaskId: '11111111-2222-3333-4444-555555555555',
                sector: 'mp1',
                isHistorical: true,
            }],
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByRole('tab', { name: 'Pojemnik' }).click();
        await page.getByPlaceholder('Scan a container barcode').fill('HSOD00015');
        await page.getByRole('button', { name: 'Look up' }).click();

        await expect(page.getByText('As dispatched')).toBeVisible();
        await expect(page.getByText('SKU-1')).toBeVisible();

        // The qualifier is load-bearing — it must be on screen, not just in the wording of
        // the heading, and it must name the way this goes stale.
        await expect(page.getByText(/not verified since/)).toBeVisible();
        await expect(page.getByText(/keeps saying the same thing/)).toBeVisible();
    });

    test('picked then partly put away shows two labelled facts, never a subtracted number', async ({ page }) => {
        await page.route(`${API_BASE}/Info/container/**`, (route) => fulfillJson(route, {
            barcode: 'HSOD00015', type: 'Tote', status: 'InProgress',
            locationBarcode: 'HZA301', assignedSector: 'mp1',
            linkedTasks: [
                { kind: 'Putaway', taskId: 'aaaaaaaa-0000-0000-0000-000000000001', status: 'InProgress', sector: 'mp1' },
            ],
            contentSections: [
                {
                    kind: 'ToBePutAway',
                    lines: [{ productSku: 'SKU-2', productName: 'Gadget', quantity: 6 }],
                    sourceTaskId: 'aaaaaaaa-0000-0000-0000-000000000001', sector: 'mp1', isHistorical: false,
                },
                {
                    kind: 'AsDispatched',
                    lines: [{ productSku: 'SKU-1', productName: 'Widget', quantity: 6 }],
                    sourceTaskId: 'bbbbbbbb-0000-0000-0000-000000000002', sector: 'mp1', isHistorical: true,
                },
            ],
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByRole('tab', { name: 'Pojemnik' }).click();
        await page.getByPlaceholder('Scan a container barcode').fill('HSOD00015');
        await page.getByRole('button', { name: 'Look up' }).click();

        // Both stand on their own. Subtracting them would invent a number, because the
        // putaway's expected quantity isn't derived from what was picked.
        await expect(page.getByText('To be put away')).toBeVisible();
        await expect(page.getByText('As dispatched')).toBeVisible();
        await expect(page.getByText('SKU-1')).toBeVisible();
        await expect(page.getByText('SKU-2')).toBeVisible();
    });

    test('lists every pending putaway task rather than picking one', async ({ page }) => {
        await page.route(`${API_BASE}/Info/container/**`, (route) => fulfillJson(route, {
            barcode: 'CONT-9', type: 'Tote', status: 'Ready',
            locationBarcode: null, assignedSector: null,
            linkedTasks: [
                { kind: 'Putaway', taskId: 'aaaaaaaa-0000-0000-0000-000000000001', status: 'New', sector: 'mp1' },
                { kind: 'Putaway', taskId: 'bbbbbbbb-0000-0000-0000-000000000002', status: 'New', sector: 'mr1' },
            ],
            contentSections: [{ kind: 'Unknown', lines: [], sourceTaskId: null, sector: null, isHistorical: false }],
        }));

        await page.goto('/');
        await page.getByRole('button', { name: 'Informacja o...' }).click();
        await page.getByRole('tab', { name: 'Pojemnik' }).click();
        await page.getByPlaceholder('Scan a container barcode').fill('CONT-9');
        await page.getByRole('button', { name: 'Look up' }).click();

        // A container can have one putaway task per zone; showing "the" task would pick
        // arbitrarily among them and present that choice as fact.
        await expect(page.getByText('Linked tasks (2)')).toBeVisible();
        await expect(page.getByText('mp1')).toBeVisible();
        await expect(page.getByText('mr1')).toBeVisible();
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
