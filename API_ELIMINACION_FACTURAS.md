# API: Invoice Deletion

## Endpoints

### Delete Single Invoice
```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?invoiceId={invoiceId}
```

### Delete All Contract Invoices
```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?contractId={contractId}
```

## Description

This endpoint allows you to delete invoices from the system. It supports two modes of operation:

1. **Single Invoice Deletion**: Delete a specific invoice by providing `invoiceId`
2. **Bulk Deletion**: Delete ALL invoices from a contract by providing `contractId`

**Important:** The deletion is physical (permanent). Invoices are permanently removed from the database. The system validates:
- Invoice(s) must NOT be paid (`PaymentStatus != "Paid"`)
- Invoice(s) must NOT have attachments

---

## Authentication

This endpoint requires authentication via JWT token.

**Required Header:**
```
Authorization: Bearer {token}
```

---

## Parameters

### Query Parameters

| Parameter | Type | Required | Description | Mutual Exclusivity |
|-----------|------|----------|-------------|-------------------|
| `invoiceId` | `integer` | Conditional | ID of the specific invoice to delete | Cannot be used with `contractId` |
| `contractId` | `integer` | Conditional | ID of the contract to delete all invoices from | Cannot be used with `invoiceId` |

**Important:** You must provide **either** `invoiceId` **OR** `contractId`, but **NOT both**.

### URL Examples

**Delete a single invoice:**
```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?invoiceId=123
```

**Delete all invoices from a contract:**
```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?contractId=45
```

---

## Request

This endpoint **does NOT require a body**. All parameters are sent as query strings in the URL.

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
  "data": {
    "success": boolean,
    "deletedCount": integer,
    "message": string
  },
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
| `data.success` | `boolean` | Indicates if the deletion was successful |
| `data.deletedCount` | `integer` | Number of invoices deleted |
| `data.message` | `string` | Detailed message about the operation |
| `message` | `string` | Summary message of the operation result |
| `errorNumber` | `string | null` | Unique error number (only in case of unexpected errors) |
| `timestamp` | `string` | Response date and time in ISO 8601 format (Costa Rica timezone) |

---

## HTTP Status Codes

| Code | Description | When Returned |
|------|-------------|---------------|
| `200` | OK | Invoice(s) deleted successfully |
| `400` | Bad Request | Invoice(s) are paid or have attachments (business validation) |
| `404` | Not Found | The invoice with the specified ID does not exist |
| `422` | Unprocessable Entity | Invalid parameters (missing both parameters or both provided) |
| `500` | Internal Server Error | Unexpected server error |

---

## Responses

### ✅ 200 OK - Single Invoice Deleted Successfully

**Response Body:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "deletedCount": 1,
    "message": "Invoice 'INV-2025-001' has been successfully deleted"
  },
  "message": "Invoice 'INV-2025-001' has been successfully deleted",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The invoice was permanently deleted from the database.

---

### ✅ 200 OK - Multiple Invoices Deleted Successfully

**Response Body:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "deletedCount": 3,
    "message": "Successfully deleted 3 invoice(s): INV-2025-001, INV-2025-002, INV-2025-003"
  },
  "message": "Successfully deleted 3 invoice(s): INV-2025-001, INV-2025-002, INV-2025-003",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** All invoices from the contract were permanently deleted. The message includes the complete list of deleted invoice numbers.

---

### ✅ 200 OK - No Invoices Found for Contract

**Response Body:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "deletedCount": 0,
    "message": "No invoices found for this contract"
  },
  "message": "No invoices found for this contract",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The contract exists but has no invoices to delete.

---

### ❌ 400 Bad Request - Paid Invoice (Single)

**Response Body:**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoice 'INV-2025-001' because it has already been paid. Paid invoices cannot be deleted to maintain financial record integrity.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The invoice has been marked as paid (`PaymentStatus = "Paid"`) and cannot be deleted to preserve financial integrity.

**Required Action:** Paid invoices cannot be deleted. This is by design to maintain audit trails and financial records.

---

### ❌ 400 Bad Request - Paid Invoices (Multiple)

**Response Body:**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoices because 2 invoices are already paid: INV-2025-001, INV-2025-003. Paid invoices cannot be deleted to maintain financial record integrity.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** One or more invoices in the contract are paid. The message lists all paid invoice numbers.

**Required Action:** You cannot delete paid invoices. You may only delete unpaid invoices individually.

---

### ❌ 400 Bad Request - Invoice with Attachments (Single)

**Response Body:**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoice 'INV-2025-001' because it has 3 associated attachments. Please delete all attachments first before deleting the invoice.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The invoice has file attachments (payment proofs, documents, etc.). The message includes the attachment count.

**Required Action:** Delete all attachments from the invoice before attempting to delete the invoice itself.

---

### ❌ 400 Bad Request - Invoices with Attachments (Multiple)

**Response Body:**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoices because 2 invoices have attachments: INV-2025-001 (2 attachments), INV-2025-003 (1 attachment). Please delete all attachments first before deleting the invoices.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** One or more invoices have attachments. The message lists each invoice with its attachment count.

**Required Action:** Delete all attachments from these invoices before attempting bulk deletion.

---

### ❌ 404 Not Found - Invoice Not Found

**Response Body:**
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Invoice with ID 999 not found",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** The provided `invoiceId` does not exist in the database.

---

### ❌ 422 Unprocessable Entity - Missing Parameters

**Response Body:**
```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "You must provide either 'invoiceId' to delete a specific invoice or 'contractId' to delete all invoices from a contract",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** Neither `invoiceId` nor `contractId` was provided.

**Required Action:** Provide one of the required parameters.

---

### ❌ 422 Unprocessable Entity - Both Parameters Provided

**Response Body:**
```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "You cannot provide both 'invoiceId' and 'contractId'. Please provide only one parameter",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** Both `invoiceId` and `contractId` were provided simultaneously.

**Required Action:** Choose one deletion mode: either single invoice or all contract invoices.

---

### ❌ 500 Internal Server Error - Unexpected Error

**Response Body:**
```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "An unexpected error occurred while deleting invoice(s): [error description]",
  "errorNumber": "ERR-20251221-001234",
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Description:** An unexpected error occurred on the server. The `errorNumber` field contains a unique identifier for support.

---

## Validations

### Business Validations

#### For Single Invoice Deletion:

1. **Invoice Existence**
   - The invoice with the specified `invoiceId` must exist
   - If not found, returns `404 Not Found`

2. **Payment Status**
   - The invoice must NOT be paid (`PaymentStatus != "Paid"`)
   - If paid, returns `400 Bad Request` with explanation
   - This protects financial record integrity

3. **Attachments**
   - The invoice must NOT have any attachments
   - If attachments exist, returns `400 Bad Request` with attachment count
   - User must delete attachments first

#### For Bulk Deletion (All Contract Invoices):

1. **Paid Invoices Check**
   - Validates ALL invoices in the contract
   - If ANY invoice is paid, returns `400 Bad Request`
   - Lists ALL paid invoice numbers
   - NO invoices are deleted if any are paid

2. **Attachments Check**
   - Validates ALL invoices in the contract
   - If ANY invoice has attachments, returns `400 Bad Request`
   - Lists ALL invoices with attachments and their counts
   - NO invoices are deleted if any have attachments

3. **All-or-Nothing**
   - Either ALL invoices pass validation and are deleted
   - Or NO invoices are deleted if any fail validation
   - This ensures data consistency

### Input Validations

1. **Parameter Exclusivity**
   - Must provide exactly ONE of: `invoiceId` OR `contractId`
   - Cannot provide both
   - Cannot provide neither

2. **Parameter Types**
   - Both parameters must be valid integers
   - Must be greater than 0

---

## System Behavior

### Physical Deletion

Unlike customers and contracts (which use soft delete), invoices use **physical deletion**:

- The invoice record is **permanently removed** from the database
- Related `InvoiceItems` are automatically deleted (CASCADE)
- The operation cannot be undone
- This is appropriate for draft/unpaid invoices that were created in error

### Cascade Deletion

When an invoice is deleted:

- **InvoiceItems** are automatically deleted (CASCADE relationship)
- **InvoiceAttachments** must be manually deleted first (RESTRICT relationship)
- This prevents accidental loss of important files

### Financial Integrity Protection

The system protects financial records by:

- **Preventing deletion of paid invoices** - maintains audit trail
- **Requiring attachment deletion first** - ensures deliberate action
- **All-or-nothing bulk deletion** - maintains consistency

---

## Usage Examples

### Example 1: Delete a Single Unpaid Invoice

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?invoiceId=123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "deletedCount": 1,
    "message": "Invoice 'INV-2025-001' has been successfully deleted"
  },
  "message": "Invoice 'INV-2025-001' has been successfully deleted",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 2: Delete All Invoices from a Contract

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?contractId=45
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "deletedCount": 5,
    "message": "Successfully deleted 5 invoice(s): INV-2025-001, INV-2025-002, INV-2025-003, INV-2025-004, INV-2025-005"
  },
  "message": "Successfully deleted 5 invoice(s): INV-2025-001, INV-2025-002, INV-2025-003, INV-2025-004, INV-2025-005",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 3: Attempt to Delete Paid Invoice

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?invoiceId=456
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (400 Bad Request):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoice 'INV-2025-005' because it has already been paid. Paid invoices cannot be deleted to maintain financial record integrity.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 4: Attempt to Delete Invoice with Attachments

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?invoiceId=789
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (400 Bad Request):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoice 'INV-2025-007' because it has 2 associated attachments. Please delete all attachments first before deleting the invoice.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 5: Bulk Delete with Mixed Issues

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?contractId=99
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (400 Bad Request - Paid Invoices):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoices because 2 invoices are already paid: INV-2025-010, INV-2025-012. Paid invoices cannot be deleted to maintain financial record integrity.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Response (400 Bad Request - Attachments):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Cannot delete invoices because 3 invoices have attachments: INV-2025-011 (1 attachment), INV-2025-013 (3 attachments), INV-2025-015 (1 attachment). Please delete all attachments first before deleting the invoices.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 6: Invalid Parameters (Both Provided)

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?invoiceId=123&contractId=45
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (422 Unprocessable Entity):**
```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "You cannot provide both 'invoiceId' and 'contractId'. Please provide only one parameter",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Example 7: Invalid Parameters (None Provided)

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (422 Unprocessable Entity):**
```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "You must provide either 'invoiceId' to delete a specific invoice or 'contractId' to delete all invoices from a contract",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

## Validation Flow

### Single Invoice Deletion Flow

```
┌───────────────────────────────────────┐
│  DELETE /Invoice?invoiceId={id}       │
└──────────────┬────────────────────────┘
               │
               ▼
    ┌──────────────────────┐
    │ Does invoice exist?   │
    └──────┬───────────────┘
           │
    ┌──────┴──────┐
    │             │
   NO            YES
    │             │
    ▼             ▼
┌────────┐  ┌──────────────────────┐
│  404   │  │ Is invoice paid?     │
│  Not   │  └──────┬───────────────┘
│ Found  │         │
└────────┘    ┌────┴────┐
              │        │
             YES       NO
              │        │
              ▼        ▼
        ┌─────────┐  ┌──────────────────────┐
        │   400   │  │ Has attachments?     │
        │   Bad   │  └──────┬───────────────┘
        │ Request │         │
        └─────────┘    ┌────┴────┐
                       │        │
                      YES       NO
                       │        │
                       ▼        ▼
                 ┌─────────┐  ┌──────────┐
                 │   400   │  │   200    │
                 │   Bad   │  │    OK    │
                 │ Request │  │ Deleted  │
                 └─────────┘  └──────────┘
```

### Bulk Deletion Flow

```
┌───────────────────────────────────────┐
│  DELETE /Invoice?contractId={id}      │
└──────────────┬────────────────────────┘
               │
               ▼
    ┌──────────────────────────┐
    │ Load all invoices        │
    │ for contract             │
    └──────┬───────────────────┘
           │
           ▼
    ┌──────────────────────────┐
    │ Any invoices found?       │
    └──────┬───────────────────┘
           │
    ┌──────┴──────┐
    │             │
   NO            YES
    │             │
    ▼             ▼
┌────────┐  ┌──────────────────────────┐
│  200   │  │ Check ALL invoices:      │
│   OK   │  │ - Any paid?              │
│ (None) │  │ - Any with attachments?  │
└────────┘  └──────┬───────────────────┘
                   │
            ┌──────┴──────┐
            │             │
         ISSUES      ALL CLEAR
            │             │
            ▼             ▼
      ┌─────────┐   ┌──────────┐
      │   400   │   │   200    │
      │   Bad   │   │    OK    │
      │ Request │   │ All      │
      │(Details)│   │ Deleted  │
      └─────────┘   └──────────┘
```

---

## Important Notes

1. **Physical Deletion**: Invoices are permanently deleted. This operation cannot be undone.

2. **Paid Invoice Protection**: Paid invoices cannot be deleted to maintain financial audit trails and compliance.

3. **Attachment Protection**: Invoices with attachments must have attachments deleted first, preventing accidental loss of important documents.

4. **Bulk Deletion Safety**: When deleting all contract invoices, the system validates ALL invoices first. If ANY fail validation, NO invoices are deleted.

5. **User-Friendly Messages**: All error messages provide specific details about what prevented the deletion and how to resolve it.

6. **Authentication Required**: This endpoint requires a valid JWT token. Without authentication, it will return `401 Unauthorized`.

7. **Timezone**: All timestamps are in Costa Rica timezone (UTC-6).

8. **Cascade Behavior**: Invoice items are automatically deleted with their parent invoice, but attachments must be manually deleted first.

---

## Frontend Error Handling

### Case 1: Single Invoice with Validation Issues

```javascript
try {
  const response = await fetch(
    `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?invoiceId=${invoiceId}`,
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
      // Show user-friendly message
      if (data.message.includes('paid')) {
        alert('This invoice cannot be deleted because it has been paid.');
      } else if (data.message.includes('attachment')) {
        alert('Please delete all attachments from this invoice first.');
      }
    }
  } else {
    console.log(`Deleted ${data.data.deletedCount} invoice(s)`);
  }
} catch (error) {
  console.error('Error deleting invoice:', error);
}
```

### Case 2: Bulk Deletion with Detailed Error Handling

```javascript
async function deleteAllContractInvoices(contractId) {
  try {
    const response = await fetch(
      `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?contractId=${contractId}`,
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
        // Extract specific issue
        if (data.message.includes('paid')) {
          // Extract paid invoice numbers
          const paidMatch = data.message.match(/:\s*([^.]+)/);
          if (paidMatch) {
            const paidInvoices = paidMatch[1].split(', ');
            showPaidInvoicesError(paidInvoices);
          }
        } else if (data.message.includes('attachment')) {
          // Extract invoices with attachments
          const attachmentMatch = data.message.match(/:\s*([^.]+)/);
          if (attachmentMatch) {
            const invoicesWithAttachments = attachmentMatch[1];
            showAttachmentsError(invoicesWithAttachments);
          }
        }
      }
    } else {
      // Success
      showSuccess(`Successfully deleted ${data.data.deletedCount} invoice(s)`);
      console.log('Deleted invoices:', data.data.message);
    }
  } catch (error) {
    console.error('Error deleting invoices:', error);
  }
}
```

### Case 3: Parameter Validation

```javascript
function deleteInvoice(invoiceId = null, contractId = null) {
  // Validate parameters
  if (!invoiceId && !contractId) {
    alert('Please provide either an invoice ID or contract ID');
    return;
  }
  
  if (invoiceId && contractId) {
    alert('Please provide only one parameter: invoice ID or contract ID');
    return;
  }

  // Build URL based on provided parameter
  const param = invoiceId ? `invoiceId=${invoiceId}` : `contractId=${contractId}`;
  const url = `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice?${param}`;
  
  // Proceed with deletion...
}
```

---

## Testing

### Recommended Test Cases

#### Single Invoice Deletion:
1. ✅ Delete unpaid invoice without attachments → Should return `200 OK`
2. ✅ Delete paid invoice → Should return `400 Bad Request` with paid message
3. ✅ Delete invoice with 1 attachment → Should return `400 Bad Request` with singular message
4. ✅ Delete invoice with multiple attachments → Should return `400 Bad Request` with plural message
5. ✅ Delete non-existent invoice → Should return `404 Not Found`
6. ✅ Delete without authentication → Should return `401 Unauthorized`

#### Bulk Deletion:
7. ✅ Delete all unpaid invoices without attachments → Should return `200 OK` with count
8. ✅ Delete when some invoices are paid → Should return `400 Bad Request` listing paid invoices
9. ✅ Delete when some invoices have attachments → Should return `400 Bad Request` with details
10. ✅ Delete when no invoices exist for contract → Should return `200 OK` with count 0
11. ✅ Delete with mixed issues (paid and attachments) → Should return appropriate `400 Bad Request`

#### Parameter Validation:
12. ✅ Call without any parameters → Should return `422 Unprocessable Entity`
13. ✅ Call with both parameters → Should return `422 Unprocessable Entity`
14. ✅ Call with invalid parameter type → Should return `400 Bad Request`

#### Data Integrity:
15. ✅ Verify InvoiceItems are deleted with invoice (CASCADE)
16. ✅ Verify database transaction rollback on error
17. ✅ Verify accurate `deletedCount` in response

---

## Relationship with Other Entities

### Invoices and Contracts

- Invoices belong to a Contract
- Deleting a contract does NOT automatically delete its invoices
- You must delete all invoices before deleting a contract
- Use bulk deletion (`contractId` parameter) to efficiently delete all contract invoices

### Invoices and InvoiceItems

- Invoice items are automatically deleted when their parent invoice is deleted (CASCADE)
- No manual action needed for invoice items

### Invoices and InvoiceAttachments

- Attachments must be manually deleted before deleting an invoice (RESTRICT)
- This prevents accidental loss of important payment proofs or documents
- Use the Invoice Attachments API to delete attachments first

---

## Best Practices

1. **Always validate payment status before allowing users to delete** - Show delete button only for unpaid invoices

2. **Warn users about permanent deletion** - Implement confirmation dialogs explaining the operation is irreversible

3. **Show attachment count in UI** - Display if an invoice has attachments before attempting deletion

4. **Use bulk deletion carefully** - Confirm with users before deleting all contract invoices

5. **Handle errors gracefully** - Parse error messages to provide specific guidance to users

6. **Maintain audit logs** - Log all deletion attempts (successful and failed) in your frontend for accountability

7. **Check attachments first** - Before attempting deletion, verify invoice has no attachments

8. **Consider soft delete for paid invoices** - If business rules change, consider implementing status-based hiding instead of deletion

---

## Changelog

### Current Version
- ✅ Physical deletion of unpaid invoices
- ✅ Single invoice and bulk deletion modes
- ✅ Payment status validation
- ✅ Attachment validation
- ✅ All-or-nothing bulk deletion
- ✅ Detailed error messages in English
- ✅ Consistent structured responses
- ✅ Financial integrity protection

---

## Support

To report issues or request changes, contact the development team with:
- The `errorNumber` (if available)
- The `timestamp` of the response
- Details of the request that caused the error
- The invoice ID(s) or contract ID involved

