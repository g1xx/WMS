# Bug Report Template (Jira style)

Copy the template below into a new issue. One defect per report.

```
Summary:            [Area] Short description of the problem (what is wrong, where)

Environment:        Build/commit: <COMMIT_SHA>
                    Frontend URL: <FRONTEND_URL>   API: <API_BASE_URL>
                    Browser/OS:   <BROWSER VERSION> / <OS>
                    Test account role: <Worker | Brigadier | Admin | Integration>

Preconditions:      State required before step 1 (data, logged-in user, sector, task status...)

Steps to reproduce: 1.
                    2.
                    3.

Expected result:    What should happen (reference: test case ID / requirement / code behaviour)

Actual result:      What actually happens (exact message text, status code, UI state)

Severity:           Blocker | Critical | Major | Minor | Trivial
Priority:           Highest | High | Medium | Low

Attachments:        Screenshot / screen recording, HAR or Postman request+response, console log

Linked test case:   TC-xx
Reproducibility:    Always | Intermittent (x of y) | Once
```

Severity guide
- **Blocker** – main flow cannot be executed at all (e.g. nobody can log in, picking cannot start).
- **Critical** – wrong stock/order data or a security gap (e.g. wrong role can change stock).
- **Major** – feature works incorrectly but a workaround exists.
- **Minor** – cosmetic/UX issue with no data impact, unclear message.
- **Trivial** – typo, alignment.

---

## EXAMPLE – format only, not a real defect

> This example only shows how a filled report should look. It does **not** describe an observed defect; the scenario is hypothetical.

| Field | Value |
|---|---|
| Summary | [Picking] Over-pick error message shows wrong remaining quantity after partial pick |
| Environment | Build `<COMMIT_SHA>`; Frontend `http://localhost`; API `http://localhost:5124`; Chrome 1xx / Windows 11; role: Worker |
| Preconditions | Worker `<WORKER_USER>` logged in, sector `mp9`; pick task InProgress with one line requiring 2 units of `<SKU>` at `<SHELF>`; 1 unit already picked |
| Steps to reproduce | 1. On the item screen scan location `<SHELF>` → "Check location"<br>2. Scan `<SKU>` → "Check SKU"<br>3. Enter quantity 2 and confirm |
| Expected result | Request rejected with 400 and alert "Over-pick! You only need to pick 1 more units of this item." (TC-20, row e) |
| Actual result | *(hypothetical)* Alert shows "Over-pick! You only need to pick 2 more units of this item." |
| Severity | Minor |
| Priority | Medium |
| Attachments | `overpick_alert.png`, `pick_request.har` (hypothetical file names) |
| Linked test case | TC-20 |
| Reproducibility | Always (3 of 3) |
