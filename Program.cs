using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SNovimGodom.Data;
using SNovimGodom.Models;

var builder = WebApplication.CreateBuilder(args);

var mysqlHost = builder.Configuration["MYSQL_HOST"] ?? "localhost";
var mysqlPort = builder.Configuration["MYSQL_PORT"] ?? "3306";
var mysqlDatabase = builder.Configuration["MYSQL_DATABASE"] ?? "snovimgodom";
var mysqlUser = builder.Configuration["MYSQL_USER"] ?? "root";
var mysqlPassword = builder.Configuration["MYSQL_PASSWORD"] ?? string.Empty;

var fallbackConnection =
    $"server={mysqlHost};port={mysqlPort};database={mysqlDatabase};user={mysqlUser};password={mysqlPassword};";

var connection =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? fallbackConnection;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connection,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    EnsureDatabaseSchema(db, connection);

    if (!db.Users.Any(x => x.Role == "Admin"))
    {
        db.Users.AddRange(
            new User { Email = "admin1@test.com", Password = "123", Role = "Admin" },
            new User { Email = "admin2@test.com", Password = "123", Role = "Admin" }
        );

        db.SaveChanges();
    }
}

app.MapControllers();

app.Run();

static void EnsureDatabaseSchema(AppDbContext db, string connectionString)
{
    try
    {
        db.Database.Migrate();
        return;
    }
    catch (MySqlException exception) when (exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
        // Existing databases may be in a partially initialized state.
    }

    RepairExistingSchema(connectionString);
    db.Database.Migrate();
}

static void RepairExistingSchema(string connectionString)
{
    using var connection = new MySqlConnection(connectionString);
    connection.Open();

    var schema = connection.Database;

    EnsureMigrationsHistoryTable(connection);
    EnsureUsersTable(connection, schema);
    EnsureProductsTable(connection, schema);
    EnsureProductColumn(connection, schema, "Weight", "ALTER TABLE `Products` ADD COLUMN `Weight` int NOT NULL DEFAULT 0;");
    EnsureProductColumn(connection, schema, "IsActive", "ALTER TABLE `Products` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT TRUE;");

    var hasProductsTable = TableExists(connection, schema, "Products");
    var hasUsersTable = TableExists(connection, schema, "Users");

    if (hasProductsTable && hasUsersTable)
    {
        EnsureMigrationHistoryRecord(connection, "20260410151152_InitialCreate");
    }
}

static void EnsureProductsTable(DbConnection connection, string schema)
{
    if (TableExists(connection, schema, "Products"))
    {
        return;
    }

    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE `Products` (
            `Id` int NOT NULL AUTO_INCREMENT,
            `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
            `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
            `Price` decimal(65,30) NOT NULL,
            `Weight` int NOT NULL DEFAULT 0,
            `Category` longtext CHARACTER SET utf8mb4 NOT NULL,
            `Brand` longtext CHARACTER SET utf8mb4 NOT NULL,
            `ImageUrl` longtext CHARACTER SET utf8mb4 NOT NULL,
            `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
            CONSTRAINT `PK_Products` PRIMARY KEY (`Id`)
        ) CHARACTER SET=utf8mb4;
        """;
    command.ExecuteNonQuery();
}

static void EnsureUsersTable(DbConnection connection, string schema)
{
    if (TableExists(connection, schema, "Users"))
    {
        return;
    }

    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE `Users` (
            `Id` int NOT NULL AUTO_INCREMENT,
            `Email` longtext CHARACTER SET utf8mb4 NOT NULL,
            `Password` longtext CHARACTER SET utf8mb4 NOT NULL,
            `Role` longtext CHARACTER SET utf8mb4 NOT NULL,
            CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
        ) CHARACTER SET=utf8mb4;
        """;
    command.ExecuteNonQuery();
}

static void EnsureProductColumn(DbConnection connection, string schema, string columnName, string alterSql)
{
    if (!TableExists(connection, schema, "Products") || ColumnExists(connection, schema, "Products", columnName))
    {
        return;
    }

    using var command = connection.CreateCommand();
    command.CommandText = alterSql;
    command.ExecuteNonQuery();
}

static bool TableExists(DbConnection connection, string schema, string tableName)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = @schema AND table_name = @tableName;
        """;

    AddParameter(command, "@schema", schema);
    AddParameter(command, "@tableName", tableName);

    return Convert.ToInt32(command.ExecuteScalar()) > 0;
}

static bool ColumnExists(DbConnection connection, string schema, string tableName, string columnName)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE table_schema = @schema AND table_name = @tableName AND column_name = @columnName;
        """;

    AddParameter(command, "@schema", schema);
    AddParameter(command, "@tableName", tableName);
    AddParameter(command, "@columnName", columnName);

    return Convert.ToInt32(command.ExecuteScalar()) > 0;
}

static void EnsureMigrationsHistoryTable(DbConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
            `MigrationId` varchar(150) NOT NULL,
            `ProductVersion` varchar(32) NOT NULL,
            CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
        );
        """;
    command.ExecuteNonQuery();
}

static void EnsureMigrationHistoryRecord(DbConnection connection, string migrationId)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
        SELECT @migrationId, '8.0.0'
        WHERE NOT EXISTS (
            SELECT 1
            FROM `__EFMigrationsHistory`
            WHERE `MigrationId` = @migrationId
        );
        """;

    AddParameter(command, "@migrationId", migrationId);
    command.ExecuteNonQuery();
}

static void AddParameter(DbCommand command, string name, object value)
{
    var parameter = command.CreateParameter();
    parameter.ParameterName = name;
    parameter.Value = value;
    command.Parameters.Add(parameter);
}
