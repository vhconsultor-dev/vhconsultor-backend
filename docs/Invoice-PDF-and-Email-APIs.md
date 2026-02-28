# Invoice PDF Generation and Invoice Email APIs – Documentation

This document describes the **Invoice PDF generation** API and the **Invoice statement email** API: how they work, request format, validations, responses, and error handling. Use it to integrate with these endpoints.

---

## Full API URLs

- **Generate invoice PDF:**  
  **POST** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/generate-pdf`
- **Send invoice email:**  
  **POST** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/send-email`

---

## Base information (both APIs)

- **Base path (API Management):**  
  `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice`
- **Authentication:** Both endpoints require a valid JWT in the `Authorization` header (e.g. `Bearer <token>`). Unauthorized requests return 401.

---

## Common error response structure (JSON)

When an endpoint returns an error (validation, configuration, or server error), the body is JSON with this envelope:

| Field         | Type    | Description |
|---------------|---------|-------------|
| `status`      | boolean | `false` |
| `statusCode`  | number  | 400, 422, 500, etc. |
| `data`        | null    | |
| `message`     | string  | Human-readable error description |
| `errorNumber` | string  | Optional; may be `null` |
| `timestamp`   | string  | Server timestamp |

Use `statusCode` and `message` to determine the type of error and what to show to the user.

---

# 1. Invoice PDF Generation API

## Purpose

Generates an invoice as a PDF using the CraftMyPDF template configured in the backend. The response is the **PDF file** (binary), not JSON. The template is fixed in configuration (`CraftMyPdf:InvoiceTemplateId`).

---

## Endpoint

**POST** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/generate-pdf`

- **Content-Type:** `application/json`
- **Body:** JSON only (no form-data, no query parameters).

---

## Request body (JSON)

The API accepts **camelCase** or **snake_case** for property names (e.g. `invoice_no` or `invoiceNo`). The template expects snake_case internally; the API accepts both.

### Required fields

| Field        | Type   | Description |
|-------------|--------|-------------|
| `items`     | array  | At least one line item. Each item must have `description`, `qty`, `unitprice`. |
| `invoice_no`| string | Invoice number. Cannot be empty. |

### Optional fields

| Field               | Type   | Description |
|---------------------|--------|-------------|
| `company_name`      | string | Company name (sender). |
| `company_address`   | string | Company address. |
| `company_email`     | string | Company email. |
| `bill_to`           | string | Bill-to name (client). |
| `bill_to_address`   | string | Bill-to address. |
| `invoice_date`      | string | Invoice date (any format the template expects). |
| `invoice_due_date`  | string | Due date. |
| `footer`            | string | Footer text. |
| `balance`           | string | Balance / total (e.g. `"10"` or `"4,850.00"`). |
| `logo`              | string | URL of logo image. |
| `currency`          | string | Currency symbol (default `"$"` if omitted). |

### Line item object (each element of `items`)

| Field         | Type    | Required | Description |
|---------------|---------|----------|-------------|
| `description` | string  | Yes      | Line description. Cannot be empty. |
| `qty`         | number  | Yes      | Quantity. Must be ≥ 0. |
| `unitprice`   | number  | Yes      | Unit price. Must be ≥ 0. |

---

## Validations (before calling CraftMyPDF)

- **items:** Must be present and contain at least one element.  
  Error: `"At least one invoice item is required."`
- **items[].description:** Required for each item.  
  Error: `"Item description is required."`
- **items[].qty:** Must be ≥ 0.  
  Error: `"Item quantity must be greater than or equal to zero."`
- **items[].unitprice:** Must be ≥ 0.  
  Error: `"Item unit price must be greater than or equal to zero."`
- **invoice_no:** Required (non-empty).  
  Error: `"Invoice number is required."`

If any validation fails, the API returns **400 Bad Request** with a JSON body: `status: false`, `statusCode: 422`, and `message` containing one or more of the messages above (comma-separated if multiple).

---

## Conditional behavior

1. **Template not configured**  
   If `CraftMyPdf:InvoiceTemplateId` is empty or not set, the API returns **500 Internal Server Error** with JSON body and message:  
   `"Invoice PDF template is not configured. Set CraftMyPdf:InvoiceTemplateId in configuration."`

2. **CraftMyPDF failure**  
   If the external CraftMyPDF service fails (e.g. network, invalid template, bad data), the API returns **500** with a JSON body and a message of the form:  
   `"Error generating invoice PDF: <details>"`

---

## Success response (200 OK)

- **Content-Type:** `application/pdf`
- **Body:** Binary PDF content (not JSON).
- **Content-Disposition:** Typically `attachment; filename=invoice_<invoice_no>_<yyyyMMdd_HHmmss>.pdf`  
  (Spaces in `invoice_no` are replaced by underscores; if `invoice_no` is empty, the name uses `"invoice"`.)

The client should treat the response as a file download (save or display as PDF).

---

## Error responses (JSON body)

| HTTP Status | statusCode in body | When |
|-------------|--------------------|------|
| 400         | 422                | Validation failed (items, invoice_no, or item fields). |
| 500         | 500                | Template not configured, or CraftMyPDF/service error. |

Example validation error (400):

```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "Invoice number is required., At least one invoice item is required.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

Example server error (500):

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Invoice PDF template is not configured. Set CraftMyPdf:InvoiceTemplateId in configuration.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## Example request body (generate PDF)

```json
{
  "items": [
    { "description": "Professional services January 2026", "qty": 1, "unitprice": 4850.00 },
    { "description": "Support fee", "qty": 1, "unitprice": 0 }
  ],
  "company_name": "VH Consultor",
  "company_address": "15A Montebelic, Mercedes Norte, Heredia, Costa Rica",
  "company_email": "facturas@vhconsultor.com",
  "bill_to": "Northwind Trading LLC",
  "bill_to_address": "123 Business Ave, City, Country",
  "invoice_no": "INV-2026-0148",
  "invoice_date": "2026-02-18",
  "invoice_due_date": "2026-03-18",
  "footer": "Thank you for your business",
  "balance": "4,850.00",
  "currency": "USD"
}
```

---

# 2. Invoice Statement Email API

## Purpose

Sends the “Invoice Statement” email to the client using a SendGrid template. The email can include the invoice PDF as an attachment. Recipient and CC are provided in the request (same pattern as the contract email).

---

## Endpoint

**POST** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Invoice/send-email`

- **Content-Type:** `multipart/form-data`
- **Body:** Form fields only (no raw JSON as body; JSON is sent as one of the form fields).

---

## Request (form-data)

Two form fields:

| Field         | Type   | Required | Description |
|---------------|--------|----------|-------------|
| `requestJson` | string | Yes      | JSON string containing `toEmail`, optional `ccEmail`, and `data` (template variables). |
| `pdfFile`     | file   | No       | Invoice PDF file to attach. If sent, must be PDF and ≤ 10 MB. |

The **recipient** is defined inside `requestJson` as `toEmail`. The **CC** list is optional and defined as `ccEmail` inside `requestJson` (comma-separated emails).

---

## Structure of `requestJson` (JSON string)

When building the form, the value of the `requestJson` field must be a **string** whose content is valid JSON with this shape:

| Property   | Type   | Required | Description |
|------------|--------|----------|-------------|
| `toEmail`  | string | Yes      | Recipient email address. Must be a valid email. |
| `ccEmail`  | string | No       | CC recipients. Multiple addresses separated by commas (e.g. `"a@x.com, b@y.com"`). Each must be a valid email. |
| `data`     | object | Yes      | Data for the SendGrid template (see below). |

### `data` object (template variables)

All fields are required when `data` is present (they are validated):

| Field             | Type   | Description |
|-------------------|--------|-------------|
| `clientName`      | string | Client name (e.g. “Jonathan Miller”). |
| `companyName`     | string | Company name (e.g. “Northwind Trading LLC”). |
| `invoiceNumber`   | string | Invoice number (e.g. “INV-2026-0148”). |
| `invoiceMonth`    | string | Billing month (e.g. “January”). |
| `invoiceYear`     | string | Billing year (e.g. “2026”). |
| `issueDate`       | string | Issue date (e.g. “February 18, 2026”). |
| `currency`        | string | Currency code or symbol (e.g. “USD”). |
| `totalAmount`     | string | Total amount, formatted (e.g. “4,850.00”). |

---

## Validations

### 1. Request JSON

- **Parse:** If `requestJson` is not valid JSON, the API returns **400** with message like:  
  `"Invalid request JSON: <details>. Provide a valid JSON with ToEmail, optional CcEmail, and Data."`
- **Presence:** If after parsing the request is null or missing, the API returns **400** with:  
  `"Request data is required. Provide 'requestJson' with ToEmail and Data."`

### 2. FluentValidation (ToEmail, CcEmail, Data)

- **toEmail:** Required, must be a valid email.  
  Errors: `"Recipient email (ToEmail) is required."` or `"Recipient email is not valid."`
- **ccEmail:** If present, must be one valid email or several valid emails separated by commas.  
  Error: `"CC must be a valid email or multiple emails separated by commas."`
- **data:** Required (non-null).  
  Error: `"Invoice email data is required."`
- **data.clientName:** Required.  
  Error: `"Client name is required."`
- **data.companyName:** Required.  
  Error: `"Company name is required."`
- **data.invoiceNumber:** Required.  
  Error: `"Invoice number is required."`
- **data.invoiceMonth:** Required.  
  Error: `"Invoice month is required."`
- **data.invoiceYear:** Required.  
  Error: `"Invoice year is required."`
- **data.issueDate:** Required.  
  Error: `"Issue date is required."`
- **data.currency:** Required.  
  Error: `"Currency is required."`
- **data.totalAmount:** Required.  
  Error: `"Total amount is required."`

If any of these fail, the API returns **400 Bad Request** with a JSON body: `status: false`, `statusCode: 422`, and `message` containing one or more messages (comma-separated if multiple).

### 3. PDF attachment (when `pdfFile` is sent)

- **File type:** Only `.pdf` is allowed.  
  If the file has another extension, the API returns **400** with message:  
  `"Only PDF attachments are allowed. File '<filename>' has extension '<ext>'."`
- **File size:** Maximum 10 MB (10,485,760 bytes).  
  If exceeded, the API returns **400** with message:  
  `"PDF is too large. Maximum size: 10 MB. File size: <x> MB."`

If both validations pass, the file is attached to the email. **Sending the PDF is optional**; the email can be sent without attachment.

---

## Conditional behavior

1. **Template not configured**  
   If `SendGrid:InvoiceStatementTemplateId` is empty or not set, the API returns **500** with JSON message:  
   `"Invoice statement email template is not configured. Set SendGrid:InvoiceStatementTemplateId in configuration."`

2. **SendGrid failure**  
   If SendGrid rejects the request or returns an error, the API returns **500** with a JSON body and message of the form:  
   `"Failed to send invoice email: <message>. Details: <errorDetails>"`

3. **Other exceptions**  
   Any unhandled exception returns **500** with message:  
   `"Error sending invoice email: <details>"`

---

## Success response (200 OK)

**Content-Type:** `application/json`

Body is the standard success envelope with `data` containing the send result:

```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Invoice email sent successfully."
  },
  "message": "Invoice email sent successfully.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## Error responses (JSON body)

| HTTP Status | statusCode in body | When |
|-------------|--------------------|------|
| 400         | 422                | Invalid or missing `requestJson`, or validation failed (ToEmail, CcEmail, Data fields). |
| 400         | 422                | PDF attachment invalid (wrong extension or > 10 MB). |
| 500         | 500                | Template not configured, SendGrid error, or server exception. |

Example validation error (400):

```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "Recipient email (ToEmail) is required., Client name is required.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

Example PDF error (400):

```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "Only PDF attachments are allowed. File 'document.docx' has extension '.docx'.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

Example server/template error (500):

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Invoice statement email template is not configured. Set SendGrid:InvoiceStatementTemplateId in configuration.",
  "errorNumber": null,
  "timestamp": "2026-02-07T12:00:00"
}
```

---

## Example: building the send-email request

1. **Build the JSON object** (then serialize it to string for `requestJson`):

```json
{
  "toEmail": "client@northwind.com",
  "ccEmail": "billing@vhconsultor.com, admin@northwind.com",
  "data": {
    "clientName": "Jonathan Miller",
    "companyName": "Northwind Trading LLC",
    "invoiceNumber": "INV-2026-0148",
    "invoiceMonth": "January",
    "invoiceYear": "2026",
    "issueDate": "February 18, 2026",
    "currency": "USD",
    "totalAmount": "4,850.00"
  }
}
```

2. **Form-data:**
   - Field name `requestJson`, value = the above JSON **as a string**.
   - Field name `pdfFile`, value = the invoice PDF file (optional).

3. **Typical flow:** Call the **generate-pdf** API to obtain the PDF, then call **send-email** with that PDF as `pdfFile` and the same (or matching) invoice data in `requestJson.data`.

---

## Summary

| API              | Method | Content-Type        | Purpose |
|------------------|--------|---------------------|--------|
| Generate PDF     | POST   | application/json    | Get invoice as PDF file (binary response). |
| Send email       | POST   | multipart/form-data | Send invoice statement email (optional PDF attachment); recipient and CC in `requestJson`. |

- **Generate PDF:** Required body fields are `items` (at least one item with `description`, `qty`, `unitprice`) and `invoice_no`. Success = 200 with binary PDF; errors = 400 (validation) or 500 (config/CraftMyPDF).
- **Send email:** Required form field `requestJson` (with `toEmail` and `data`); optional `pdfFile` (PDF, max 10 MB). Success = 200 with JSON; errors = 400 (parse/validation/PDF) or 500 (config/SendGrid/server).

This document describes only the behavior and usage of these APIs, not how to implement the client.
