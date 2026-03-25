START TRANSACTION;
ALTER TABLE `roulettelogs` ADD `RouletteId` int NOT NULL DEFAULT 0;

ALTER TABLE `roulettelogs` ADD `RouletteName` varchar(100) NOT NULL DEFAULT '';

CREATE INDEX `IX_roulettelogs_RouletteId` ON `roulettelogs` (`RouletteId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260325142801_AddRouletteMetaDataManualFix', '9.0.0');

COMMIT;

