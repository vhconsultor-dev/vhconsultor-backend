# Amazon Account APIs – Documentation for Frontend

This document describes the **Amazon Account** maintenance APIs: how they work, parameters, validations, and all possible JSON responses. Use it to integrate the frontend with the backend.

---

## Base information

- **Base URL (API Management):**  
  `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccount`
- **Authentication:** All endpoints require a valid JWT in the `Authorization` header (e.g. `Bearer <token>`). Unauthorized requests return 401.
- **Content-Type:** Send `application/json` for POST and PUT bodies.

---

## Common response structure

Every response (success or error) uses this JSON shape:

| Field        | Type    | Description |
|-------------|---------|-------------|
| `status`    | boolean | `true` = success, `false` = error or validation failure |
| `statusCode`| number  | HTTP-style code (200, 400, 404, 422, 500) |
| `data`      | object / array / number / null | Response payload; `null` on errors |
| `message`   | string  | Human-readable message |
| `errorNumber`| string | Optional; may be `null` |
| `timestamp` | string  | ISO-style date/time (Costa Rica time) |

Frontend should use `status` and `statusCode` to decide success vs error; `message` and `data` for UI/feedback.

---

## 1. GET – List / search Amazon accounts

**URL:**  
`GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccount`

**Purpose:**  
Return Amazon accounts. With no query parameters, returns all accounts. With one or more parameters, filters are applied with **AND** (all conditions must match).

**Query parameters (all optional):**

| Parameter                 | Type   | Match type   | Description |
|---------------------------|--------|--------------|-------------|
| `id`                      | number | Exact        | Only the account with this `AmazonAccountId`. If provided and no account exists, API returns **404**. |
| `customerId`              | number | Exact        | Only accounts for this customer ID. |
| `amazonAccountIdentifier` | string | Partial (LIKE)| Substring match on account identifier (case-sensitive, `%value%`). |
| `isSeller`                | boolean| Exact        | `true` or `false`. |
| `isVendor`                | boolean| Exact        | `true` or `false`. |
| `amazonRegion`            | string | Partial (LIKE)| Substring match on Amazon region (`%value%`). |
| `isActive`                | boolean| Exact        | `true` or `false`. |

**Behavior:**

- **Exact:** value must match the column (e.g. `customerId=5` → only `CustomerId = 5`).
- **Partial (LIKE):** backend wraps the value with `%` (e.g. `amazonAccountIdentifier=ABC` → SQL `LIKE '%ABC%'`). Empty or whitespace-only values are ignored (no filter).
- **Order:** Results are ordered by `CreatedAt` **descending** (newest first).
- **Single ID:** If `id` is sent and there are no results, the API returns **404** with a “not found” message. Without `id`, an empty list returns **200** with `data: []`.

**Success response (200 OK)**

When at least one account is found (or `id` not used and zero accounts):

```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "amazonAccountId": 1,
      "customerId": 10,
      "amazonAccountIdentifier": "A1PQBFHBHS6YH",
      "isSeller": true,
      "isVendor": false,
      "refreshToken": "Atzr|...",
      "amazonRegion": "NA",
      "isActive": true,
      "createdAt": "2026-01-15T10:30:00",
      "updatedAt": "2026-02-01T14:00:00"
    }
  ],
  "message": "Found 1 Amazon account(s).",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

- If exactly one account was requested by `id`, the message is: `"Amazon account retrieved successfully."`
- If multiple accounts: `"Found N Amazon account(s)."`

**No results when filtering by `id` (404 Not Found)**

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Amazon account with ID 999 was not found.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

**Server error (500)**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error retrieving Amazon accounts: <details>",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## 2. POST – Create Amazon account

**URL:**  
`POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccount`

**Purpose:**  
Create a new Amazon account linked to a customer. The **CustomerId must exist** in the system; otherwise the API returns 400.

**Request body (JSON):**

| Field                   | Type    | Required | Description |
|-------------------------|---------|----------|-------------|
| `customerId`            | number  | Yes      | Must be &gt; 0. Customer must exist in DB. |
| `amazonAccountIdentifier`| string  | Yes      | Non-empty, max 50 characters. |
| `isSeller`              | boolean | Yes      | Seller flag. |
| `isVendor`              | boolean | Yes      | Vendor flag. |
| `refreshToken`          | string  | Yes      | Non-empty, max 500 characters. |
| `amazonRegion`          | string  | Yes      | Non-empty, max 20 characters. |
| `isActive`              | boolean | Yes      | Default in backend is `true` if omitted. |

Example body:

```json
{
  "customerId": 10,
  "amazonAccountIdentifier": "A1PQBFHBHS6YH",
  "isSeller": true,
  "isVendor": false,
  "refreshToken": "Atzr|IwMB...",
  "amazonRegion": "NA",
  "isActive": true
}
```

**Validations (run before DB):**

- `customerId`: required, must be &gt; 0.
- `amazonAccountIdentifier`: required, max 50 characters.
- `refreshToken`: required, max 500 characters.
- `amazonRegion`: required, max 20 characters.

If any of these fail, the API returns **400 Bad Request** with `statusCode: 422` in the body and a concatenation of all validation messages in `message`.

**Business rule (after validation):**

- **CustomerId must exist:** If `customerId` does not exist in the Corporate customers table, the API returns **400 Bad Request** (not 404) with a clear message.

**Success response (200 OK)**

```json
{
  "status": true,
  "statusCode": 200,
  "data": 5,
  "message": "Amazon account created successfully.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

`data` is the new **AmazonAccountId** (integer). The frontend can use it to navigate to the detail or list.

**Validation error (400 Bad Request, statusCode 422 in body)**

Example when multiple validations fail (messages can be combined with commas):

```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "Customer ID is required and must be greater than zero., Amazon account identifier is required.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

Single examples:

- `"Customer ID is required and must be greater than zero."`
- `"Amazon account identifier is required."`
- `"Amazon account identifier cannot exceed 50 characters."`
- `"Refresh token is required."`
- `"Refresh token cannot exceed 500 characters."`
- `"Amazon region is required."`
- `"Amazon region cannot exceed 20 characters."`

**Customer not found (400 Bad Request)**

When `customerId` is valid by format but the customer does not exist:

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Customer with ID 999 was not found. Cannot create Amazon account for a non-existent customer.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

**Server / DB error (500)**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error creating Amazon account: <details>",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## 3. PUT – Update Amazon account

**URL:**  
`PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccount/{amazonAccountId}`

**Purpose:**  
Update an existing Amazon account by ID. The account must exist; otherwise 404. **CustomerId** in the body must exist in the system; otherwise 400.

**Path parameter:**

| Parameter          | Type   | Description |
|--------------------|--------|-------------|
| `amazonAccountId`  | number | ID of the account to update. |

**Request body (JSON):**

Same structure as POST, but without “create” semantics:

| Field                   | Type    | Required | Description |
|-------------------------|---------|----------|-------------|
| `customerId`            | number  | Yes      | Must be &gt; 0; customer must exist. |
| `amazonAccountIdentifier`| string  | Yes      | Non-empty, max 50 characters. |
| `isSeller`              | boolean | Yes      | Seller flag. |
| `isVendor`              | boolean | Yes      | Vendor flag. |
| `refreshToken`          | string  | Yes      | Non-empty, max 500 characters. |
| `amazonRegion`          | string  | Yes      | Non-empty, max 20 characters. |
| `isActive`              | boolean | Yes      | Active flag. |

Example:

```json
{
  "customerId": 10,
  "amazonAccountIdentifier": "A1PQBFHBHS6YH",
  "isSeller": true,
  "isVendor": false,
  "refreshToken": "Atzr|IwMB...",
  "amazonRegion": "NA",
  "isActive": false
}
```

**Validations:**  
Same as POST (customerId &gt; 0, required strings, max lengths). If validation fails → **400** with `statusCode: 422` and validation message(s) in `message`.

**Business rules:**

1. **Account must exist:** If `amazonAccountId` does not exist → **404 Not Found**.
2. **Customer must exist:** If `customerId` in the body does not exist → **400 Bad Request** with a clear message.

**Success response (200 OK)**

No `data` payload; success is indicated by `status` and `message`:

```json
{
  "status": true,
  "statusCode": 200,
  "data": null,
  "message": "Amazon account updated successfully.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

**Validation error (400 Bad Request, statusCode 422)**

Same format as POST validation errors; `message` contains one or more validation messages.

**Account not found (404 Not Found)**

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Amazon account with ID 999 was not found.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

**Customer not found (400 Bad Request)**

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Customer with ID 999 was not found. Cannot assign Amazon account to a non-existent customer.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

**Server / DB error (500)**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error updating Amazon account: <details>",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## Summary for frontend

| Endpoint | Method | Purpose |
|----------|--------|--------|
| `/api/corporate/AmazonAccount` | GET    | List/search accounts (optional filters; 404 only when `id` is sent and not found). |
| `/api/corporate/AmazonAccount` | POST   | Create account; body validations + “customer must exist”; success returns new ID in `data`. |
| `/api/corporate/AmazonAccount/{amazonAccountId}` | PUT | Update account; path ID must exist (404), customer in body must exist (400). |

- **Auth:** All requests need a valid JWT.
- **Responses:** Same envelope (`status`, `statusCode`, `data`, `message`, `timestamp`). Use `status` and `statusCode` for branching; show `message` (and `data` when present) to the user.
- **Validation errors:** HTTP 400, body `statusCode: 422`, all rules in English in `message`.
- **Not found:** 404 for missing Amazon account (GET by `id`, PUT by path id).
- **Business errors:** 400 with explicit message when the referenced customer does not exist (POST/PUT).

This is everything the frontend needs to integrate GET, POST, and PUT for Amazon Account without having to know backend implementation details.
