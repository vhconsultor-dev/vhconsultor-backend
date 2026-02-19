# Amazon Account–Marketplace APIs – Documentation

This document describes the **Amazon Account–Marketplace** maintenance APIs. They manage the link between **Amazon Accounts** (which are linked to customers) and **Amazon Marketplaces**: one account can have many marketplaces. Use this documentation to integrate with the APIs (parameters, validations, request/response JSON, and error handling).

---

## Base information

- **Base URL (API Management):**  
  `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccountMarketplace`
- **Authentication:** All endpoints require a valid JWT in the `Authorization` header (e.g. `Bearer <token>`). Unauthorized requests return 401.
- **Content-Type:** For POST and PUT, send `application/json`.

---

## Common response structure

Every response uses the same JSON envelope:

| Field         | Type    | Description |
|---------------|---------|-------------|
| `status`      | boolean | `true` = success, `false` = error or validation failure |
| `statusCode`  | number  | HTTP-style code (200, 400, 404, 422, 500) |
| `data`        | varies  | Response payload; `null` on errors |
| `message`     | string  | Human-readable message |
| `errorNumber` | string  | Optional; may be `null` |
| `timestamp`   | string  | Server timestamp (e.g. Costa Rica time) |

Use `status` and `statusCode` to determine success or failure; use `message` and `data` for user feedback.

---

## 1. GET – List or filter associations

**URL:**  
`GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccountMarketplace`

**Purpose:**  
Return associations between Amazon accounts and marketplaces. With no query parameters, all associations are returned. With one or more parameters, filters are applied with **AND** (all conditions must match). All filters use **exact match** (no partial/LIKE).

### Query parameters (all optional)

| Parameter              | Type    | Match | Description |
|------------------------|--------|-------|-------------|
| `id`                   | number | Exact | Return only the association with this `AmazonAccountMarketplaceId`. If provided and no row exists, the API returns **404**. |
| `amazonAccountId`      | number | Exact | Filter by Amazon account ID (from `Corporate.AmazonAccounts`). |
| `amazonMarketplaceId`  | number | Exact | Filter by Amazon marketplace ID (from `Corporate.AmazonMarketplaces`). |
| `isPrimary`            | boolean| Exact | Filter by primary flag (`true` or `false`). |
| `isActive`             | boolean| Exact | Filter by active status (`true` or `false`). |

### Behaviour

- **Combining filters:** Multiple parameters are combined with AND. Example: `?amazonAccountId=5&isActive=true` returns only associations for account 5 that are active.
- **Order:** Results are ordered by `CreatedAt` **descending** (newest first).
- **Single ID:** If **only** `id` is sent and there are no results, the API returns **404**. If no `id` is sent, an empty list returns **200** with `data: []`.

### Success response (200 OK)

When at least one association is found, or when no `id` is sent and the list is empty:

```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "amazonAccountMarketplaceId": 1,
      "amazonAccountId": 5,
      "amazonMarketplaceId": 3,
      "isPrimary": true,
      "isActive": true,
      "createdAt": "2026-02-07T10:00:00"
    }
  ],
  "message": "Found 1 Amazon account–marketplace association(s).",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

- If exactly one association was requested by `id`, the message is: `"Amazon account–marketplace association retrieved successfully."`
- Otherwise: `"Found N Amazon account–marketplace association(s)."` (or `"Found 0 ..."` when the list is empty and `id` was not used).

### Not found when filtering by `id` (404 Not Found)

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Amazon account–marketplace association with ID 999 was not found.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

### Server error (500)

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error retrieving Amazon account–marketplace associations: <details>",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## 2. POST – Create an association

**URL:**  
`POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccountMarketplace`

**Purpose:**  
Create a new link between an Amazon account and a marketplace. The account and marketplace must exist; the same pair (account + marketplace) cannot be linked more than once.

### Request body (JSON)

| Field                 | Type    | Required | Description |
|-----------------------|---------|----------|-------------|
| `amazonAccountId`     | number  | Yes      | ID of the Amazon account (`Corporate.AmazonAccounts`). Must be greater than 0. |
| `amazonMarketplaceId` | number  | Yes      | ID of the Amazon marketplace (`Corporate.AmazonMarketplaces`). Must be greater than 0. |
| `isPrimary`           | boolean | Yes      | Whether this marketplace is the primary one for this account. |
| `isActive`            | boolean | Yes      | Whether the association is active. Defaults to `true` if omitted. |

Example:

```json
{
  "amazonAccountId": 5,
  "amazonMarketplaceId": 3,
  "isPrimary": true,
  "isActive": true
}
```

### Validations (before database)

- **amazonAccountId:** Required, must be greater than 0.  
  Error: `"Amazon account ID is required and must be greater than zero."`
- **amazonMarketplaceId:** Required, must be greater than 0.  
  Error: `"Amazon marketplace ID is required and must be greater than zero."`

If any validation fails, the API returns **400 Bad Request** with `statusCode: 422` in the body and one or more messages in `message` (comma-separated if multiple).

### Business rules (after validation)

1. **Amazon account must exist:** The `amazonAccountId` must exist in `Corporate.AmazonAccounts`. If not, the API returns **400** with:
   - `"Amazon account with ID {id} was not found. Cannot link marketplace to a non-existent account."`

2. **Amazon marketplace must exist:** The `amazonMarketplaceId` must exist in `Corporate.AmazonMarketplaces`. If not, the API returns **400** with:
   - `"Amazon marketplace with ID {id} was not found. Cannot link account to a non-existent marketplace."`

3. **No duplicate pair:** The combination (amazonAccountId, amazonMarketplaceId) must not already exist in `Corporate.AmazonAccountMarketplaces`. If it does, the API returns **400** with:
   - `"This Amazon account is already linked to the selected marketplace. Amazon account ID {x} and marketplace ID {y} association already exists."`

### Success response (200 OK)

```json
{
  "status": true,
  "statusCode": 200,
  "data": 7,
  "message": "Amazon account–marketplace association created successfully.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

`data` is the new **AmazonAccountMarketplaceId** (integer). Use it to reference the association in GET by `id`, PUT, or DELETE.

### Validation error (400 Bad Request, statusCode 422 in body)

Example with one or more validation messages:

```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "Amazon account ID is required and must be greater than zero., Amazon marketplace ID is required and must be greater than zero.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

### Account or marketplace not found / duplicate (400 Bad Request)

Examples:

- Account not found:  
  `"message": "Amazon account with ID 999 was not found. Cannot link marketplace to a non-existent account."`
- Marketplace not found:  
  `"message": "Amazon marketplace with ID 999 was not found. Cannot link account to a non-existent marketplace."`
- Duplicate:  
  `"message": "This Amazon account is already linked to the selected marketplace. Amazon account ID 5 and marketplace ID 3 association already exists."`

In all cases the response body has `status: false`, `statusCode: 400`, `data: null`, and the corresponding `message`.

### Server / database error (500)

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error creating Amazon account–marketplace association: <details>",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## 3. PUT – Update an association

**URL:**  
`PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccountMarketplace/{amazonAccountMarketplaceId}`

**Purpose:**  
Update an existing association by its ID. Only **IsPrimary** and **IsActive** can be changed; the account and marketplace IDs are not updated (to change the link, delete and create a new association).

### Path parameter

| Parameter                   | Type   | Description |
|----------------------------|--------|-------------|
| `amazonAccountMarketplaceId` | number | ID of the association row (`AmazonAccountMarketplaceId`). |

### Request body (JSON)

| Field       | Type    | Required | Description |
|------------|---------|----------|-------------|
| `isPrimary`| boolean | Yes      | Whether this marketplace is the primary one for this account. |
| `isActive` | boolean | Yes      | Whether the association is active. |

Example:

```json
{
  "isPrimary": false,
  "isActive": true
}
```

### Validations

- The body is validated (e.g. types). For the current implementation there are no extra FluentValidation rules; invalid JSON or types can result in 400.
- **Association must exist:** If `amazonAccountMarketplaceId` does not exist, the API returns **404**.

### Success response (200 OK)

```json
{
  "status": true,
  "statusCode": 200,
  "data": null,
  "message": "Amazon account–marketplace association updated successfully.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

### Association not found (404 Not Found)

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Amazon account–marketplace association with ID 999 was not found.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

### Server / database error (500)

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error updating Amazon account–marketplace association: <details>",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## 4. DELETE – Remove an association

**URL:**  
`DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccountMarketplace/{amazonAccountMarketplaceId}`

**Purpose:**  
Delete the link between an Amazon account and a marketplace. The row in `Corporate.AmazonAccountMarketplaces` is removed. There is no request body.

### Path parameter

| Parameter                   | Type   | Description |
|----------------------------|--------|-------------|
| `amazonAccountMarketplaceId` | number | ID of the association to delete (`AmazonAccountMarketplaceId`). |

### Success response (200 OK)

```json
{
  "status": true,
  "statusCode": 200,
  "data": null,
  "message": "Amazon account–marketplace association deleted successfully.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

### Association not found (404 Not Found)

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Amazon account–marketplace association with ID 999 was not found.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

### Server / database error (500)

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error deleting Amazon account–marketplace association: <details>",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## Summary table

| Method | Endpoint (path only) | Purpose |
|--------|------------------------|--------|
| GET    | `/api/corporate/AmazonAccountMarketplace` | List/filter associations (optional: id, amazonAccountId, amazonMarketplaceId, isPrimary, isActive). 404 only when `id` is sent and not found. |
| POST   | `/api/corporate/AmazonAccountMarketplace` | Create association; body: amazonAccountId, amazonMarketplaceId, isPrimary, isActive. Validates existence and no duplicate pair. |
| PUT    | `/api/corporate/AmazonAccountMarketplace/{amazonAccountMarketplaceId}` | Update association; body: isPrimary, isActive. 404 if ID not found. |
| DELETE | `/api/corporate/AmazonAccountMarketplace/{amazonAccountMarketplaceId}` | Delete association by ID. 404 if ID not found. |

---

## Data model (returned in GET)

Each item in `data` (GET) has the following shape:

| Field                       | Type    | Description |
|----------------------------|---------|-------------|
| `amazonAccountMarketplaceId` | number  | Primary key of the association. |
| `amazonAccountId`          | number  | Reference to `Corporate.AmazonAccounts`. |
| `amazonMarketplaceId`      | number  | Reference to `Corporate.AmazonMarketplaces`. |
| `isPrimary`                | boolean | Whether this marketplace is the primary one for this account. |
| `isActive`                 | boolean | Whether the association is active. |
| `createdAt`                | string  | When the association was created (ISO-style datetime). |

---

## Notes for integration

- **Authentication:** All requests must send a valid JWT; otherwise the API returns 401.
- **Ids:** Use `AmazonAccountId` and `AmazonMarketplaceId` from the existing Amazon Account and Amazon Marketplace APIs to build valid POST bodies. Use `AmazonAccountMarketplaceId` from GET or POST success for PUT and DELETE.
- **Errors:** Use `status` and `statusCode` to branch; show `message` to the user. For validation (422), `message` may contain several sentences separated by commas.
- **No DELETE body:** DELETE does not accept a body; the ID is only in the URL.

This document describes only how to use the APIs (parameters, validations, request/response JSON, and errors), not how to implement the client.
