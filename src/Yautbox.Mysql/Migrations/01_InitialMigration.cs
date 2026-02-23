using System;
using FluentMigrator;
using Microsoft.Extensions.Options;
using Yautbox.Mysql.Migrations.Options;
using Yautbox.Mysql.Migrations.Shared;

namespace Yautbox.Mssql.Migrations;

[Migration(1, "InitialMigration")]
internal sealed class InitialMigration : SqlMigration
{
    private readonly MigrationOptions _options;

    public InitialMigration(IOptions<MigrationOptions> options) => _options = options.Value;

    protected override string GetUpSql(IServiceProvider services) =>
        $"""
         CREATE DATABASE IF NOT EXISTS `{_options.SchemaName}`
           DEFAULT CHARACTER SET utf8mb4
           DEFAULT COLLATE utf8mb4_0900_ai_ci;

         CREATE TABLE IF NOT EXISTS `{_options.SchemaName}`.`outbox_messages`
         (
             `id`           BIGINT NOT NULL AUTO_INCREMENT,
             `type`         VARCHAR(450) NOT NULL,
             `payload`      LONGTEXT NOT NULL,
             `created_at`   DATETIME(6) NOT NULL,
             `attempt`      INT NOT NULL DEFAULT 0,
             `scheduled_at` DATETIME(6) NULL,
             `is_deleted`   TINYINT(1) NOT NULL DEFAULT 0,
             `locker`       DATETIME(6) NULL,
             PRIMARY KEY (`id`)
         ) ENGINE=InnoDB;

         CREATE INDEX `IX_outbox_messages_active`
             ON `{_options.SchemaName}`.`outbox_messages` (`type`, `is_deleted`, `scheduled_at`, `locker`, `id`);

         CREATE INDEX `IX_outbox_messages_deleted`
             ON `{_options.SchemaName}`.`outbox_messages` (`type`, `is_deleted`, `created_at`, `id`);
         """;

    protected override string GetDownSql(IServiceProvider services) =>
        $"""
         DROP TABLE IF EXISTS `{_options.SchemaName}`.`outbox_messages`;
         """;
}
