import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson, nextDialogMessage, primeAuthToken } from './fixtures/mockApi';

// Both specs below jump straight into an already-InProgress task via the
// worker's "resume on load" path (Terminal + PickTasks both call
// GET /PickTask/active on mount) instead of re-driving container scan/start —
// the goal here is the supervisor-override submenu itself, not task setup.
const activeTask = {
    id: 'task-1',
    sector: 'mp1',
    status: 'InProgress',
    containerBarcode: 'CONT-1',
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

test.describe('Supervisor override', () => {
    test('missing item: empty badge is rejected, a bad badge gets a 403 fallback alert, a good badge attaches the elevated token', async ({ page }) => {
        await primeAuthToken(page);

        let missingReported = false;
        let dispatched = false;

        await page.route(`${API_BASE}/PickTask/active**`, (route) => {
            if (dispatched) return fulfillJson(route, null);
            if (!missingReported) return fulfillJson(route, activeTask);
            // Writing off the only line off doesn't finish the task by itself — the
            // worker still has to physically dispatch the container, so the task
            // stays InProgress with no scannable item left (the screen should move
            // to dispatch mode on its own).
            return fulfillJson(route, {
                ...activeTask,
                items: [{ ...activeTask.items[0], missingQuantity: 5 }],
            });
        });
        await page.route(`${API_BASE}/PickTask/next**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));

        await page.goto('/');
        await expect(page.getByRole('heading', { name: /Task:/ })).toBeVisible();

        await page.getByRole('button', { name: 'ESC (Menu)' }).click();
        await page.getByRole('button', { name: /Item not found/ }).click();
        await expect(page.getByRole('heading', { name: 'Confirm shortage' })).toBeVisible();

        // 1. Empty badge never reaches the network — rejected client-side, submenu stays open.
        let dialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm' }).click();
        expect(await dialogMessage).toBe("Scan the supervisor's badge!");
        await expect(page.getByRole('heading', { name: 'Confirm shortage' })).toBeVisible();

        // 2. A scanned badge the backend rejects (403) must show the specific
        // supervisor-auth alert, not the generic failure message or a logout.
        await page.route(`${API_BASE}/Auth/supervisor-override`, (route) =>
            fulfillJson(route, { message: 'Invalid badge.' }, 403));

        await page.getByPlaceholder('Supervisor barcode...').fill('BADGE-BAD');
        dialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm' }).click();
        expect(await dialogMessage).toBe('Supervisor authorization failed: Invalid badge or missing permissions.');

        // The flow layer swallows the mutation error before it reaches the screen,
        // so the submenu closes either way, back to the normal active-task view.
        await expect(page.getByRole('heading', { name: /Task:/ })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Confirm shortage' })).not.toBeVisible();

        // 3. A badge the backend accepts: the elevated token from the override
        // exchange must be the one attached to the actual report-missing call.
        await page.unroute(`${API_BASE}/Auth/supervisor-override`);
        await page.route(`${API_BASE}/Auth/supervisor-override`, (route) =>
            fulfillJson(route, { token: 'elevated-jwt' }));

        let capturedAuthHeader: string | null = null;
        await page.route(`${API_BASE}/PickTask/*/report-missing`, async (route) => {
            missingReported = true;
            capturedAuthHeader = await route.request().headerValue('authorization');
            await fulfillJson(route, { message: 'Missing item reported. 5 unit(s) marked missing for this item.' });
        });

        await page.getByRole('button', { name: 'ESC (Menu)' }).click();
        await page.getByRole('button', { name: /Item not found/ }).click();
        await page.getByPlaceholder('Supervisor barcode...').fill('BADGE-GOOD');
        dialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm' }).click();
        expect(await dialogMessage).toContain('Missing item reported');
        expect(capturedAuthHeader).toBe('Bearer elevated-jwt');

        // The task isn't gone: every line is now accounted for (missing, not
        // picked), so the screen moves straight to dispatch — the worker still
        // has to physically send the container to the conveyor to finish.
        await expect(page.getByRole('heading', { name: 'Task complete!' })).toBeVisible();

        await page.route(`${API_BASE}/PickTask/*/dispatch`, (route) => {
            dispatched = true;
            return fulfillJson(route, { message: 'Container successfully verified and sent to the conveyor.' });
        });

        await page.getByPlaceholder(/Scan container CONT-1/).fill('CONT-1');
        await page.getByPlaceholder('2. CONVEYOR barcode...').fill('CONV-1');
        const dispatchDialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm dispatch' }).click();
        expect(await dispatchDialogMessage).toContain('successfully verified and sent to the conveyor');

        await expect(page.getByText('No tasks available in sector mp1')).toBeVisible();
    });

    test('defect report: a 403 fallback alerts, then a good badge attaches the elevated token', async ({ page }) => {
        await primeAuthToken(page);

        let reported = false;
        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, reported ? null : activeTask));
        await page.route(`${API_BASE}/PickTask/next**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));

        await page.goto('/');
        await expect(page.getByRole('heading', { name: /Task:/ })).toBeVisible();

        await page.getByRole('button', { name: 'ESC (Menu)' }).click();
        await page.getByRole('button', { name: /Defective \/ Damaged/ }).click();
        await expect(page.getByRole('heading', { name: 'Report defective stock' })).toBeVisible();

        await page.route(`${API_BASE}/Auth/supervisor-override`, (route) =>
            fulfillJson(route, { message: 'Invalid badge.' }, 403));

        await page.getByPlaceholder('Supervisor barcode...').fill('BADGE-BAD');
        let dialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm' }).click();
        expect(await dialogMessage).toBe('Supervisor authorization failed: Invalid badge or missing permissions.');
        await expect(page.getByRole('heading', { name: /Task:/ })).toBeVisible();

        await page.unroute(`${API_BASE}/Auth/supervisor-override`);
        await page.route(`${API_BASE}/Auth/supervisor-override`, (route) =>
            fulfillJson(route, { token: 'elevated-jwt' }));

        let capturedAuthHeader: string | null = null;
        await page.route(`${API_BASE}/PickTask/*/report-defect`, async (route) => {
            reported = true;
            capturedAuthHeader = await route.request().headerValue('authorization');
            await fulfillJson(route, { message: '5 defective unit(s) written off.' });
        });

        await page.getByRole('button', { name: 'ESC (Menu)' }).click();
        await page.getByRole('button', { name: /Defective \/ Damaged/ }).click();
        await page.getByPlaceholder('Supervisor barcode...').fill('BADGE-GOOD');
        dialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Confirm' }).click();
        expect(await dialogMessage).toContain('defective unit');

        await expect(page.getByText('No tasks available in sector mp1')).toBeVisible();
        expect(capturedAuthHeader).toBe('Bearer elevated-jwt');
    });
});
