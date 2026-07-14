using Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    [DbContext(typeof(NemeBookDbContext))]
    [Migration("20260714120000_RepairNotificationsIsReadColumn")]
    public partial class RepairNotificationsIsReadColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Notifications]', N'U') IS NOT NULL
                    AND COL_LENGTH(N'dbo.Notifications', N'IsRead') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Notifications]
                    ADD [IsRead] bit NOT NULL
                        CONSTRAINT [DF_Notifications_IsRead] DEFAULT CAST(0 AS bit)
                        WITH VALUES;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Notifications]', N'U') IS NOT NULL
                    AND COL_LENGTH(N'dbo.Notifications', N'IsRead') IS NOT NULL
                BEGIN
                    DECLARE @constraintName sysname;
                    DECLARE @sql nvarchar(max);

                    SELECT @constraintName = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.default_object_id = dc.object_id
                    INNER JOIN sys.tables t
                        ON t.object_id = c.object_id
                    INNER JOIN sys.schemas s
                        ON s.schema_id = t.schema_id
                    WHERE s.name = N'dbo'
                        AND t.name = N'Notifications'
                        AND c.name = N'IsRead';

                    IF @constraintName IS NOT NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT ' + QUOTENAME(@constraintName);
                        EXEC sp_executesql @sql;
                    END

                    ALTER TABLE [dbo].[Notifications] DROP COLUMN [IsRead];
                END
                """);
        }
    }
}
