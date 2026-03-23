START TRANSACTION;
ALTER TABLE `streamerprofiles` DROP COLUMN `NaverId`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260323003927_RemoveNaverIdColumn', '9.0.0');

COMMIT;

