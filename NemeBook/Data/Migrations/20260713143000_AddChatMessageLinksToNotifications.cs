using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageLinksToNotificationsNoAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Notifications', 'ChatId') IS NULL
                BEGIN
                    ALTER TABLE [Notifications] ADD [ChatId] uniqueidentifier NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Notifications', 'MessageId') IS NULL
                BEGIN
                    ALTER TABLE [Notifications] ADD [MessageId] uniqueidentifier NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_ChatId' AND object_id = OBJECT_ID('Notifications'))
                BEGIN
                    CREATE INDEX [IX_Notifications_ChatId] ON [Notifications] ([ChatId]);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_MessageId' AND object_id = OBJECT_ID('Notifications'))
                BEGIN
                    CREATE INDEX [IX_Notifications_MessageId] ON [Notifications] ([MessageId]);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Chats_ChatId')
                BEGIN
                    ALTER TABLE [Notifications]
                    ADD CONSTRAINT [FK_Notifications_Chats_ChatId]
                    FOREIGN KEY ([ChatId]) REFERENCES [Chats] ([Id]) ON DELETE SET NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Messages_MessageId')
                BEGIN
                    ALTER TABLE [Notifications]
                    ADD CONSTRAINT [FK_Notifications_Messages_MessageId]
                    FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]) ON DELETE NO ACTION;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Chats_ChatId')
                BEGIN
                    ALTER TABLE [Notifications] DROP CONSTRAINT [FK_Notifications_Chats_ChatId];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Messages_MessageId')
                BEGIN
                    ALTER TABLE [Notifications] DROP CONSTRAINT [FK_Notifications_Messages_MessageId];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_ChatId' AND object_id = OBJECT_ID('Notifications'))
                BEGIN
                    DROP INDEX [IX_Notifications_ChatId] ON [Notifications];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_MessageId' AND object_id = OBJECT_ID('Notifications'))
                BEGIN
                    DROP INDEX [IX_Notifications_MessageId] ON [Notifications];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Notifications', 'ChatId') IS NOT NULL
                BEGIN
                    ALTER TABLE [Notifications] DROP COLUMN [ChatId];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Notifications', 'MessageId') IS NOT NULL
                BEGIN
                    ALTER TABLE [Notifications] DROP COLUMN [MessageId];
                END
                """);
        }
    }
}
