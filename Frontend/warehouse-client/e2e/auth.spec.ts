import { test, expect } from '@playwright/test';
import { API_BASE, fulfillJson, nextDialogMessage } from './fixtures/mockApi';

test.describe('Authentication', () => {
    test('unauthenticated user is redirected to /login', async ({ page }) => {
        await page.goto('/');

        await expect(page).toHaveURL(/\/login$/);
        await expect(page.getByRole('heading', { name: 'WMS Sign In' })).toBeVisible();
    });

    test('successful login stores a token and reaches the terminal menu', async ({ page }) => {
        await page.route(`${API_BASE}/Auth/login`, (route) => fulfillJson(route, { token: 'fake-jwt-token' }));
        await page.route(`${API_BASE}/PickTask/active**`, (route) => fulfillJson(route, null));
        await page.route(`${API_BASE}/PutawayTask/active**`, (route) => fulfillJson(route, null));

        await page.goto('/login');
        await page.getByPlaceholder('Enter username').fill('worker1');
        await page.getByPlaceholder('Enter password').fill('secret');
        await page.getByRole('button', { name: 'Sign in to warehouse' }).click();

        // Login navigates to a route ("/tasks") that doesn't exist, so the app's
        // catch-all redirects to "/" — Terminal then resumes into the menu.
        await expect(page.getByRole('button', { name: 'Start Picking' })).toBeVisible();
        expect(await page.evaluate(() => window.localStorage.getItem('token'))).toBe('fake-jwt-token');
    });

    test('failed login shows an alert and does not store a token', async ({ page }) => {
        await page.route(`${API_BASE}/Auth/login`, (route) =>
            fulfillJson(route, 'Invalid username or password.', 400));

        await page.goto('/login');
        await page.getByPlaceholder('Enter username').fill('worker1');
        await page.getByPlaceholder('Enter password').fill('wrong-password');

        const dialogMessage = nextDialogMessage(page);
        await page.getByRole('button', { name: 'Sign in to warehouse' }).click();
        expect(await dialogMessage).toContain('Login failed');

        expect(await page.evaluate(() => window.localStorage.getItem('token'))).toBeNull();
        await expect(page).toHaveURL(/\/login$/);
    });
});
