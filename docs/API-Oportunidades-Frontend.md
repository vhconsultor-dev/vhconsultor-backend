# Corporate Opportunities API - Frontend Documentation

## Overview

This document provides comprehensive API documentation for the Corporate Opportunities module. All endpoints require JWT authentication via the `Authorization: Bearer <token>` header.

**Base URL REST (APIM):** `https://vh-apimanagement.azure-api.net/corporate-vh`

**Base URL SignalR (App Service directo):** `https://vh-backend-app-g4hydde8hnepf6bn.canadacentral-01.azurewebsites.net`

**API Prefix (REST):** `/api/corporate/Opportunity`

**Hub path (SignalR):** `/hubs/opportunities`

**Full REST URL pattern:** `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/...`

**Full SignalR URL:** `https://vh-backend-app-g4hydde8hnepf6bn.canadacentral-01.azurewebsites.net/hubs/opportunities`

> SignalR **no pasa por API Management**. El frontend usa APIM para REST y se conecta directo al App Service para tiempo real (WebSockets).

**Authentication:** `Authorization: Bearer <token>` on every REST request. SignalR uses the same JWT via `accessTokenFactory`.

**Date Format:** All dates are in UTC ISO 8601 format (e.g., `2026-05-31T14:30:00Z`)

**Response Structure:** All responses follow the standard format:
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... }
}
```

---

## Endpoint Catalog (APIM)

| # | Method | URL |
|---|--------|-----|
| 1 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity` |
| 2 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{id}` |
| 3 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/count/{status}` |
| 4 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/convert-from-lead` |
| 5 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/stages` |
| 6 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/lost-reasons` |
| 7 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/follow-ups` |
| 8 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/{followUpId}` |
| 9 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/pending` |
| 10 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/{followUpId}/attachments` |
| 11 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/follow-ups` |
| 12 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/{followUpId}/complete` |
| 13 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/change-stage` |
| 14 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/mark-as-won` |
| 15 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/mark-as-lost` |
| 16 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/comments` |
| 17 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}` |
| 18 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}/attachments` |
| 19 | `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}/mentions` |
| 20 | `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/comments` |
| 21 | `PUT` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}` |
| 22 | `DELETE` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}` |

**SignalR Hub (WebSocket, App Service directo):** `https://vh-backend-app-g4hydde8hnepf6bn.canadacentral-01.azurewebsites.net/hubs/opportunities`

> **Note:** `convert-from-lead` is **POST**, not GET. SignalR is **not** registered in APIM.

---

## Table of Contents

1. [Opportunities](#opportunities)
2. [Follow-Ups](#follow-ups)
3. [Stages & Lost Reasons](#stages--lost-reasons)
4. [Comments & Mentions](#comments--mentions)
5. [SignalR Real-Time Notifications](#signalr-real-time-notifications)
6. [Models](#models)
7. [Error Handling](#error-handling)

---

## Opportunities

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity

Get all opportunities with optional filters.

**Query Parameters:**
- `status` (string, optional): Filter by status (`Open`, `Won`, `Lost`)
- `stageKey` (string, optional): Filter by stage key (e.g., `first_contact`)
- `assignedToUserId` (int, optional): Filter by assigned user ID
- `viewerUserId` (int, optional): Filter by viewer user ID
- `mine` (bool, optional): Show only my opportunities (assigned or viewer)
- `search` (string, optional): Search in name, email, brand, title
- `fromDate` (datetime, optional): Filter from conversion date
- `toDate` (datetime, optional): Filter to conversion date

**Example Request:**
```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity?mine=true&status=Open
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "message": "Opportunities retrieved successfully.",
  "data": [
    {
      "opportunityId": 1,
      "submissionId": 42,
      "status": "Open",
      "currentStageKey": "first_contact",
      "title": "Acme Corp",
      "firstName": "John",
      "lastName": "Doe",
      "email": "john@example.com",
      "phoneNumber": "+1234567890",
      "country": "USA",
      "brandName": "Acme Corp",
      "numberOfListings": 50,
      "assignedToUserId": 10,
      "viewerUserId": 5,
      "convertedAt": "2026-05-31T14:30:00Z",
      "createdAt": "2026-05-31T14:30:00Z"
    }
  ]
}
```

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{id}

Get a single opportunity by ID.

**Path Parameters:**
- `id` (int, required): Opportunity ID

**Example Request:**
```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/1
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "message": "Opportunity retrieved successfully.",
  "data": {
    "opportunityId": 1,
    "status": "Open",
    "currentStageKey": "first_contact",
    "title": "Acme Corp",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "brandName": "Acme Corp",
    "assignedToUserId": 10,
    "viewerUserId": 5,
    "convertedAt": "2026-05-31T14:30:00Z"
  }
}
```

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/count/{status}

Get count of opportunities by status.

**Path Parameters:**
- `status` (string, required): Status to count (`Open`, `Won`, `Lost`)

**Example Request:**
```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/count/Open
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "message": "Count retrieved successfully.",
  "data": {
    "count": 25
  }
}
```

---

### POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/convert-from-lead

Convert a lead to an opportunity.

**Request Body:**
```json
{
  "submissionId": 42,
  "assignedToUserId": 10,
  "viewerUserId": 5
}
```

**Validation Rules:**
- `submissionId`: Required, must be > 0, lead must exist and not be already converted
- `assignedToUserId`: Required, must be > 0, must be active corporate user
- `viewerUserId`: Optional, if provided must be > 0 and active corporate user

**Example Response:**
```json
{
  "success": true,
  "message": "Lead converted to opportunity successfully.",
  "data": {
    "opportunityId": 1
  }
}
```

**Notes:**
- Automatically creates a "first contact" follow-up due today at 11:59 PM UTC
- Sets opportunity status to `Open` and stage to `first_contact`
- Copies all contact data from the lead

---

### POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/change-stage

Change the current stage of an opportunity.

**Path Parameters:**
- `opportunityId` (int, required): Opportunity ID

**Request Body:**
```json
{
  "stageKey": "proposal_sent"
}
```

**Validation Rules:**
- `stageKey`: Required, max 50 chars, must be an active stage

**Example Response:**
```json
{
  "success": true,
  "message": "Opportunity stage changed successfully.",
  "data": null
}
```

---

### POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/mark-as-won

Mark an opportunity as won and link to customer/contract.

**Path Parameters:**
- `opportunityId` (int, required): Opportunity ID

**Request Body:**
```json
{
  "customerId": 123,
  "contractId": 456
}
```

**Validation Rules:**
- `customerId`: Required, must be > 0, customer must exist and be active
- `contractId`: Optional, if provided must be > 0 and belong to the customer

**Example Response:**
```json
{
  "success": true,
  "message": "Opportunity marked as won successfully.",
  "data": null
}
```

**Notes:**
- Updates opportunity status to `Won`
- Links contract to opportunity if provided
- Records `wonAt` and `wonByUserId`

---

### POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/mark-as-lost

Mark an opportunity as lost with a reason.

**Path Parameters:**
- `opportunityId` (int, required): Opportunity ID

**Request Body:**
```json
{
  "lostReasonKey": "price_too_high",
  "lostReasonNotes": "Customer found a cheaper alternative"
}
```

**Validation Rules:**
- `lostReasonKey`: Required, max 50 chars, must be an active lost reason
- `lostReasonNotes`: Optional, max 500 chars

**Example Response:**
```json
{
  "success": true,
  "message": "Opportunity marked as lost successfully.",
  "data": null
}
```

**Notes:**
- Updates opportunity status to `Lost`
- Records `lostAt` and `lostByUserId`

---

## Follow-Ups

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/follow-ups

Get all follow-ups for an opportunity.

**Path Parameters:**
- `opportunityId` (int, required): Opportunity ID

**Example Response:**
```json
{
  "success": true,
  "message": "Follow-ups retrieved successfully.",
  "data": [
    {
      "followUpId": 1,
      "opportunityId": 1,
      "stageKey": "first_contact",
      "status": "Pending",
      "dueAt": "2026-05-31T23:59:59Z",
      "isRequired": true,
      "createdAt": "2026-05-31T14:30:00Z"
    }
  ]
}
```

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/{followUpId}

Get a follow-up by ID.

**Path Parameters:**
- `followUpId` (int, required): Follow-up ID

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/pending

Get pending follow-ups for dashboard alerts.

**Query Parameters:**
- `userId` (int, optional): Filter by user ID (defaults to current user)

**Example Response:**
```json
{
  "success": true,
  "message": "Pending follow-ups retrieved successfully.",
  "data": [
    {
      "followUpId": 1,
      "opportunityId": 1,
      "opportunityTitle": "Acme Corp",
      "stageKey": "first_contact",
      "stageDisplayName": "First contact",
      "dueAt": "2026-05-31T23:59:59Z",
      "isRequired": true,
      "assignedToUserId": 10
    }
  ]
}
```

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/{followUpId}/attachments

Get attachments for a follow-up.

**Path Parameters:**
- `followUpId` (int, required): Follow-up ID

**Example Response:**
```json
{
  "success": true,
  "message": "Attachments retrieved successfully.",
  "data": [
    {
      "followUpAttachmentId": 1,
      "followUpId": 1,
      "fileUrl": "https://blob.azure.com/...",
      "fileName": "email-proof.pdf",
      "contentType": "application/pdf",
      "uploadedBy": 10,
      "uploadedAt": "2026-05-31T15:00:00Z"
    }
  ]
}
```

---

### POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/follow-ups

Create a manual follow-up.

**Path Parameters:**
- `opportunityId` (int, required): Opportunity ID

**Request Body:**
```json
{
  "stageKey": "proposal_sent",
  "dueAt": "2026-06-15T17:00:00Z",
  "notes": "Send follow-up email about proposal"
}
```

**Validation Rules:**
- `stageKey`: Optional, max 50 chars
- `dueAt`: Optional
- `notes`: Optional, max 500 chars

**Example Response:**
```json
{
  "success": true,
  "message": "Follow-up created successfully.",
  "data": {
    "followUpId": 2
  }
}
```

---

### POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/follow-ups/{followUpId}/complete

Complete a follow-up with evidence attachments.

**Path Parameters:**
- `followUpId` (int, required): Follow-up ID

**Request Body (multipart/form-data):**
- `notes` (string, optional): Notes about completion
- `files` (file[], required): At least one attachment file

**Validation Rules:**
- At least one file is required
- Allowed file types: `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.txt`, `.csv`, `.xls`, `.xlsx`
- Max file size: 10 MB per file

**Example Response:**
```json
{
  "success": true,
  "message": "Follow-up completed successfully.",
  "data": {
    "followUpId": 1,
    "attachmentCount": 2
  }
}
```

**Notes:**
- Updates follow-up status to `Completed`
- Uploads files to Azure Blob Storage
- Records `completedAt` and `completedByUserId`

---

## Stages & Lost Reasons

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/stages

Get all active opportunity stages (catalog).

**Example Response:**
```json
{
  "success": true,
  "message": "Stages retrieved successfully.",
  "data": [
    {
      "opportunityStageId": 1,
      "stageKey": "first_contact",
      "displayName": "First contact",
      "sortOrder": 10,
      "defaultChecklistText": "Contact the client on the same day as conversion.",
      "isActive": true
    },
    {
      "opportunityStageId": 2,
      "stageKey": "needs_assessment",
      "displayName": "Needs assessment",
      "sortOrder": 20,
      "defaultChecklistText": "Schedule discovery call to understand requirements.",
      "isActive": true
    }
  ]
}
```

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/lost-reasons

Get all active lost reasons (catalog).

**Example Response:**
```json
{
  "success": true,
  "message": "Lost reasons retrieved successfully.",
  "data": [
    {
      "opportunityLostReasonId": 1,
      "reasonKey": "price_too_high",
      "displayName": "Price too high",
      "sortOrder": 10,
      "isActive": true
    },
    {
      "opportunityLostReasonId": 2,
      "reasonKey": "no_response",
      "displayName": "No response from client",
      "sortOrder": 20,
      "isActive": true
    }
  ]
}
```

---

## Comments & Mentions

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/comments

Get all comments for an opportunity.

**Path Parameters:**
- `opportunityId` (int, required): Opportunity ID

**Example Response:**
```json
{
  "success": true,
  "message": "Comments retrieved successfully.",
  "data": [
    {
      "commentId": 1,
      "opportunityId": 1,
      "authorUserId": 10,
      "body": "Client is interested, scheduling follow-up call.",
      "createdAt": "2026-05-31T15:30:00Z",
      "updatedAt": null
    }
  ]
}
```

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}

Get a comment by ID.

**Path Parameters:**
- `commentId` (int, required): Comment ID

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}/attachments

Get attachments for a comment.

**Path Parameters:**
- `commentId` (int, required): Comment ID

---

### GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}/mentions

Get mentions for a comment.

**Path Parameters:**
- `commentId` (int, required): Comment ID

**Example Response:**
```json
{
  "success": true,
  "message": "Mentions retrieved successfully.",
  "data": [
    {
      "commentMentionId": 1,
      "commentId": 1,
      "mentionedUserId": 5,
      "createdAt": "2026-05-31T15:30:00Z"
    }
  ]
}
```

---

### POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/{opportunityId}/comments

Create a comment with optional attachments and @mentions.

**Path Parameters:**
- `opportunityId` (int, required): Opportunity ID

**Request Body (multipart/form-data):**
- `body` (string, required): Comment text (max 2000 chars)
- `files` (file[], optional): Optional attachment files

**Validation Rules:**
- `body`: Required, max 2000 chars
- Files: Same rules as follow-up attachments (10 MB max, allowed extensions)

**@Mention Syntax:**
Use `@userId` to mention a user. Example: `@123` will mention user with ID 123.

**Example Request Body:**
```
body: "Hi @5, please review this opportunity. The client is interested in our services."
files: [screenshot.png]
```

**Example Response:**
```json
{
  "success": true,
  "message": "Comment created successfully.",
  "data": {
    "commentId": 1,
    "mentionedUserIds": [5],
    "attachmentCount": 1
  }
}
```

**Notes:**
- Mentioned users receive email notifications
- Mentioned users receive SignalR real-time notifications (if connected)
- Only active corporate users can be mentioned

---

### PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}

Update a comment (only author can update).

**Path Parameters:**
- `commentId` (int, required): Comment ID

**Request Body:**
```json
{
  "body": "Updated comment text"
}
```

**Validation Rules:**
- `body`: Required, max 2000 chars
- Only the author can update their own comments

---

### DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Opportunity/comments/{commentId}

Delete a comment (only author can delete).

**Path Parameters:**
- `commentId` (int, required): Comment ID

**Notes:**
- Only the author can delete their own comments
- Deletes associated mentions and attachment records (files remain in Azure)

---

## SignalR Real-Time Notifications

SignalR connects **directly to App Service**, not through APIM.

**Hub URL:** `https://vh-backend-app-g4hydde8hnepf6bn.canadacentral-01.azurewebsites.net/hubs/opportunities`

**Connection:** Use SignalR JavaScript/TypeScript client library.

```javascript
import * as signalR from "@microsoft/signalr";

const SIGNALR_HUB_URL =
  "https://vh-backend-app-g4hydde8hnepf6bn.canadacentral-01.azurewebsites.net/hubs/opportunities";

const connection = new signalR.HubConnectionBuilder()
  .withUrl(SIGNALR_HUB_URL, {
    accessTokenFactory: () => getAccessToken() // same Corporate JWT as REST APIs
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Hub Methods (Client → Server)

#### JoinOpportunityRoom(opportunityId)
Join a specific opportunity room to receive updates for that opportunity.

```javascript
await connection.invoke("JoinOpportunityRoom", 1);
```

#### LeaveOpportunityRoom(opportunityId)
Leave a specific opportunity room.

```javascript
await connection.invoke("LeaveOpportunityRoom", 1);
```

#### JoinUserOpportunitiesRoom(userId)
Join all opportunities for a user (assigned or viewer).

```javascript
await connection.invoke("JoinUserOpportunitiesRoom", 10);
```

#### LeaveUserOpportunitiesRoom(userId)
Leave all opportunities for a user.

```javascript
await connection.invoke("LeaveUserOpportunitiesRoom", 10);
```

---

### Server → Client Events

#### ReceiveCommentNotification
Triggered when a new comment is created on an opportunity.

```javascript
connection.on("ReceiveCommentNotification", (notification) => {
  console.log("New comment:", notification);
  // {
  //   type: "CommentCreated",
  //   opportunityId: 1,
  //   commentId: 1,
  //   authorUserId: 10,
  //   authorName: "John Doe",
  //   commentBody: "Client is interested...",
  //   mentionedUserIds: [5],
  //   timestamp: "2026-05-31T15:30:00Z"
  // }
});
```

#### ReceiveMentionNotification
Triggered when a user is @mentioned in a comment.

```javascript
connection.on("ReceiveMentionNotification", (notification) => {
  console.log("You were mentioned:", notification);
  // Same structure as ReceiveCommentNotification
});
```

#### ReceiveStatusChangeNotification
Triggered when an opportunity status changes (Won/Lost).

```javascript
connection.on("ReceiveStatusChangeNotification", (notification) => {
  console.log("Status changed:", notification);
  // {
  //   type: "StatusChanged",
  //   opportunityId: 1,
  //   newStatus: "Won",
  //   changedByUserId: 10,
  //   changedByName: "John Doe",
  //   timestamp: "2026-05-31T15:30:00Z"
  // }
});
```

#### ReceiveStageChangeNotification
Triggered when an opportunity stage changes.

```javascript
connection.on("ReceiveStageChangeNotification", (notification) => {
  console.log("Stage changed:", notification);
  // {
  //   type: "StageChanged",
  //   opportunityId: 1,
  //   newStageKey: "proposal_sent",
  //   newStageDisplayName: "Proposal sent",
  //   timestamp: "2026-05-31T15:30:00Z"
  // }
});
```

#### ReceiveFollowUpNotification
Triggered when a follow-up is completed.

```javascript
connection.on("ReceiveFollowUpNotification", (notification) => {
  console.log("Follow-up completed:", notification);
  // {
  //   type: "FollowUpCompleted",
  //   opportunityId: 1,
  //   followUpId: 1,
  //   completedByUserId: 10,
  //   completedByName: "John Doe",
  //   timestamp: "2026-05-31T15:30:00Z"
  // }
});
```

---

## Models

### Opportunity

```typescript
interface Opportunity {
  opportunityId: number;
  submissionId?: number;
  status: "Open" | "Won" | "Lost";
  currentStageKey?: string;
  title: string;
  
  // Contact data
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  country: string;
  brandName: string;
  numberOfListings: number;
  productPageLink?: string;
  storeLink?: string;
  selectedPlatform?: string;
  accountType?: string;
  serviceType?: string;
  annualSalesRange?: string;
  advertisingBudgetRange?: string;
  promotionalBudgetRange?: string;
  additionalDetails?: string;
  
  // Assignment
  assignedToUserId: number;
  viewerUserId?: number;
  
  // Won
  customerId?: number;
  contractId?: number;
  wonAt?: string;
  wonByUserId?: number;
  
  // Lost
  lostAt?: string;
  lostByUserId?: number;
  lostReasonKey?: string;
  lostReasonNotes?: string;
  
  // Audit
  convertedAt: string;
  convertedByUserId?: number;
  createdAt: string;
  updatedAt?: string;
}
```

### Follow-Up

```typescript
interface FollowUp {
  followUpId: number;
  opportunityId: number;
  stageKey: string;
  status: "Pending" | "Completed" | "Cancelled";
  dueAt?: string;
  completedAt?: string;
  completedByUserId?: number;
  notes?: string;
  isRequired: boolean;
  createdAt: string;
  updatedAt?: string;
}
```

### Comment

```typescript
interface Comment {
  commentId: number;
  opportunityId: number;
  authorUserId: number;
  body: string;
  createdAt: string;
  updatedAt?: string;
}
```

### Stage

```typescript
interface Stage {
  opportunityStageId: number;
  stageKey: string;
  displayName: string;
  sortOrder: number;
  defaultChecklistText?: string;
  isActive: boolean;
  createdAt: string;
}
```

### Lost Reason

```typescript
interface LostReason {
  opportunityLostReasonId: number;
  reasonKey: string;
  displayName: string;
  sortOrder: number;
  isActive: boolean;
  createdAt: string;
}
```

---

## Error Handling

All errors follow this structure:

```json
{
  "success": false,
  "message": "Error description. Error ID: ERR-123456.",
  "data": null
}
```

**HTTP Status Codes:**
- `200 OK`: Success
- `400 Bad Request`: Validation error or invalid operation
- `401 Unauthorized`: Missing or invalid JWT token
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error (check `Error ID` in logs)

**Validation Errors:**
```json
{
  "success": false,
  "message": "Opportunity ID must be greater than 0., Stage key is required.",
  "data": null
}
```

**Business Logic Errors:**
```json
{
  "success": false,
  "message": "Lead with ID 42 has already been converted to Opportunity ID 10.",
  "data": null
}
```

---

## Best Practices

1. **Date Handling:** All dates are UTC. Convert to local time in the frontend.
2. **File Uploads:** Use `multipart/form-data` content type for file uploads.
3. **@Mentions:** Use `@userId` syntax (e.g., `@123`) to mention users in comments.
4. **SignalR:** Connect to the App Service hub URL on app startup (not APIM) and join relevant rooms.
5. **Dual base URLs:** REST → APIM (`corporate-vh`); SignalR → App Service (`vh-backend-app-...azurewebsites.net`).
6. **Error Handling:** Always check `success` field in responses. Display `message` to users.
7. **Caching:** Cache stages and lost reasons catalogs (they rarely change).
8. **Polling:** Use pending follow-ups endpoint to show alerts in dashboard.
9. **Real-Time:** Use SignalR for real-time updates instead of polling.

---

## Support

For issues or questions, contact the backend team or check the [Corporate-Oportunidades-Fases-Tecnico.md](./Corporate-Oportunidades-Fases-Tecnico.md) document for technical implementation details.

**Document Version:** 1.2  
**Last Updated:** 2026-05-31
