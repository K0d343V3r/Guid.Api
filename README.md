# Guid.Api
ASP.NET Core Web API service to efficiently generate, store, and retrieve guids.

##### Features:

- Guids may be created, retrieved, updated, or deleted.
- When creating a guid, caller should specify a user name and may specify an expiration date.
- If no expiration date is given, a 30-day default is used.
- Expired guids cannot be retrieved, although they can be updated.
- Caller may update a guid's user name and expiration date, but not guid itself.
- Guids represented as 32 hexadecimal characters, all uppercase.
- The expiration date is formatted in UNIX time.

##### Implementation:

- ASP.NET Core Web API 2.2.
- Entity Framework Core.
- SQL Server back-end.
- Redis cache for fast guid retrieval.
- Dependency injected database and cache logic.
- Unit tests (xUnit/Moq) for all controller methods.
- Rich error information returned from all APIs.
- Swagger document support.
- All async API's.
