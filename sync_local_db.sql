-- ============================================
-- 로컬 개발 DB 스키마 동기화 스크립트
-- ============================================

-- 1. streamerprofiles 컬럼 추가/수정
ALTER TABLE streamerprofiles
  ADD COLUMN IF NOT EXISTS IsBotEnabled TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS BotAccessToken LONGTEXT NULL,
  ADD COLUMN IF NOT EXISTS BotRefreshToken LONGTEXT NULL,
  ADD COLUMN IF NOT EXISTS BotTokenExpiresAt DATETIME(6) NULL,
  ADD COLUMN IF NOT EXISTS ActiveOverlayPresetId INT NULL,
  ADD COLUMN IF NOT EXISTS SonglistSessionActive TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS OmakaseEnabled TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS CategorySyncEnabled TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS AvatarEnabled TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS AvatarImageUrl LONGTEXT NULL;

-- 2. overlaypresets 테이블
CREATE TABLE IF NOT EXISTS overlaypresets (
  Id INT NOT NULL AUTO_INCREMENT,
  ChzzkUid VARCHAR(50) NULL,
  Name LONGTEXT NOT NULL,
  ConfigJson LONGTEXT NOT NULL,
  PRIMARY KEY (Id)
);

-- 3. streameromakases 테이블
CREATE TABLE IF NOT EXISTS streameromakases (
  Id INT NOT NULL AUTO_INCREMENT,
  ChzzkUid VARCHAR(50) NULL,
  Name VARCHAR(100) NOT NULL,
  Icon VARCHAR(10) NULL,
  Count INT NOT NULL DEFAULT 0,
  CheesePrice INT NOT NULL DEFAULT 0,
  PRIMARY KEY (Id)
);

-- 4. periodicmessages 테이블
CREATE TABLE IF NOT EXISTS periodicmessages (
  Id INT NOT NULL AUTO_INCREMENT,
  ChzzkUid VARCHAR(50) NOT NULL,
  Message LONGTEXT NOT NULL,
  IntervalMinutes INT NOT NULL DEFAULT 30,
  IsEnabled TINYINT(1) NOT NULL DEFAULT 1,
  LastSentAt DATETIME(6) NULL,
  PRIMARY KEY (Id)
);

-- 5. songlistsessions 테이블
CREATE TABLE IF NOT EXISTS songlistsessions (
  Id INT NOT NULL AUTO_INCREMENT,
  ChzzkUid VARCHAR(50) NULL,
  StartedAt DATETIME(6) NOT NULL,
  EndedAt DATETIME(6) NULL,
  IsActive TINYINT(1) NOT NULL DEFAULT 1,
  RequestCount INT NOT NULL DEFAULT 0,
  CompleteCount INT NOT NULL DEFAULT 0,
  PRIMARY KEY (Id)
);

-- 6. chzzkcategories 테이블
CREATE TABLE IF NOT EXISTS chzzkcategories (
  Id INT NOT NULL AUTO_INCREMENT,
  CategoryType VARCHAR(50) NULL,
  CategoryId VARCHAR(100) NULL,
  CategoryValue VARCHAR(200) NULL,
  PRIMARY KEY (Id)
);

-- 7. chzzkviewerprofiles 테이블
CREATE TABLE IF NOT EXISTS chzzkviewerprofiles (
  Id INT NOT NULL AUTO_INCREMENT,
  ChzzkUid VARCHAR(50) NULL,
  ViewerChannelId VARCHAR(100) NULL,
  Nickname VARCHAR(100) NULL,
  ChatPoint INT NOT NULL DEFAULT 0,
  TotalDonation INT NOT NULL DEFAULT 0,
  PRIMARY KEY (Id)
);

-- 8. roulettes 테이블
CREATE TABLE IF NOT EXISTS roulettes (
  Id INT NOT NULL AUTO_INCREMENT,
  ChzzkUid VARCHAR(50) NULL,
  Name LONGTEXT NOT NULL,
  Type VARCHAR(50) NOT NULL,
  Command VARCHAR(100) NULL,
  CostPerSpin INT NOT NULL DEFAULT 0,
  IsActive TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (Id)
);

-- 9. rouletteitems 테이블
CREATE TABLE IF NOT EXISTS rouletteitems (
  Id INT NOT NULL AUTO_INCREMENT,
  RouletteId INT NOT NULL,
  Name LONGTEXT NOT NULL,
  Probability DOUBLE NOT NULL DEFAULT 1.0,
  PRIMARY KEY (Id),
  FOREIGN KEY (RouletteId) REFERENCES roulettes(Id) ON DELETE CASCADE
);

SELECT 'Schema sync complete!' AS Result;
