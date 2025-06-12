START TRANSACTION;

ALTER TABLE `Messages` DROP COLUMN `NotificationGroup`;

ALTER TABLE `Notifications` ADD `NotificationGroup` char(36) COLLATE ascii_general_ci NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250204231636_DatabaseCorrection', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Notifications` DROP COLUMN `NotificationGroup`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250204232807_DatabaseUpdated', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Messages` ADD `IsDeleted` tinyint(1) NOT NULL DEFAULT FALSE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250206112246_UpdatedMessagesTable', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `VideoCallInvitations` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SenderId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ReceiverId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Timestamp` datetime(6) NOT NULL,
    `IsAccepted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_VideoCallInvitations` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_VideoCallInvitations_AspNetUsers_ReceiverId` FOREIGN KEY (`ReceiverId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_VideoCallInvitations_AspNetUsers_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_VideoCallInvitations_ReceiverId` ON `VideoCallInvitations` (`ReceiverId`);

CREATE INDEX `IX_VideoCallInvitations_SenderId` ON `VideoCallInvitations` (`SenderId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250210103044_VideoCallInvite', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `VideoConferences` (
    `CallId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CallerId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ReceiverId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StartTime` datetime(6) NOT NULL,
    `EndTime` datetime(6) NULL,
    CONSTRAINT `PK_VideoConferences` PRIMARY KEY (`CallId`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250214170950_AddVideoConferenceTable', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `VideoConferences` ADD `IsEnded` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `VideoConferences` ADD `Status` int NOT NULL DEFAULT 0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250217082024_UpdatedVideoConferences', '8.0.2');

COMMIT;

START TRANSACTION;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250217121154_VideoCallStatusUpdate', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Messages` ADD `RepliedToMessageId` int NULL;

CREATE INDEX `IX_Messages_RepliedToMessageId` ON `Messages` (`RepliedToMessageId`);

ALTER TABLE `Messages` ADD CONSTRAINT `FK_Messages_Messages_RepliedToMessageId` FOREIGN KEY (`RepliedToMessageId`) REFERENCES `Messages` (`Id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250221162047_MessageReplyOption', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Messages` DROP FOREIGN KEY `FK_Messages_Messages_RepliedToMessageId`;

ALTER TABLE `Messages` DROP INDEX `IX_Messages_RepliedToMessageId`;

ALTER TABLE `Messages` DROP COLUMN `RepliedToMessageId`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250221163425_MessageModelRollBack', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `Projects` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StartDate` datetime(6) NOT NULL,
    `Deadline` datetime(6) NOT NULL,
    `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Priority` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedById` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CompanyId` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `TotalTasks` int NOT NULL,
    `CompletedTasks` int NOT NULL,
    CONSTRAINT `PK_Projects` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Projects_AspNetUsers_CreatedById` FOREIGN KEY (`CreatedById`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Projects_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ProjectMembers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProjectId` int NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Role` longtext CHARACTER SET utf8mb4 NOT NULL,
    `JoinedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ProjectMembers` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProjectMembers_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ProjectMembers_Projects_ProjectId` FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ProjectMembers_ProjectId` ON `ProjectMembers` (`ProjectId`);

CREATE INDEX `IX_ProjectMembers_UserId` ON `ProjectMembers` (`UserId`);

CREATE INDEX `IX_Projects_CompanyId` ON `Projects` (`CompanyId`);

CREATE INDEX `IX_Projects_CreatedById` ON `Projects` (`CreatedById`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250221211656_ProjectTable', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Tasks` RENAME COLUMN `UserId` TO `ProjectId`;

ALTER TABLE `Tasks` ADD `CreatedById` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Tasks` ADD `Priority` longtext CHARACTER SET utf8mb4 NOT NULL;

CREATE INDEX `IX_Tasks_CreatedById` ON `Tasks` (`CreatedById`);

CREATE INDEX `IX_Tasks_ProjectId` ON `Tasks` (`ProjectId`);

ALTER TABLE `Tasks` ADD CONSTRAINT `FK_Tasks_AspNetUsers_CreatedById` FOREIGN KEY (`CreatedById`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

ALTER TABLE `Tasks` ADD CONSTRAINT `FK_Tasks_Projects_ProjectId` FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE CASCADE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250221224056_UpdatedTask', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Tasks` DROP FOREIGN KEY `FK_Tasks_AspNetUsers_AssignedToId`;

ALTER TABLE `Tasks` DROP INDEX `IX_Tasks_AssignedToId`;

ALTER TABLE `Tasks` DROP COLUMN `AssignedToId`;

CREATE TABLE `TaskAssignment` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TaskId` int NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `AssignedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_TaskAssignment` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_TaskAssignment_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_TaskAssignment_Tasks_TaskId` FOREIGN KEY (`TaskId`) REFERENCES `Tasks` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_TaskAssignment_TaskId` ON `TaskAssignment` (`TaskId`);

CREATE INDEX `IX_TaskAssignment_UserId` ON `TaskAssignment` (`UserId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250221225911_UpdateTasks', '8.0.2');

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS `POMELO_BEFORE_DROP_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID TINYINT(1);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `Extra` = 'auto_increment'
			AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

DROP PROCEDURE IF EXISTS `POMELO_AFTER_ADD_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255), IN `COLUMN_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID INT(11);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
			AND `COLUMN_TYPE` LIKE '%int%'
			AND `COLUMN_KEY` = 'PRI';
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL AUTO_INCREMENT;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

ALTER TABLE `TaskAssignment` DROP FOREIGN KEY `FK_TaskAssignment_AspNetUsers_UserId`;

ALTER TABLE `TaskAssignment` DROP FOREIGN KEY `FK_TaskAssignment_Tasks_TaskId`;

CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'TaskAssignment');
ALTER TABLE `TaskAssignment` DROP PRIMARY KEY;

ALTER TABLE `TaskAssignment` RENAME `TaskAssignments`;

ALTER TABLE `TaskAssignments` RENAME INDEX `IX_TaskAssignment_UserId` TO `IX_TaskAssignments_UserId`;

ALTER TABLE `TaskAssignments` RENAME INDEX `IX_TaskAssignment_TaskId` TO `IX_TaskAssignments_TaskId`;

ALTER TABLE `TaskAssignments` ADD CONSTRAINT `PK_TaskAssignments` PRIMARY KEY (`Id`);
CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'TaskAssignments', 'Id');

ALTER TABLE `TaskAssignments` ADD CONSTRAINT `FK_TaskAssignments_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

ALTER TABLE `TaskAssignments` ADD CONSTRAINT `FK_TaskAssignments_Tasks_TaskId` FOREIGN KEY (`TaskId`) REFERENCES `Tasks` (`Id`) ON DELETE CASCADE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250221230706_TaskErrorFix', '8.0.2');

DROP PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`;

DROP PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`;

COMMIT;

START TRANSACTION;

ALTER TABLE `Schedules` ADD `ProjectId` int NULL;

CREATE INDEX `IX_Schedules_ProjectId` ON `Schedules` (`ProjectId`);

ALTER TABLE `Schedules` ADD CONSTRAINT `FK_Schedules_Projects_ProjectId` FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250303083928_AddedProjectToScedule', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Projects` MODIFY COLUMN `Deadline` datetime(6) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250303090534_DatabaseUpdate', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `GroupChats` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedById` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_GroupChats` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GroupChats_AspNetUsers_CreatedById` FOREIGN KEY (`CreatedById`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `GroupMembers` (
    `GroupId` int NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `JoinedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_GroupMembers` PRIMARY KEY (`GroupId`, `UserId`),
    CONSTRAINT `FK_GroupMembers_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_GroupMembers_GroupChats_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `GroupChats` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `GroupMessages` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `GroupId` int NOT NULL,
    `SenderId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Timestamp` datetime(6) NOT NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_GroupMessages` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GroupMessages_AspNetUsers_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_GroupMessages_GroupChats_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `GroupChats` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_GroupChats_CreatedById` ON `GroupChats` (`CreatedById`);

CREATE INDEX `IX_GroupMembers_UserId` ON `GroupMembers` (`UserId`);

CREATE INDEX `IX_GroupMessages_GroupId` ON `GroupMessages` (`GroupId`);

CREATE INDEX `IX_GroupMessages_SenderId` ON `GroupMessages` (`SenderId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250303151935_GroupChatModels', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `GroupMessageReads` (
    `MessageId` int NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ReadAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_GroupMessageReads` PRIMARY KEY (`MessageId`, `UserId`),
    CONSTRAINT `FK_GroupMessageReads_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_GroupMessageReads_GroupMessages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `GroupMessages` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_GroupMessageReads_UserId` ON `GroupMessageReads` (`UserId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250303154202_GroupchatModelsupdate', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `GroupMembers` ADD `IsAdmin` tinyint(1) NOT NULL DEFAULT FALSE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250306175307_GroupChatsAdmin', '8.0.2');

COMMIT;

START TRANSACTION;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250306180447_JustATestUpdate', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `DashboardLayouts` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `LayoutData` longtext CHARACTER SET utf8mb4 NOT NULL,
    `LastUpdated` datetime(6) NOT NULL,
    CONSTRAINT `PK_DashboardLayouts` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_DashboardLayouts_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_DashboardLayouts_UserId` ON `DashboardLayouts` (`UserId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250306210815_DashboardLayout', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `AspNetUsers` ADD `TotalVacationDays` int NOT NULL DEFAULT 0;

ALTER TABLE `AspNetUsers` ADD `UsedVacationDays` int NOT NULL DEFAULT 0;

ALTER TABLE `AspNetUsers` ADD `VacationYear` int NOT NULL DEFAULT 0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250317141654_VacationDaysForUsers', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Projects` ADD `CompletedAt` datetime(6) NULL;

CREATE TABLE `ActivityLogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProjectId` int NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Action` longtext CHARACTER SET utf8mb4 NOT NULL,
    `TargetType` longtext CHARACTER SET utf8mb4 NOT NULL,
    `TargetId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `TargetName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Timestamp` datetime(6) NOT NULL,
    `AdditionalData` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_ActivityLogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ActivityLogs_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ActivityLogs_Projects_ProjectId` FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ActivityLogs_ProjectId` ON `ActivityLogs` (`ProjectId`);

CREATE INDEX `IX_ActivityLogs_UserId` ON `ActivityLogs` (`UserId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250320112100_ActivityLogsForProjects', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `ActivityLogs` ADD `Type` longtext CHARACTER SET utf8mb4 NOT NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250322153545_TypeToActivitylogs', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `ActivityLogs` MODIFY COLUMN `Type` longtext CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250322154316_UpdateActivityModel', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `AttendanceAuditLogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `AttendanceId` int NOT NULL,
    `ModifiedByUserId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ModificationTime` datetime(6) NOT NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AttendanceAuditLogs` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250324113655_Attendanceauditlog', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Notifications` ADD `MetaData` longtext CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Notifications` ADD `ProjectId` int NULL;

ALTER TABLE `Notifications` ADD `RelatedUserId` longtext CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Notifications` ADD `TaskId` int NULL;

ALTER TABLE `Notifications` ADD `Type` int NOT NULL DEFAULT 0;

ALTER TABLE `AttendanceAuditLogs` MODIFY COLUMN `ModifiedByUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

CREATE INDEX `IX_AttendanceAuditLogs_AttendanceId` ON `AttendanceAuditLogs` (`AttendanceId`);

CREATE INDEX `IX_AttendanceAuditLogs_ModifiedByUserId` ON `AttendanceAuditLogs` (`ModifiedByUserId`);

ALTER TABLE `AttendanceAuditLogs` ADD CONSTRAINT `FK_AttendanceAuditLogs_AspNetUsers_ModifiedByUserId` FOREIGN KEY (`ModifiedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

ALTER TABLE `AttendanceAuditLogs` ADD CONSTRAINT `FK_AttendanceAuditLogs_Attendances_AttendanceId` FOREIGN KEY (`AttendanceId`) REFERENCES `Attendances` (`Id`) ON DELETE CASCADE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250326114221_NotificationUpdate', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `AspNetUsers` ADD `CreatedDate` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00';

CREATE TABLE `OwnerActivityLogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ActivityType` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Timestamp` datetime(6) NOT NULL,
    `CompanyName` longtext CHARACTER SET utf8mb4 NULL,
    `CompanyId` int NULL,
    `PerformedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_OwnerActivityLogs` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250401201337_DatabaseUpdateOwner', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `LicenseKeys` ADD `Status` int NOT NULL DEFAULT 0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250401210012_Licensekeyupdate', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE `RefreshTokens` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Token` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ExpiryDate` datetime(6) NOT NULL,
    `IsRevoked` tinyint(1) NOT NULL,
    `IsUsed` tinyint(1) NOT NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NOT NULL,
    CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RefreshTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_RefreshTokens_UserId` ON `RefreshTokens` (`UserId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250402104137_jwttokens', '8.0.2');

COMMIT;

START TRANSACTION;

DROP TABLE `RefreshTokens`;

ALTER TABLE `Tasks` ADD `CompletedAt` datetime(6) NULL;

ALTER TABLE `Tasks` ADD `UpdatedAt` datetime(6) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250402185014_addtasktimestamp', '8.0.2');

COMMIT;

