# GBTSolutionsTest

A product REST API built with .NET 10, ASP.NET Core, Entity Framework Core and Microsoft SQL Server. The project requires the .NET 10 SDK and a SQL Server instance (or Docker with a compatible host). All commands below run from the solution directory.

## Start the API

1. Start a local SQL Server instance with `docker compose up -d`. Wait until SQL Server is ready to accept connections. Alternatively, use an existing SQL Server and set the `ConnectionStrings__Products` environment variable to its connection string. The sample `sa` password and `TrustServerCertificate=True` setting are for local development only.
2. Install the EF Core tool: `dotnet tool install --global dotnet-ef --version 10.0.0`. Ensure the .NET tools directory is on your PATH.
3. Restore dependencies: `dotnet restore GBTSolutionsTest.slnx`.
4. Apply the migration: `dotnet ef database update --project Products.Api`. The configured SQL login must have permissions to create the database and schema objects, or the database must already exist with appropriate permissions. Run migrations once as a deployment step before starting API instances. The initial migration inserts three sample products.
5. Start the API: `dotnet run --project Products.Api`. The console displays the listening address.
6. Run unit tests without a database: `dotnet test Products.Tests/Products.Tests.csproj`. See the BDD section for integration tests.

## API

| Method | Path | Behavior |
| --- | --- | --- |
| GET | `/api/products` | List products, including stock |
| POST | `/api/products` | Create a product |
| GET | `/api/products/{id}` | Retrieve a product |
| PUT | `/api/products/{id}` | Replace product fields |
| DELETE | `/api/products/{id}` | Delete a product |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | Decrease stock |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | Increase stock |
| GET | `/api/products/search?name=lens` | Search by partial name |
| GET | `/api/products/stock-level?min=1&max=50` | Search by inclusive stock range |

Create/update example: `{"name":"Lens","price":25.50,"stock":20}`. Name is required (maximum 200 characters), price must be positive with at most two decimal places, and stock must be a nonnegative integer. Search uses literal substring matching and a case-insensitive, accent-sensitive SQL Server collation. Stock operations require a positive integer and return 409 for insufficient stock or integer overflow. Missing products return 404; invalid input returns 400; creation returns 201 with Location; update and delete return 204.

## IDs and concurrency

All application instances must connect to the same SQL Server database. A database sequence allocates six-digit IDs from 100000 through 999999 without cycling, and a primary key enforces uniqueness. The sequence does not reuse numbers after deletion, rollback, or server restart; gaps are expected. Once exhausted, creation fails and the six-digit constraint must be revisited. Stock increments and decrements are conditional atomic database updates, preventing overselling and integer overflow. PUT replaces all mutable fields, including stock; clients must coordinate PUT with stock operations to avoid overwriting a newer quantity. The sample has no authentication and is intended for the assessment, not direct production deployment.

## Behavior-driven tests

`Products.BddTests` uses Reqnroll and xUnit. English Gherkin scenarios execute HTTP requests against in-process API hosts and check responses and database state. Each scenario creates a temporary database on a SQL Server instance, applies the EF migration and removes the database afterward. Concurrency scenarios use two API hosts connected to the same test database.

Use a **dedicated disposable SQL Server instance**, never a production server. The login specified by `BDD_CONNECTION_STRING` must be able to connect to `master` and create, alter and drop databases. Each generated database is named `bdd_` followed by a random identifier. If the process is interrupted, remove orphaned `bdd_` databases manually. The included local Docker instance can be used for this purpose.

PowerShell:

```powershell
$env:BDD_CONNECTION_STRING = 'Server=localhost,1433;Database=master;User Id=sa;Password=Local_only_123!;TrustServerCertificate=True'
dotnet test GBTSolutionsTest.slnx
```

Bash:

```bash
export BDD_CONNECTION_STRING='Server=localhost,1433;Database=master;User Id=sa;Password=Local_only_123!;TrustServerCertificate=True'
dotnet test GBTSolutionsTest.slnx
```

To run only the BDD scenarios, use `dotnet test Products.BddTests/Products.BddTests.csproj`. Dependency restoration requires access to NuGet. These tests do not replace production deployment or load tests.

### Windows LocalDB alternative

On Windows, SQL Server LocalDB can be used instead of the Docker instance. Create and start a LocalDB instance, then configure the API connection string with Windows authentication:

```powershell
sqllocaldb create GBTSolutionsTest
sqllocaldb start GBTSolutionsTest
$env:ConnectionStrings__Products = 'Server=(localdb)\GBTSolutionsTest;Database=Products;Integrated Security=True;TrustServerCertificate=True'
```

To run BDD tests against this instance, use the following in the same PowerShell session:

```powershell
$env:BDD_CONNECTION_STRING = 'Server=(localdb)\GBTSolutionsTest;Integrated Security=True;TrustServerCertificate=True'
dotnet test GBTSolutionsTest.slnx
```

The BDD tests create and remove dedicated temporary databases on the configured instance. Use an instance intended for testing.
