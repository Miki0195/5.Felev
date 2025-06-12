-- comment out problematic lines
-- ALTER TABLE `Messages` DROP COLUMN `NotificationGroup`;

-- Check if column exists before adding
ALTER TABLE `Notifications` ADD COLUMN IF NOT EXISTS `NotificationGroup` char(36) COLLATE ascii_general_ci NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

-- Comment out problematic drop
-- ALTER TABLE `Notifications` DROP COLUMN `NotificationGroup`;

-- Rest of the file remains unchanged 