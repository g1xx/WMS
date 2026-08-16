import type { Page, Route } from '@playwright/test';

// axiosClient.ts's baseURL is the relative path "/api", which the browser resolves
// against whatever origin the page itself loaded from — during these tests, that's
// always playwright.config.ts's baseURL (localhost:5173, the Vite dev server), not
// wherever the real backend happens to be running.
export const API_BASE = 'http://localhost:5173/api';

// Seeds localStorage with a session token before the app's first script runs,
// so ProtectedRoute treats the worker as already logged in without needing to
// drive the real Login form on every spec.
export async function primeAuthToken(page: Page, token = 'fake-jwt-token') {
    await page.addInitScript((t) => {
        window.localStorage.setItem('token', t);
    }, token);
}

export function fulfillJson(route: Route, data: unknown, status = 200) {
    return route.fulfill({
        status,
        contentType: 'application/json',
        body: JSON.stringify(data),
    });
}

// Some of this app's dialogs (window.confirm in handleLocationConfirm, the
// empty-badge window.alert) fire synchronously inside a click handler, before
// the triggering click() action has even resolved; others (mutation onError/
// onSuccess alerts) fire asynchronously, well after click() resolves. Sequencing
// "await click(), then await waitForEvent('dialog')" deadlocks the synchronous
// case — click() can't resolve while the native dialog blocks the page, and
// nothing accepts the dialog until click() returns. Registering the listener
// up front (before click() is even called) and letting it accept the dialog
// the instant it fires works for both cases, with no race either way.
export function nextDialogMessage(page: Page): Promise<string> {
    return new Promise((resolve) => {
        page.once('dialog', (dialog) => {
            resolve(dialog.message());
            void dialog.accept();
        });
    });
}
