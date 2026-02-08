# Invoice APIs – Usage Guide

Base URL: `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice`

All endpoints require **Authorization: Bearer {token}**.

All responses use this envelope:

| Field       | Type    | Description                          |
|------------|---------|--------------------------------------|
| status     | boolean | `true` = success, `false` = error   |
| statusCode | number  | HTTP status (200, 400, 404, 422, 500) |
| data       | object  | Payload on success; `null` on error  |
| message    | string  | Human-readable message               |
| errorNumber| string? | Optional error code                  |
| timestamp  | string  | ISO date-time                        |

---

## 1. Generate fixed-amount invoices

**Method:** `POST`  
**URL:** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/generate-fixed/{contractId}`

### Purpose

Generates invoices for a **fixed amount** contract (FeeTypeId = 1). Each invoice has the same amount as the contract’s `FeeAmount`. If the contract already has some invoices, only the **missing** ones are created (e.g. 8 exist, 12 expected → 4 new invoices).

### Parameters

| Parameter   | Location | Type | Required | Description        |
|------------|----------|------|----------|--------------------|
| contractId | URL path | int  | Yes      | ID of the contract |

No request body.

### Validations

- `contractId` must be &gt; 0.
- Contract must exist.
- Contract must be **fixed amount** (FeeTypeId = 1). If FeeTypeId ≠ 1, the API returns an error.
- Contract must have: StartDate, EndDate, PaymentFrequency, CurrencyCode.
- Contract must have FeeAmount &gt; 0.
- Maximum number of invoices is derived from StartDate, EndDate and PaymentFrequency. If the contract already has that many invoices (e.g. 12/12), no new invoices are created and the response indicates that.

### Success response (200)

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Successfully generated 4 invoice(s) for contract CNT-2026-001. Contract now has 12/12 invoices",
    "invoicesGenerated": 4,
    "existingInvoicesCount": 8,
    "expectedInvoicesCount": 12,
    "totalAmount": 12000.00,
    "invoiceIds": [101, 102, 103, 104]
  },
  "message": "Successfully generated 4 invoice(s) for contract CNT-2026-001. Contract now has 12/12 invoices",
  "errorNumber": null,
  "timestamp": "2026-02-07T18:00:00"
}
```

| data field             | Type    | Description                                      |
|------------------------|---------|--------------------------------------------------|
| success                | boolean | Always true in 200 response                      |
| message                | string  | Summary message                                  |
| invoicesGenerated      | int     | Number of invoices created in this call         |
| existingInvoicesCount  | int     | Count before this call                          |
| expectedInvoicesCount  | int     | Total invoices allowed for the contract         |
| totalAmount            | number  | Sum of amounts of the newly created invoices    |
| invoiceIds             | int[]   | IDs of the newly created invoices               |

### When maximum already reached (400)

If the contract already has the maximum number of invoices (e.g. 12/12), the API returns 400 and no invoices are created:

```json
{
  "status": false,
  "statusCode": 422,
  "data": {
    "success": false,
    "message": "Cannot generate more invoices. The contract already has the maximum number of invoices (12/12)",
    "invoicesGenerated": 0,
    "existingInvoicesCount": 12,
    "expectedInvoicesCount": 12,
    "invoiceIds": []
  },
  "message": "Cannot generate more invoices. The contract already has the maximum number of invoices (12/12)",
  "errorNumber": null,
  "timestamp": "2026-02-07T18:00:00"
}
```

### Error responses

- **404** – Contract not found. `data` is null, `message` describes the error.
- **400** – Validation error (e.g. invalid contractId). `message` contains validation details.
- **422** – Business rule (e.g. contract is not fixed amount, or missing FeeAmount/required fields). `message` explains the reason.
- **500** – Server/database error. `message` contains the error description.

---

## 2. Create manual invoice (percentage contracts)

**Method:** `POST`  
**URL:** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/create-manual`

### Purpose

Creates **one** invoice manually for a **percentage** contract (FeeTypeId = 2). The user supplies dates, amount and optional description/notes. The contract’s maximum invoice count is enforced; once reached (e.g. 12/12), no more manual invoices can be created.

### Request body (JSON)

| Field       | Type    | Required | Description                                  |
|------------|---------|----------|----------------------------------------------|
| contractId | integer | Yes      | ID of the contract (must be &gt; 0)         |
| invoiceDate| string  | Yes      | Invoice date (ISO date, e.g. "2026-02-07")  |
| dueDate    | string  | Yes      | Due date (ISO date); must be ≥ invoiceDate  |
| amount     | number  | Yes      | Invoice amount (≥ 0)                        |
| description| string  | No       | Line description (max 500 characters)        |
| notes      | string  | No       | Notes (max 1000 characters)                  |

Example:

```json
{
  "contractId": 5,
  "invoiceDate": "2026-02-07",
  "dueDate": "2026-03-07",
  "amount": 2500.00,
  "description": "Services for January 2026",
  "notes": "Optional notes"
}
```

### Validations

- **Request:** contractId &gt; 0; invoiceDate and dueDate required; dueDate ≥ invoiceDate; amount ≥ 0; description ≤ 500 characters; notes ≤ 1000 characters.
- **Contract:** Must exist and must be **percentage** (FeeTypeId = 2). Otherwise an error is returned.
- **Contract data:** Must have StartDate, EndDate, PaymentFrequency, CurrencyCode.
- **Count:** Current invoice count must be less than the expected maximum for the contract. If already at maximum (e.g. 12/12), the API returns an error stating that no more invoices can be added.

### Success response (200)

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Invoice 'INV-2026-015' created successfully for contract CNT-2026-002. Contract now has 9/12 invoices",
    "invoiceId": 105,
    "invoiceNumber": "INV-2026-015",
    "currentInvoiceCount": 9,
    "expectedInvoiceCount": 12
  },
  "message": "Invoice 'INV-2026-015' created successfully for contract CNT-2026-002. Contract now has 9/12 invoices",
  "errorNumber": null,
  "timestamp": "2026-02-07T18:00:00"
}
```

| data field            | Type   | Description                           |
|-----------------------|--------|---------------------------------------|
| success               | boolean| Always true                           |
| message               | string | Summary message                       |
| invoiceId             | int    | ID of the created invoice            |
| invoiceNumber         | string | Assigned number (e.g. INV-2026-015)  |
| currentInvoiceCount   | int    | Total invoices for contract after create |
| expectedInvoiceCount  | int    | Maximum invoices for the contract    |

### Error responses

- **404** – Contract not found.
- **400** – Validation failed (invalid body or rules above). `message` lists the validation errors.
- **422** – Business rule: contract is not percentage, or maximum invoice count already reached. `message` explains.
- **500** – Server error. `message` contains the error description.

---

## 3. Generate percentage invoices (all with zero amount)

**Method:** `POST`  
**URL:** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/generate-percentage/{contractId}`

### Purpose

For a **percentage** contract (FeeTypeId = 2), creates all **missing** invoice “slots” with **amount 0** and **tax 0**. The number of slots is determined by the contract’s StartDate, EndDate and PaymentFrequency. If some invoices already exist (e.g. 8), only the remaining ones (e.g. 4) are created. The user can later update each invoice with the real amount (e.g. via the update-invoice API).

### Parameters

| Parameter   | Location | Type | Required | Description        |
|------------|----------|------|----------|--------------------|
| contractId | URL path | int  | Yes      | ID of the contract |

No request body.

### Validations

- `contractId` must be &gt; 0.
- Contract must exist.
- Contract must be **percentage** (FeeTypeId = 2).
- Contract must have StartDate, EndDate, PaymentFrequency, CurrencyCode.
- Maximum number of invoices is calculated from contract dates and frequency. If the contract already has that many invoices, no new ones are created and the response indicates that.

### Success response (200)

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Successfully generated 4 invoice(s) with zero amount for contract CNT-2026-002. Contract now has 12/12 invoices. You can now manually edit each invoice to set the correct amount",
    "invoicesGenerated": 4,
    "invoiceIds": [106, 107, 108, 109],
    "existingInvoicesCount": 8,
    "expectedInvoicesCount": 12
  },
  "message": "Successfully generated 4 invoice(s) with zero amount for contract CNT-2026-002. Contract now has 12/12 invoices. You can now manually edit each invoice to set the correct amount",
  "errorNumber": null,
  "timestamp": "2026-02-07T18:00:00"
}
```

| data field             | Type   | Description                                  |
|------------------------|--------|----------------------------------------------|
| success                | boolean| Always true                                 |
| message                | string | Summary message                              |
| invoicesGenerated      | int    | Number of invoices created in this call     |
| invoiceIds             | int[]  | IDs of the newly created invoices           |
| existingInvoicesCount  | int    | Count before this call                      |
| expectedInvoicesCount | int    | Total invoices allowed for the contract     |

### When maximum already reached (400)

If the contract already has the maximum number of invoices:

```json
{
  "status": false,
  "statusCode": 422,
  "data": {
    "success": false,
    "message": "Cannot generate more invoices. The contract already has the maximum number of invoices (12/12)",
    "invoicesGenerated": 0,
    "invoiceIds": [],
    "existingInvoicesCount": 12,
    "expectedInvoicesCount": 12
  },
  "message": "Cannot generate more invoices. The contract already has the maximum number of invoices (12/12)",
  "errorNumber": null,
  "timestamp": "2026-02-07T18:00:00"
}
```

### Error responses

- **404** – Contract not found.
- **400** – Validation error (e.g. invalid contractId).
- **422** – Contract not percentage, or already at maximum invoices, or invalid dates/frequency. `message` explains.
- **500** – Server error. `message` contains the error description.

---

## 4. Update invoice

**Method:** `PUT`  
**URL:** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/{invoiceId}`

### Purpose

Updates an existing invoice (dates, amount, description, status, notes). Allowed only when the invoice is **not** paid (PaymentStatus ≠ "Paid"). Paid invoices cannot be updated.

### Parameters

| Parameter  | Location | Type | Required | Description        |
|-----------|----------|------|----------|--------------------|
| invoiceId | URL path | int  | Yes      | ID of the invoice  |

### Request body (JSON)

| Field        | Type   | Required | Description                                           |
|-------------|--------|----------|-------------------------------------------------------|
| invoiceDate | string | Yes      | Invoice date (ISO date)                              |
| dueDate     | string | Yes      | Due date (ISO date); must be ≥ invoiceDate            |
| amount      | number | Yes      | Invoice amount (≥ 0)                                 |
| description | string | No       | Line description (max 500 characters)                 |
| status      | string | No       | One of: Draft, Sent, Paid, Overdue, Cancelled         |
| notes       | string | No       | Notes (max 1000 characters)                         |

Example:

```json
{
  "invoiceDate": "2026-02-07",
  "dueDate": "2026-03-07",
  "amount": 3200.00,
  "description": "Services for February 2026",
  "status": "Draft",
  "notes": "Updated amount"
}
```

### Validations

- **Request:** invoiceDate and dueDate required; dueDate ≥ invoiceDate; amount ≥ 0; description ≤ 500; status, if present, must be one of Draft, Sent, Paid, Overdue, Cancelled; notes ≤ 1000.
- **Invoice:** Must exist.
- **Payment status:** Invoice must not be paid (PaymentStatus ≠ "Paid"). If it is paid, the API returns an error and does not update.

Tax is always stored as 0; it is not sent in the request.

### Success response (200)

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Invoice 'INV-2026-015' has been successfully updated",
    "invoiceId": 105,
    "invoiceNumber": "INV-2026-015"
  },
  "message": "Invoice 'INV-2026-015' has been successfully updated",
  "errorNumber": null,
  "timestamp": "2026-02-07T18:00:00"
}
```

| data field      | Type   | Description                |
|-----------------|--------|----------------------------|
| success         | boolean| Always true                |
| message         | string | Confirmation message       |
| invoiceId       | int    | ID of the updated invoice |
| invoiceNumber   | string | Invoice number            |

### Error responses

- **404** – Invoice not found.
- **400** – Validation failed on the body. `message` lists the validation errors.
- **422** – Invoice is paid; updates are not allowed. `message` states that the invoice cannot be modified because it is already paid.
- **500** – Server error. `message` contains the error description.

---

## Summary by endpoint

| Endpoint | Method | Use case | Request body | Key condition |
|----------|--------|----------|--------------|----------------|
| `/generate-fixed/{contractId}` | POST | Fixed amount contract: create all or missing invoices, same amount each | None | FeeTypeId = 1; not over max count |
| `/create-manual` | POST | Percentage contract: create one invoice with chosen amount | contractId, invoiceDate, dueDate, amount, optional description, notes | FeeTypeId = 2; under max count |
| `/generate-percentage/{contractId}` | POST | Percentage contract: create missing invoice slots with amount 0 | None | FeeTypeId = 2; not over max count |
| `/{invoiceId}` | PUT | Change invoice data (dates, amount, description, status, notes) | invoiceDate, dueDate, amount, optional description, status, notes | Invoice exists and is not paid |

All APIs return the same response envelope (`status`, `statusCode`, `data`, `message`, `errorNumber`, `timestamp`). On success, `data` contains the specific payload; on error, `data` is usually `null` and `message` describes what went wrong (validation or business rule).
