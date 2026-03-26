-- 1. 테스트용 스트리머 프로필 생성
INSERT INTO streamerprofiles (ChzzkUid, Nickname, SongCommand, SongPrice, IsBotEnabled, SonglistSessionActive) 
VALUES ('test_uid_123', '테스트스트리머', '!신청', 100, 1, 1);

-- 2. 테스트용 곡 목록 생성 (50개)
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', '좋은날', '아이유', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', '밤편지', '아이유', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Hype Boy', 'NewJeans', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Ditto', 'NewJeans', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'OMG', 'NewJeans', NOW(), NOW());

-- 반복 삽입을 위한 루프 대체 (단순 노가다로 20개 정도 생성)
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 1', 'Artist A', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 2', 'Artist B', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 3', 'Artist C', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 4', 'Artist D', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 5', 'Artist E', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 6', 'Artist F', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 7', 'Artist G', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 8', 'Artist H', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 9', 'Artist I', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 10', 'Artist J', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 11', 'Artist K', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 12', 'Artist L', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 13', 'Artist M', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 14', 'Artist N', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 15', 'Artist O', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 16', 'Artist P', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 17', 'Artist Q', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 18', 'Artist R', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 19', 'Artist S', NOW(), NOW());
INSERT INTO songbooks (ChzzkUid, Title, Artist, CreatedAt, UpdatedAt) VALUES ('test_uid_123', 'Test Song 20', 'Artist T', NOW(), NOW());

SELECT 'Seed data inserted successfully!' AS Status;
