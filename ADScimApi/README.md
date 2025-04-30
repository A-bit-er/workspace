# Active Directory SCIM API

A .NET 8 API that provides SCIM 2.0 interface for managing Active Directory objects (users, groups, gMSA) with business rules for OU assignment and authentication/authorization.

## Features

- SCIM 2.0 compliant API for Active Directory management
- CRUD operations for users, groups, and gMSA objects
- Business rules engine for automatic OU assignment
- Authentication and authorization layer
- Blazor frontend for management
- Audit logging

## Architecture

The application is built with a layered architecture:

1. **API Layer**: SCIM-compliant REST API endpoints
2. **Business Logic Layer**: Rules engine for OU assignment
3. **Service Layer**: Active Directory operations and SCIM transformations
4. **Data Layer**: Entity Framework Core for local data storage

## Getting Started

### Prerequisites

- .NET 8 SDK
- Access to an Active Directory domain (or use the mock service for development)

### Configuration

Update the `appsettings.json` file with your Active Directory settings:

```json
"ActiveDirectory": {
  "Domain": "yourdomain.com",
  "Username": "admin",
  "Password": "password",
  "Server": "dc.yourdomain.com",
  "BaseDN": "DC=yourdomain,DC=com",
  "AllowedOUs": [
    "OU=Users,DC=yourdomain,DC=com",
    "OU=Groups,DC=yourdomain,DC=com",
    "OU=ServiceAccounts,DC=yourdomain,DC=com"
  ]
}
```

For development, you can use the mock service by setting `"UseMockService": true` in the `ActiveDirectory` section of `appsettings.Development.json`.

### Running the Application

```bash
dotnet run
```

The application will be available at:
- API: http://localhost:50972
- Swagger UI: http://localhost:50972/swagger
- Blazor UI: http://localhost:50972

## API Endpoints

### SCIM 2.0 Endpoints

- `/scim/v2/Users` - User management
- `/scim/v2/Groups` - Group management

### Custom API Endpoints

- `/api/BusinessRules` - Business rules management
- `/api/OrganizationalUnits` - OU management
- `/api/Auth` - Authentication and user management

## Authentication

The API uses JWT Bearer token authentication. To get a token, send a POST request to `/api/Auth/login` with:

```json
{
  "username": "admin",
  "password": "password"
}
```

Use the returned token in the Authorization header for subsequent requests:

```
Authorization: Bearer <token>
```

## Business Rules

Business rules determine which OU an object should be placed in based on conditions. Rules are evaluated in priority order (lower numbers have higher priority).

Example rule:
```json
{
  "name": "IT Department Users",
  "objectType": "User",
  "condition": "department == \"IT\"",
  "targetOU": "OU=IT,OU=Users,DC=yourdomain,DC=com",
  "priority": 10,
  "isActive": true
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.