# API Documentation - Brand Partner Authentication

Complete and detailed documentation for Brand Partner authentication endpoints (excluding user creation).

---

## Base URL

```
https://vh-apimanagement.azure-api.net/brandparthner-vh
```

---

## Common Response Structure

All endpoints return responses in this standard format:

```json
{
    "status": true,
    "statusCode": 200,
    "data": { ... },
    "message": "Operation completed successfully",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `status` | boolean | `true` for success, `false` for errors |
| `statusCode` | integer | HTTP-style status code (200, 400, 401, 500) |
| `data` | object/null | Response payload (null on errors) |
| `message` | string | Human-readable message in English |
| `errorNumber` | integer/null | Optional error code for specific errors |
| `timestamp` | string | Server timestamp in ISO 8601 format |

---

## Authentication Flow

Brand Partner login uses **Two-Factor Authentication (2FA)**:

1. **Step 1: Login** - Submit email/password → Receive "2FA code sent"
2. **Step 2: Verify 2FA** - Submit email + 5-character code → Receive JWT token

---

## Endpoints Overview

| # | Endpoint | Method | Auth Required | Description |
|---|----------|--------|---------------|-------------|
| 1 | `/api/brandpartner/auth/login` | POST | No | Login Step 1: Validate credentials & send 2FA code |
| 2 | `/api/brandpartner/auth/verify-2fa` | POST | No | Login Step 2: Verify 2FA code & get JWT |
| 3 | `/api/brandpartner/auth/reset-password` | POST | No | Send temporary password via email |
| 4 | `/api/brandpartner/auth/change-password` | POST | Yes (BrandPartner) | Change password |
| 5 | `/api/brandpartner/auth/users` | GET | Yes (Admin/Corporate) | List users by customer |
| 6 | `/api/brandpartner/auth/login-history` | GET | Yes (All roles) | Get login history |

---

## 1. Login (Step 1) - Send 2FA Code

### Endpoint Details

**URL:** `POST /api/brandpartner/auth/login`  
**Authorization:** None required (anonymous)  
**Purpose:** Validates user credentials (email/password). If valid, generates a 5-character 2FA code and sends it to the user's email. The code expires in 3 minutes.

### Request Body

```json
{
    "email": "user@example.com",
    "password": "SecurePass123"
}
```

#### Required Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `email` | string | **Yes** | Must be valid email format | User's registered email address (case-insensitive) |
| `password` | string | **Yes** | Cannot be empty | User's password (sent in plain text, secured via HTTPS) |

#### Optional Fields (Auto-populated by backend)

The following fields are automatically captured from the HTTP request context:

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| `ipAddress` | string | `HttpContext.Connection.RemoteIpAddress` | Client IP address (e.g., "192.168.1.100") |
| `userAgent` | string | `Request.Headers["User-Agent"]` | Browser/client identifier (e.g., "Mozilla/5.0...") |
| `location` | string | Optional (can be sent manually) | Geographic location |
| `country` | string | Optional (can be sent manually) | Country name |
| `city` | string | Optional (can be sent manually) | City name |

**Note:** You **do not need to send** `ipAddress`, `userAgent`, `location`, `country`, or `city` in the request body. The backend extracts `ipAddress` and `userAgent` automatically from the request headers.

### Success Response (200)

```json
{
    "status": true,
    "statusCode": 200,
    "data": {
        "requiresTwoFactor": true
    },
    "message": "2FA code sent to your email. Please check your inbox.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

### Error Responses

#### Validation Error - Missing Email (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Email is required",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

#### Validation Error - Invalid Email Format (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Invalid email format",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

#### Validation Error - Missing Password (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Password is required",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

#### Invalid Credentials (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Invalid email or password.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

**Note:** For security, this generic message is returned for both "user not found" and "incorrect password" scenarios.

#### Account Inactive (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Account is inactive. Please contact support.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

#### Account Locked (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Account is locked until 2026-02-28 15:30:00",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

**Note:** Accounts lock automatically after 5 failed login attempts for 30 minutes.

#### Too Many Failed Attempts (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Too many failed attempts. Account locked for 30 minutes.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

#### Email Sending Failed (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Failed to send verification code. Please check your email configuration or contact support.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

**Note:** This error occurs when SendGrid fails to send the email (e.g., misconfigured API key, invalid template ID).

#### Unexpected Server Error (500)

```json
{
    "status": false,
    "statusCode": 500,
    "data": null,
    "message": "An unexpected error occurred. Please try again later or contact support if the problem persists.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:30:00.0000000"
}
```

### 2FA Code Format

The 2FA code sent via email has this format:
- **1 uppercase letter** (A-Z)
- **4 random digits** (0-9)
- Example: `A1234`, `Z9876`, `M4582`

The code expires **3 minutes** after generation.

---

## 2. Verify 2FA Code (Step 2) - Get JWT Token

### Endpoint Details

**URL:** `POST /api/brandpartner/auth/verify-2fa`  
**Authorization:** None required (anonymous)  
**Purpose:** Validates the 2FA code sent to the user's email. If valid, resets failed login attempts, records successful login, and returns a JWT token for authenticated API requests.

### Request Body

```json
{
    "email": "user@example.com",
    "code": "A1234"
}
```

#### Required Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `email` | string | **Yes** | Must be valid email format | Same email used in login step 1 |
| `code` | string | **Yes** | Must match `^[A-Z][0-9]{4}$` (1 uppercase letter + 4 digits) | The 5-character 2FA code received via email |

#### Optional Fields (Auto-populated by backend)

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| `ipAddress` | string | `HttpContext.Connection.RemoteIpAddress` | Client IP address |
| `userAgent` | string | `Request.Headers["User-Agent"]` | Browser/client identifier |
| `sessionId` | string | `Guid.NewGuid()` | Unique session identifier (auto-generated) |

**Note:** You **only need to send** `email` and `code`. The backend handles `ipAddress`, `userAgent`, and `sessionId` automatically.

### Success Response (200)

```json
{
    "status": true,
    "statusCode": 200,
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbmMiOiJBRVMyNTYiLCJwYXlsb2FkIjoiYmFzZTY0X2VuY3J5cHRlZF9kYXRhIn0.signature",
        "requiresPasswordChange": false,
        "user": {
            "brandPartnerUserId": 5,
            "customerId": 2,
            "email": "user@example.com",
            "firstName": "John",
            "lastName": "Doe",
            "phoneNumber": "+506 8888-8888",
            "isActive": true,
            "emailVerified": true,
            "requirePasswordChangeOnNextLogin": false
        }
    },
    "message": "Login successful.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

#### Response Data Fields

| Field | Type | Description |
|-------|------|-------------|
| `token` | string | JWT Bearer token (use in `Authorization` header for authenticated requests) |
| `requiresPasswordChange` | boolean | If `true`, user must change password before accessing other features |
| `user` | object | User profile information |
| `user.brandPartnerUserId` | integer | Unique user ID |
| `user.customerId` | integer | Customer (company) ID this user belongs to |
| `user.email` | string | User's email address (lowercase) |
| `user.firstName` | string | User's first name |
| `user.lastName` | string | User's last name |
| `user.phoneNumber` | string/null | User's phone number (optional) |
| `user.isActive` | boolean | Account active status |
| `user.emailVerified` | boolean | Email verification status |
| `user.requirePasswordChangeOnNextLogin` | boolean | Same as `requiresPasswordChange` (for backward compatibility) |

### Error Responses

#### Validation Error - Missing Email (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Email is required",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

#### Validation Error - Invalid Email Format (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Invalid email format",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

#### Validation Error - Missing Code (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "2FA code is required",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

#### Validation Error - Invalid Code Format (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Invalid 2FA code format. Expected format: A1234 (1 uppercase letter + 4 digits)",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

#### Invalid or Expired Code (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Invalid or expired 2FA code.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

**Note:** This error occurs when:
- The code doesn't match the one sent
- The code has expired (>3 minutes since generation)
- The code has already been used

#### Invalid Email (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Invalid email.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

#### Unexpected Server Error (500)

```json
{
    "status": false,
    "statusCode": 500,
    "data": null,
    "message": "An unexpected error occurred. Please try again later or contact support if the problem persists.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:32:00.0000000"
}
```

---

## 3. Reset Password (Forgot Password)

### Endpoint Details

**URL:** `POST /api/brandpartner/auth/reset-password`  
**Authorization:** None required (anonymous)  
**Purpose:** Generates a temporary password and sends it to the user's email. Sets `requirePasswordChangeOnNextLogin = true` to force the user to change the password upon next login.

### Request Body

```json
{
    "email": "user@example.com"
}
```

#### Required Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `email` | string | **Yes** | Must be valid email format | User's registered email address |

### Success Response (200)

```json
{
    "status": true,
    "statusCode": 200,
    "data": null,
    "message": "A temporary password has been sent to your email.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:35:00.0000000"
}
```

**Note:** For security reasons, this response is returned **even if the email doesn't exist** in the system. This prevents attackers from discovering valid email addresses.

### Error Responses

#### Validation Error - Missing Email (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Email is required",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:35:00.0000000"
}
```

#### Validation Error - Invalid Email Format (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Invalid email format",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:35:00.0000000"
}
```

#### Account Inactive (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Account is inactive.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:35:00.0000000"
}
```

**Note:** This error is only returned for **existing but inactive** accounts.

#### Unexpected Server Error (500)

```json
{
    "status": false,
    "statusCode": 500,
    "data": null,
    "message": "An unexpected error occurred. Please try again later or contact support if the problem persists.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:35:00.0000000"
}
```

### Temporary Password Format

The temporary password is generated with:
- **Minimum 12 characters**
- **At least 1 uppercase letter** (A-Z)
- **At least 1 lowercase letter** (a-z)
- **At least 1 digit** (0-9)
- **At least 1 special character** (!@#$%^&*)
- Example: `Xk7!mPq2@nLt`

---

## 4. Change Password

### Endpoint Details

**URL:** `POST /api/brandpartner/auth/change-password`  
**Authorization:** **Required** - JWT Bearer token with `BrandPartner` role  
**Purpose:** Allows users to change their password at any time or when forced by `requirePasswordChangeOnNextLogin` flag.

### Request Headers

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Request Body

```json
{
    "oldPassword": "OldSecurePass123",
    "newPassword": "NewSecurePass456"
}
```

#### Required Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `oldPassword` | string | **Yes** | Cannot be empty | User's current password |
| `newPassword` | string | **Yes** | Min 8 chars, 1 uppercase, 1 lowercase, 1 digit, different from old | New password |

**Note:** `brandPartnerUserId` is **automatically extracted** from the JWT token's `UserId` claim. You do **not** need to send it in the request body.

#### Password Validation Rules

The `newPassword` must meet these requirements:

| Rule | Description | Example Error |
|------|-------------|---------------|
| **Min Length** | At least 8 characters | "New password must be at least 8 characters" |
| **Uppercase** | At least 1 uppercase letter (A-Z) | "New password must contain at least one uppercase letter" |
| **Lowercase** | At least 1 lowercase letter (a-z) | "New password must contain at least one lowercase letter" |
| **Digit** | At least 1 digit (0-9) | "New password must contain at least one digit" |
| **Different** | Must be different from `oldPassword` | "New password must be different from the old password" |

### Success Response (200)

```json
{
    "status": true,
    "statusCode": 200,
    "data": null,
    "message": "Password changed successfully.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

**Note:** After successful password change, `requirePasswordChangeOnNextLogin` is automatically set to `false`.

### Error Responses

#### Validation Error - Missing Old Password (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Old password is required",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

#### Validation Error - Missing New Password (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "New password is required",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

#### Validation Error - Multiple Rules Failed (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "New password must be at least 8 characters; New password must contain at least one uppercase letter; New password must contain at least one digit",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

**Note:** Multiple validation errors are separated by `;`.

#### Incorrect Old Password (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "Current password is incorrect.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

#### User Not Found (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "User not found.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

#### Missing or Invalid JWT Token (401)

```json
{
    "status": false,
    "statusCode": 401,
    "data": null,
    "message": "Invalid or missing user ID in token.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

#### Unexpected Server Error (500)

```json
{
    "status": false,
    "statusCode": 500,
    "data": null,
    "message": "An unexpected error occurred. Please try again later or contact support if the problem persists.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:40:00.0000000"
}
```

---

## 5. List Users by Customer

### Endpoint Details

**URL:** `GET /api/brandpartner/auth/users`  
**Authorization:** **Required** - JWT Bearer token with `Admin` or `Corporate` role  
**Purpose:** Retrieves all Brand Partner users for a specific customer, with optional filters.

### Request Headers

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `customerId` | integer | **Yes** | - | Customer ID to filter users (must be > 0) |
| `isActive` | boolean | No | null (all) | Filter by active status (`true` = active only, `false` = inactive only, omit = all) |
| `emailVerified` | boolean | No | null (all) | Filter by email verification status (`true` = verified only, `false` = unverified only, omit = all) |

### Example Requests

**Get all users for customer 2:**
```
GET /api/brandpartner/auth/users?customerId=2
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Get active, verified users for customer 2:**
```
GET /api/brandpartner/auth/users?customerId=2&isActive=true&emailVerified=true
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Get inactive users for customer 2:**
```
GET /api/brandpartner/auth/users?customerId=2&isActive=false
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Success Response (200)

```json
{
    "status": true,
    "statusCode": 200,
    "data": [
        {
            "brandPartnerUserId": 5,
            "customerId": 2,
            "email": "user1@example.com",
            "passwordHash": "[HIDDEN]",
            "firstName": "John",
            "lastName": "Doe",
            "phoneNumber": "+506 8888-8888",
            "isActive": true,
            "emailVerified": true,
            "requirePasswordChangeOnNextLogin": false,
            "failedLoginAttempts": 0,
            "lockedUntil": null,
            "lastLogin": "2026-02-28T10:00:00.0000000",
            "lastLoginIP": "192.168.1.100",
            "lastLoginUserAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            "createdAt": "2026-01-15T08:00:00.0000000",
            "updatedAt": "2026-02-28T10:00:00.0000000",
            "createdBy": "admin@vhconsultor.com"
        },
        {
            "brandPartnerUserId": 6,
            "customerId": 2,
            "email": "user2@example.com",
            "passwordHash": "[HIDDEN]",
            "firstName": "Jane",
            "lastName": "Smith",
            "phoneNumber": null,
            "isActive": true,
            "emailVerified": true,
            "requirePasswordChangeOnNextLogin": false,
            "failedLoginAttempts": 1,
            "lockedUntil": null,
            "lastLogin": "2026-02-27T16:30:00.0000000",
            "lastLoginIP": "192.168.1.101",
            "lastLoginUserAgent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)",
            "createdAt": "2026-01-20T09:15:00.0000000",
            "updatedAt": "2026-02-27T16:30:00.0000000",
            "createdBy": "admin@vhconsultor.com"
        }
    ],
    "message": "Retrieved 2 users.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:45:00.0000000"
}
```

#### Data Array Fields

| Field | Type | Description |
|-------|------|-------------|
| `brandPartnerUserId` | integer | Unique user ID |
| `customerId` | integer | Customer (company) ID |
| `email` | string | User's email address (lowercase) |
| `passwordHash` | string | SHA256 password hash (base64 encoded) |
| `firstName` | string | User's first name |
| `lastName` | string | User's last name |
| `phoneNumber` | string/null | User's phone number (optional) |
| `isActive` | boolean | Account active status |
| `emailVerified` | boolean | Email verification status |
| `requirePasswordChangeOnNextLogin` | boolean | Password change required flag |
| `failedLoginAttempts` | integer | Number of consecutive failed login attempts |
| `lockedUntil` | datetime/null | Account unlock datetime (null if not locked) |
| `lastLogin` | datetime/null | Last successful login datetime |
| `lastLoginIP` | string/null | IP address of last successful login |
| `lastLoginUserAgent` | string/null | User-Agent of last successful login |
| `createdAt` | datetime | Account creation datetime |
| `updatedAt` | datetime/null | Last update datetime |
| `createdBy` | string/null | Email of admin who created the account |

### Error Responses

#### Validation Error - Invalid Customer ID (400)

```json
{
    "status": false,
    "statusCode": 400,
    "data": null,
    "message": "CustomerId must be greater than 0.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:45:00.0000000"
}
```

#### Unauthorized - Missing JWT Token (401)

```json
{
    "status": false,
    "statusCode": 401,
    "data": null,
    "message": "Unauthorized",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:45:00.0000000"
}
```

#### Forbidden - Wrong Role (403)

```json
{
    "status": false,
    "statusCode": 403,
    "data": null,
    "message": "Forbidden",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:45:00.0000000"
}
```

**Note:** This error occurs when the JWT token has the `BrandPartner` role instead of `Admin` or `Corporate`.

#### Unexpected Server Error (500)

```json
{
    "status": false,
    "statusCode": 500,
    "data": null,
    "message": "An unexpected error occurred. Please try again later or contact support if the problem persists.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:45:00.0000000"
}
```

---

## 6. Get Login History

### Endpoint Details

**URL:** `GET /api/brandpartner/auth/login-history`  
**Authorization:** **Required** - JWT Bearer token  
- **`Admin` or `Corporate` roles:** Can view any user's history by specifying `brandPartnerUserId`
- **`BrandPartner` role:** Can only view their own history (JWT user ID is used, ignores `brandPartnerUserId` parameter)

**Purpose:** Retrieves login history records with optional filters for auditing and security monitoring.

### Request Headers

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `brandPartnerUserId` | integer | No | null (all users) | User ID to filter (ignored for `BrandPartner` role) |
| `dateFrom` | datetime | No | null (no start limit) | Start date filter (ISO 8601: `2026-01-01T00:00:00`) |
| `dateTo` | datetime | No | null (no end limit) | End date filter (ISO 8601: `2026-12-31T23:59:59`) |
| `ipAddress` | string | No | null (all IPs) | Filter by exact IP address (e.g., "192.168.1.100") |
| `country` | string | No | null (all countries) | Filter by country name (e.g., "Costa Rica") |
| `city` | string | No | null (all cities) | Filter by city name (e.g., "San Jose") |
| `loginSuccessful` | boolean | No | null (all) | Filter by success status (`true` = successes only, `false` = failures only) |
| `limit` | integer | No | 100 | Max records to return (upper limit depends on backend config) |

### Example Requests

**Get all login history for user 5:**
```
GET /api/brandpartner/auth/login-history?brandPartnerUserId=5
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Get successful logins for user 5 in February 2026:**
```
GET /api/brandpartner/auth/login-history?brandPartnerUserId=5&dateFrom=2026-02-01T00:00:00&dateTo=2026-02-28T23:59:59&loginSuccessful=true&limit=50
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Get failed logins from a specific IP:**
```
GET /api/brandpartner/auth/login-history?ipAddress=192.168.1.100&loginSuccessful=false
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**BrandPartner user viewing their own history (brandPartnerUserId is ignored):**
```
GET /api/brandpartner/auth/login-history?limit=20
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9... (BrandPartner role)
```

### Success Response (200)

```json
{
    "status": true,
    "statusCode": 200,
    "data": [
        {
            "loginHistoryId": 123,
            "brandPartnerUserId": 5,
            "loginDate": "2026-02-28T10:00:00.0000000",
            "ipAddress": "192.168.1.100",
            "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "location": "San Jose, Costa Rica",
            "country": "Costa Rica",
            "city": "San Jose",
            "loginSuccessful": true,
            "failureReason": null,
            "sessionId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
        },
        {
            "loginHistoryId": 122,
            "brandPartnerUserId": 5,
            "loginDate": "2026-02-28T09:45:00.0000000",
            "ipAddress": "192.168.1.100",
            "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "location": "San Jose, Costa Rica",
            "country": "Costa Rica",
            "city": "San Jose",
            "loginSuccessful": false,
            "failureReason": "Waiting for 2FA code",
            "sessionId": null
        },
        {
            "loginHistoryId": 121,
            "brandPartnerUserId": 5,
            "loginDate": "2026-02-27T14:30:00.0000000",
            "ipAddress": "192.168.1.101",
            "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
            "location": "Alajuela, Costa Rica",
            "country": "Costa Rica",
            "city": "Alajuela",
            "loginSuccessful": false,
            "failureReason": "Incorrect password",
            "sessionId": null
        }
    ],
    "message": "Retrieved 3 login history records.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:50:00.0000000"
}
```

#### Data Array Fields

| Field | Type | Description |
|-------|------|-------------|
| `loginHistoryId` | integer | Unique history record ID |
| `brandPartnerUserId` | integer | User ID who attempted login |
| `loginDate` | datetime | Date and time of login attempt |
| `ipAddress` | string | IP address of the client |
| `userAgent` | string/null | Browser/client User-Agent string |
| `location` | string/null | Geographic location (if available) |
| `country` | string/null | Country name (if available) |
| `city` | string/null | City name (if available) |
| `loginSuccessful` | boolean | `true` = successful login, `false` = failed attempt |
| `failureReason` | string/null | Reason for failure (e.g., "Incorrect password", "User not found", "Account locked", "Waiting for 2FA code") |
| `sessionId` | string/null | Unique session ID (only for successful 2FA verification) |

#### Failure Reason Values

| Value | Description |
|-------|-------------|
| `"User not found"` | Email doesn't exist in the system |
| `"Account inactive"` | User's account is deactivated |
| `"Account locked"` | User's account is locked due to too many failed attempts |
| `"Incorrect password"` | Wrong password provided |
| `"Waiting for 2FA code"` | Login step 1 completed successfully, waiting for 2FA verification |
| `"Invalid or expired 2FA code"` | 2FA code verification failed |

### Error Responses

#### Unauthorized - Missing JWT Token (401)

```json
{
    "status": false,
    "statusCode": 401,
    "data": null,
    "message": "Invalid or missing user ID in token.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:50:00.0000000"
}
```

#### Unexpected Server Error (500)

```json
{
    "status": false,
    "statusCode": 500,
    "data": null,
    "message": "An unexpected error occurred. Please try again later or contact support if the problem persists.",
    "errorNumber": null,
    "timestamp": "2026-02-28T14:50:00.0000000"
}
```

---

## JWT Usage

### How to Use JWT Token

After successful 2FA verification (endpoint #2), you receive a JWT token in the response:

```json
{
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
}
```

Include this token in the `Authorization` header for all authenticated requests:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### JWT Structure

The JWT contains three parts (separated by dots):

1. **Header:** Algorithm and token type
2. **Payload:** Encrypted user data
3. **Signature:** HMAC-SHA256 signature

### Encrypted Payload

The JWT payload contains an **AES-256 encrypted string** with this format:

```
<BrandPartnerUserId>|<Email>|<Role>
```

Example (before encryption):
```
5|user@example.com|BrandPartner
```

**Important:** The payload is encrypted and **cannot be decoded client-side**. The backend uses AES-256-CBC with a 256-bit key derived from the `JwtSettings.ValidationKey`.

### Token Expiration

JWT tokens expire after a configured time period (check `JwtSettings.ExpirationInMinutes` in `appsettings.json`, typically **24 hours**).

When a token expires, authenticated requests will return **401 Unauthorized**. Users must login again to get a new token.

### Token Storage Best Practices

- **Browser:** Store in secure, HttpOnly cookies (preferred) or secure localStorage
- **Mobile apps:** Store in secure storage (Keychain for iOS, Keystore for Android)
- **Never expose tokens:**
  - Don't include in URLs or query parameters
  - Don't log tokens in console or logs
  - Don't store in plain text files

---

## SendGrid Email Templates

Configure these template IDs in `appsettings.json` or `appsettings.Development.json`:

```json
{
    "SendGrid": {
        "ApiKey": "SG.your-api-key-here",
        "BrandPartnerTwoFactorCodeTemplateId": "d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
        "BrandPartnerResetPasswordTemplateId": "d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
    }
}
```

### 2FA Code Email Template

**Configuration Key:** `BrandPartnerTwoFactorCodeTemplateId`

**Template Variables:**

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `fullName` | string | "John Doe" | User's full name (firstName + lastName) |
| `code` | string | "A1234" | The 5-character 2FA code |
| `expiresIn` | string | "3 minutos" | Expiration time (hardcoded to "3 minutos") |

**Sample JSON sent to SendGrid:**

```json
{
    "fullName": "John Doe",
    "code": "A1234",
    "expiresIn": "3 minutos"
}
```

**Example Email Content:**

```
Subject: Your 2FA Login Code

Hi John Doe,

Your 2FA login code is: A1234

This code will expire in 3 minutos.

If you didn't request this code, please contact support immediately.

Best regards,
VH Consultor Team
```

### Password Reset Email Template

**Configuration Key:** `BrandPartnerResetPasswordTemplateId`

**Template Variables:**

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `fullName` | string | "John Doe" | User's full name (firstName + lastName) |
| `email` | string | "user@example.com" | User's email address |
| `newPassword` | string | "Xk7!mPq2@nLt" | The temporary password |

**Sample JSON sent to SendGrid:**

```json
{
    "fullName": "John Doe",
    "email": "user@example.com",
    "newPassword": "Xk7!mPq2@nLt"
}
```

**Example Email Content:**

```
Subject: Password Reset - Temporary Password

Hi John Doe,

Your password has been reset. Your temporary password is: Xk7!mPq2@nLt

For security reasons, you will be required to change this password when you log in.

Email: user@example.com
Temporary Password: Xk7!mPq2@nLt

Best regards,
VH Consultor Team
```

---

## Status Code Summary

| Status Code | `statusCode` Value | Description | Typical Scenarios |
|-------------|--------------------|-------------|-------------------|
| **200 OK** | 200 | Success | Request completed successfully |
| **200 OK** | 400 | Bad Request | Validation errors, invalid credentials, account inactive/locked, business logic errors |
| **200 OK** | 401 | Unauthorized | Missing/invalid JWT token, invalid user ID in token |
| **200 OK** | 500 | Internal Server Error | Unexpected server errors, database errors, email sending failures |

**Important:** All responses return HTTP **200 OK**. The actual status is in the `statusCode` field inside the JSON response body.

---

## Error Handling Best Practices

### 1. Always Check `status` Field First

```javascript
const response = await fetch('/api/brandpartner/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
});

const result = await response.json();

if (result.status === false) {
    // Handle error
    console.error(result.message);
    alert(result.message); // Display to user
    return;
}

// Handle success
console.log('Success:', result.data);
```

### 2. Handle 2FA Flow Correctly

```javascript
async function login(email, password) {
    // Step 1: Login
    const loginResponse = await fetch('/api/brandpartner/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    });
    
    const loginResult = await loginResponse.json();
    
    if (!loginResult.status) {
        alert(loginResult.message);
        return;
    }
    
    if (loginResult.data.requiresTwoFactor) {
        // Step 2: Prompt for 2FA code
        const code = prompt('Enter the 5-character code sent to your email (e.g., A1234):');
        
        const verifyResponse = await fetch('/api/brandpartner/auth/verify-2fa', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, code })
        });
        
        const verifyResult = await verifyResponse.json();
        
        if (!verifyResult.status) {
            alert(verifyResult.message);
            return;
        }
        
        // Success! Store token
        const token = verifyResult.data.token;
        localStorage.setItem('jwt', token);
        
        // Check if password change required
        if (verifyResult.data.requiresPasswordChange) {
            alert('You must change your password.');
            redirectTo('/change-password');
        } else {
            redirectTo('/dashboard');
        }
    }
}
```

### 3. Store and Use JWT Securely

```javascript
// Store token after successful login
localStorage.setItem('jwt', token);

// Use token in authenticated requests
async function getUsers(customerId) {
    const token = localStorage.getItem('jwt');
    
    const response = await fetch(`/api/brandpartner/auth/users?customerId=${customerId}`, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    
    const result = await response.json();
    
    if (!result.status) {
        if (result.statusCode === 401) {
            // Token expired or invalid
            alert('Session expired. Please login again.');
            localStorage.removeItem('jwt');
            redirectTo('/login');
        } else {
            alert(result.message);
        }
        return;
    }
    
    return result.data;
}
```

### 4. Handle Password Change Requirements

```javascript
if (loginResult.data.requiresPasswordChange) {
    // Force user to change password before accessing other features
    disableNavigation();
    showPasswordChangeModal();
}
```

### 5. Display User-Friendly Error Messages

All error messages are in English and designed to be displayed directly to users:

```javascript
if (!result.status) {
    // Display message directly
    showErrorToast(result.message);
    // Or
    document.getElementById('error-message').innerText = result.message;
}
```

### 6. Handle Multiple Validation Errors

Validation errors may contain multiple messages separated by `;`:

```javascript
if (!result.status && result.statusCode === 400) {
    const errors = result.message.split(';').map(msg => msg.trim());
    errors.forEach(error => {
        addErrorToForm(error);
    });
}
```

Example message:
```
"New password must be at least 8 characters; New password must contain at least one uppercase letter; New password must contain at least one digit"
```

---

## Security Notes

### 1. Password Security

- **Hashing:** Passwords are hashed using **SHA256** before storage
- **Never transmitted in plaintext:** Use HTTPS/TLS for all API calls
- **Complexity requirements:** Min 8 chars, 1 uppercase, 1 lowercase, 1 digit
- **Temporary passwords:** Generated securely with 12+ characters including special chars

### 2. Account Lockout Protection

- **5 failed attempts:** Account locks for **30 minutes**
- **Progressive warnings:** Remaining attempts shown in error messages
- **Automatic unlock:** Lockout expires after 30 minutes

### 3. 2FA Code Security

- **Random generation:** Codes use cryptographically secure RNG
- **Short expiration:** 3 minutes
- **One-time use:** Codes are marked as used after verification
- **Secure storage:** Codes stored in database with expiration timestamp

### 4. JWT Security

- **Encrypted payload:** User data encrypted with AES-256
- **HMAC-SHA256 signature:** Prevents tampering
- **Expiration:** Tokens expire after configured time (24 hours default)
- **Role-based access:** Endpoints enforce role requirements

### 5. Email Security

- **Generic responses:** Password reset returns success even if email doesn't exist
- **Secure templates:** SendGrid templates with proper formatting
- **No sensitive data in URLs:** Passwords and codes only in email body

### 6. Input Validation

- **Email format:** RFC 5322 compliant email validation
- **Password complexity:** FluentValidation rules enforced
- **2FA code format:** Strict regex `^[A-Z][0-9]{4}$`
- **SQL injection prevention:** Entity Framework with parameterized queries

### 7. Error Handling

- **No internal details exposed:** Generic error messages for 500 errors
- **Security through obscurity:** "Invalid email or password" for both scenarios
- **Logging recommended:** Server-side logging for debugging (not shown to client)

---

## Additional Notes

1. **Date/Time Format:** All dates/times are in **ISO 8601 format** (e.g., `2026-02-28T14:30:00.0000000`)
2. **Email Case Sensitivity:** Email addresses are stored and compared in **lowercase**
3. **2FA Code Expiration:** Codes expire exactly **3 minutes** after generation
4. **Account Lockout Duration:** **30 minutes** after 5 failed attempts
5. **Password Hashing Algorithm:** **SHA256** (not salted - consider upgrading to bcrypt/Argon2 for production)
6. **JWT Algorithm:** **HMAC-SHA256** with AES-256 payload encryption
7. **SendGrid Rate Limits:** Be aware of SendGrid's sending limits on your plan
8. **Database Tables Required:** Execute `/Scripts/CreateBrandPartnerUsersTable.sql` before using these APIs
9. **User-Agent Tracking:** Automatically captured from HTTP headers for login history
10. **IP Address Tracking:** Automatically captured from `HttpContext` for security monitoring
