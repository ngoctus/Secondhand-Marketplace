# Web API + JWT (M1)

## Login
`POST /api/auth/login`

```json
{
  "email": "user@example.com",
  "password": "your-password"
}
```

A successful response returns `accessToken`, `tokenType: Bearer`, expiry, and basic user information. API login uses the same `IAuthService` and BCrypt validation as MVC login; locked accounts are rejected.

## Call protected endpoints
Send the token in the HTTP header:

`Authorization: Bearer <accessToken>`

## Configuration
Set `Jwt:Key` in appsettings or environment configuration to a private random secret of at least 32 bytes. Do not commit a production secret. `appsettings.Example.json` contains a development placeholder only.

The existing MVC UI continues using its Cookie scheme for compatibility. Web API endpoints must explicitly select the JWT scheme using `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`. This allows the MVC demo to remain usable while API authentication is JWT-based.
