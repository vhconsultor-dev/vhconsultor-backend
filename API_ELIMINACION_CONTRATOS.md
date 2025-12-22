# API: Contract Deletion

## Endpoint

```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/{contractId}
```

## Description

This endpoint allows you to delete (soft delete) a contract from the system. The deletion is logical, meaning the record is not physically removed from the database, but its status is changed to "Cancelled".

**Important:** The system validates that the contract does not have associated invoices before allowing deletion. If the contract has invoices, an error is returned with the list of associated invoice numbers.

---

## Authentication

This endpoint requires authentication via JWT token.

**Required Header:**
```
Authorization: Bearer {token}
```

---

## Parameters

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|-----------|-------------|
| `contractId` | `integer` | Yes | ID of the contract to delete |

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|-----------|-------------|
| `deletedBy` | `string` | No | Username or identifier of the user performing the deletion |

### URL Example

```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/123
```

### URL Example with Optional Parameter

```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/123?deletedBy=john.doe
```

---

## Request

This endpoint **does NOT require a body**. All parameters are sent in the URL.

### Headers

```
Content-Type: application/json
Authorization: Bearer {jwt_token}
```

---

## Response Structure

All responses follow this standard structure:

```json
{
  "status": boolean,
  "statusCode": integer,
  "data": any,
  "message": string,
  "errorNumber": string | null,
  "timestamp": string (ISO 8601)
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `status` | `boolean` | Indicates whether the operation was successful (`true`) or failed (`false`) |
| `statusCode` | `integer` | HTTP status code of the response |
| `data` | `any` | Response data (may be `null` in case of error) |
| `message` | `string` | Descriptive message of the operation result |
| `errorNumber` | `string | null` | Unique error number (only in case of unexpected errors) |
| `timestamp` | `string` | Response date and time in ISO 8601 format (Costa Rica timezone) |

---

## HTTP Status Codes

| Code | Description | When Returned |
|------|-------------|---------------|
| `200` | OK | Contract cancelled successfully |
| `400` | Bad Request | The contract has associated invoices (business validation) |
| `404` | Not Found | The contract with the specified ID does not exist |
| `500` | Internal Server Error | Unexpected server error |

---

## Responses

### ✅ 200 OK - Contract Cancelled Successfully

**Response Body:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Contract 'CON-2025-001' has been successfully cancelled",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The contract status was changed to "Cancelled" successfully. The message includes the contract number of the cancelled contract.

---

### ❌ 400 Bad Request - Contract with Associated Invoices

**Response Body (Multiple Invoices):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete the contract because it has 3 associated invoices: INV-2025-001, INV-2025-002, INV-2025-003. Please delete these invoices first before deleting the contract.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Response Body (Single Invoice):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete the contract because it has 1 associated invoice: INV-2025-001. Please delete these invoices first before deleting the contract.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The contract has one or more associated invoices. The message includes:
- The number of invoices (with correct singular/plural)
- The complete list of invoice numbers separated by commas
- Clear instructions on what to do

**Required Action:** The user must delete all associated invoices before being able to delete the contract.

---

### ❌ 404 Not Found - Contract Not Found

**Response Body:**
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Contract with ID 999 not found",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The provided `contractId` does not exist in the database.

---

### ❌ 500 Internal Server Error - Unexpected Error

**Response Body:**
```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "An unexpected error occurred while cancelling the contract: [error description]",
  "errorNumber": "ERR-20251221-001234",
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** An unexpected error occurred on the server. The `errorNumber` field contains a unique identifier that can be used to report the error to technical support.

---

## Validations

### Business Validations

1. **Contract Existence**
   - The contract with the specified `contractId` must exist in the database
   - If it does not exist, returns `404 Not Found`

2. **Associated Invoices**
   - The contract **MUST NOT** have associated invoices
   - If it has invoices, returns `400 Bad Request` with the list of invoice numbers
   - The validation searches for all invoices where `ContractId` matches the contract ID

### Input Validations

1. **contractId**
   - Must be a valid integer
   - Must be greater than 0
   - If invalid, the framework will automatically return `400 Bad Request`

2. **deletedBy** (Optional)
   - If provided, should be a non-empty string
   - Used to track who performed the deletion

---

## System Behavior

### Soft Delete

The system implements **logical deletion (soft delete)**:

- The record is **NOT physically deleted** from the database
- The `Status` field is updated to `"Cancelled"`
- The `UpdatedAt` field is updated with the current date and time (Costa Rica timezone)
- The `LastModifiedBy` field is updated with the user who performed the deletion (if provided)
- The contract can be recovered later if necessary

### Invoice Validation

Before deleting the contract, the system:

1. Queries all invoices associated with the contract
2. If invoices are found:
   - Retrieves the invoice numbers (`InvoiceNumber`)
   - Generates a descriptive message with the complete list
   - Throws an `InvalidOperationException` that is converted to `400 Bad Request`
3. If no invoices are found:
   - Proceeds with the logical deletion

---

## Usage Examples

### Example 1: Successful Cancellation

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Contract 'CON-2025-001' has been successfully cancelled",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 2: Contract with Invoices

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/456
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (400 Bad Request):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete the contract because it has 2 associated invoices: INV-2025-001, INV-2025-015. Please delete these invoices first before deleting the contract.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 3: Contract Not Found

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/999
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (404 Not Found):**
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Contract with ID 999 not found",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 4: Invalid ID

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/abc
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-...",
  "errors": {
    "contractId": [
      "The value 'abc' is not valid."
    ]
  }
}
```

---

### Example 5: Cancellation with User Tracking

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/123?deletedBy=john.doe
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Contract 'CON-2025-001' has been successfully cancelled",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Note:** The `deletedBy` parameter is stored in the `LastModifiedBy` field of the contract record.

---

## Validation Flow

```
┌─────────────────────────────────────┐
│  DELETE /Contract/{contractId}      │
└──────────────┬──────────────────────┘
               │
               ▼
    ┌──────────────────────┐
    │ Does contract exist?  │
    └──────┬───────────────┘
           │
    ┌──────┴──────┐
    │             │
   NO            YES
    │             │
    ▼             ▼
┌────────┐  ┌──────────────────────┐
│  404   │  │ Has invoices?        │
│  Not   │  └──────┬───────────────┘
│ Found  │         │
└────────┘    ┌────┴────┐
              │        │
             YES       NO
              │        │
              ▼        ▼
        ┌─────────┐  ┌──────────┐
        │   400   │  │   200    │
        │   Bad   │  │    OK    │
        │ Request │  │ Cancelled│
        └─────────┘  └──────────┘
```

---

## Important Notes

1. **Logical Deletion**: The contract is not physically deleted, only its status is changed to "Cancelled". This allows maintaining history and referential integrity.

2. **Data Integrity**: Invoice validation prevents deletion of contracts with active relationships, maintaining data integrity.

3. **User-Friendly Messages**: Error messages are descriptive and provide specific information (invoice numbers) to help users resolve the issue.

4. **Authentication Required**: This endpoint requires a valid JWT token. Without authentication, it will return `401 Unauthorized`.

5. **Timezone**: All timestamps are in Costa Rica timezone (UTC-6).

6. **User Tracking**: The optional `deletedBy` parameter allows tracking who performed the deletion, which is useful for audit purposes.

---

## Frontend Error Handling

### Case 1: Contract with Invoices

```javascript
try {
  const response = await fetch(
    `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/${contractId}`,
    {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );

  const data = await response.json();

  if (!data.status) {
    if (response.status === 400) {
      // Show user-friendly message with invoice numbers
      alert(data.message);
      // Optional: extract invoice numbers from message to display in UI
    }
  }
} catch (error) {
  console.error('Error deleting contract:', error);
}
```

### Case 2: Extract Invoice Numbers from Message

```javascript
// Message format: "...has X associated invoices: INV-2025-001, INV-2025-002..."
const message = data.message;
const invoicesMatch = message.match(/:\s*([^.]+)/);
if (invoicesMatch) {
  const invoiceNumbers = invoicesMatch[1].split(', ').map(i => i.trim());
  console.log('Associated invoices:', invoiceNumbers);
  // Display invoice numbers in UI for user to delete them
}
```

### Case 3: Cancellation with User Tracking

```javascript
const deletedBy = 'john.doe'; // Current logged-in user
const response = await fetch(
  `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/${contractId}?deletedBy=${deletedBy}`,
  {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  }
);
```

---

## Testing

### Recommended Test Cases

1. ✅ Delete contract without invoices → Should return `200 OK`
2. ✅ Delete contract with 1 invoice → Should return `400 Bad Request` with singular message
3. ✅ Delete contract with multiple invoices → Should return `400 Bad Request` with complete list
4. ✅ Delete non-existent contract → Should return `404 Not Found`
5. ✅ Delete with invalid ID (non-numeric) → Should return `400 Bad Request`
6. ✅ Delete without authentication → Should return `401 Unauthorized`
7. ✅ Verify that `Status` is updated to `"Cancelled"` after successful deletion
8. ✅ Verify that `UpdatedAt` is updated with correct timestamp
9. ✅ Verify that `LastModifiedBy` is updated when `deletedBy` parameter is provided
10. ✅ Verify that `LastModifiedBy` remains unchanged when `deletedBy` parameter is not provided

---

## Relationship with Invoices

When a contract is cancelled:

- **Invoices are NOT automatically deleted**
- Invoices remain in the database with their original status
- The relationship between contract and invoices is maintained
- Users must manually delete invoices before cancelling the contract
- This ensures data integrity and prevents accidental loss of financial records

---

## Changelog

### Current Version
- ✅ Invoice validation before deletion
- ✅ User-friendly messages with invoice numbers
- ✅ Soft delete (status change to "Cancelled")
- ✅ Improved error handling
- ✅ Consistent structured responses
- ✅ Optional user tracking via `deletedBy` parameter
- ✅ All messages in English

---

## Support

To report issues or request changes, contact the development team with:
- The `errorNumber` (if available)
- The `timestamp` of the response
- Details of the request that caused the error

