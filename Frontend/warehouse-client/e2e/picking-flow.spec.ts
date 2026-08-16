import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson, nextDialogMessage, primeAuthToken } from './fixtures/mockApi';

test.describe('Picking flow', () => {
    test('complete a pick task end-to-end: scan container, pick the item, dispatch', async ({ page }) => {
        await primeAuthToken(page);

        const taskId = 'task-pick-1';
        const newTask = {
            id: taskId,
            sector: 'mp1',
            status: 'New',
            containerBarcode: null,
            items: [
                {
                    id: 'item-1',
                    productName: 'Widget',
                    productSku: 'SKU-1',
                    locationBarcode: 'mp1000101a',
                    requiredQuantity: 5,
                    pickedQuantity: 0,
                    missingQuantity: 0,
                    availableStock: 20,
                },
            ],
        };

        let started = false;
        let pickedQuantity = 0;
        let dispatched = false;

        // GetActiveForUserAsync only ever returns an InProgress task assigned to
        // this worker — an unclaimed "New" task is never "active" until started.
        await page.route(`${API_BASE}/PickTask/active**`, (route) => {
            if (!started || dispatched) return fulfillJson(route, null);
            return fulfillJson(route, {
                ...newTask,
                status: 'InProgress',
                containerBarcode: 'CONT-1',
                items: [{ ...newTask.items[0], pickedQuantity }],
            });
        });

        await page.route(`${API_BASE}/PickTask/next**`, (route) =>
            fulfillJson(route, started ? null : newTask));

        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));

        await page.route(`${API_BASE}/PickTask/*/start`, (route) => {
            started = true;
            return fulfillJson(route, {});
        });

        await page.route(`${API_BASE}/PickTask/*/pick`, (route) => {
            pickedQuantity = 5; // fully pick the only line in one scan
            return fulfillJson(route, {});
        });

        await page.route(`${API_BASE}/PickTask/*/dispatch`, (route) => {
            dispatched = true;
            return fulfillJson(route, { message: 'Container successfully verified and sent to the conveyor.' });
        });

        await page.goto('/');

        // MENU -> Start Picking -> no saved sector -> SectorSelect
        await page.getByRole('button', { name: 'Start Picking' }).click();
        await page.getByPlaceholder('Sector (e.g. mp1, mr1)').fill('mp1');
        await page.getByRole('button', { name: 'Confirm Sector' }).click();

        // NewTaskScreen: review the route, scan the container, start the task
        await expect(page.getByRole('heading', { name: /Picking Route/ })).toBeVisible();
        await expect(page.getByText('mp1000101a')).toBeVisible();
        await page.getByPlaceholder('Scan Container Barcode').fill('CONT-1');
        await page.getByRole('button', { name: 'Start Task' }).click();

        // ActiveTaskScreen step 1: location
        await expect(page.getByRole('heading', { name: /Task:/ })).toBeVisible();
        await page.getByPlaceholder('Location barcode...').fill('mp1000101a');
        await page.getByRole('button', { name: 'Check location' }).click();

        // step 2: SKU
        await page.getByPlaceholder('Product SKU...').fill('SKU-1');
        await page.getByRole('button', { name: 'Check SKU' }).click();

        // step 3: quantity is prefilled to the full remaining amount
        await expect(page.locator('input[type="number"]')).toHaveValue('5');
        await page.getByRole('button', { name: 'Into container' }).click();

        // Item now fully picked -> the screen auto-switches to container dispatch
        await expect(page.getByRole('heading', { name: 'Task complete!' })).toBeVisible();
        await page.getByPlaceholder(/Scan container CONT-1/).fill('CONT-1');
        await page.getByPlaceholder('2. CONVEYOR barcode...').fill('CONV-1');

        const dialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm dispatch' }).click();
        expect(await dialogMessage).toContain('successfully verified and sent to the conveyor');

        // Nothing left in the sector -> back to the empty state
        await expect(page.getByText('No tasks available in sector mp1')).toBeVisible();
    });
});
