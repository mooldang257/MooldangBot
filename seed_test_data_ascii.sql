-- 1. Create Test Streamer Profile with all required fields
INSERT INTO streamerprofiles (
    ChzzkUid, ChannelName, SongCommand, SongPrice, IsBotEnabled, SonglistSessionActive, 
    OmakaseCount, OmakaseCommand, OmakasePrice, AttendanceCommands, AttendanceReply, 
    PointCheckCommand, PointCheckReply, IsOmakaseEnabled
) 
VALUES (
    'test_uid_123', 'TestStreamer', '!song', 100, 1, 1, 
    0, '!omakase', 1000, 'hello,hi', 'Welcome!', 
    '!point', 'Your points: {point}', 1
);

-- 2. Create Test SongBook Entries (25 entries)
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Song A', 'Artist 1', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Song B', 'Artist 2', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Song C', 'Artist 3', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Gold Day', 'Singer A', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Night Letter', 'Singer A', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Hype Boy', 'NewJeans', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Ditto', 'NewJeans', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'OMG', 'NewJeans', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 1', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 2', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 3', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 4', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 5', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 6', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 7', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 8', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 9', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 10', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 11', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 12', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 13', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 14', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 15', 'Artist X', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Extra 16', 'Artist X', NOW(), NOW());

SELECT 'ASCII Seed data inserted successfully!' AS Status;
