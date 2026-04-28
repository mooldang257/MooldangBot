-- 🔱 [MooldangBot] GlobalViewer 중복 정리 및 유니크 인덱스 적용 v1.1 (테이블명 수정)
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO';
START TRANSACTION;

-- 1. 중복된 UID 중 유지할 ID(최솟값)와 삭제할 ID들을 매핑하는 임시 테이블 생성
CREATE TEMPORARY TABLE tmp_duplicates AS
SELECT 
    MIN(id) as keep_id, 
    GROUP_CONCAT(id) as all_ids,
    LOWER(TRIM(viewer_uid)) as normalized_uid
FROM core_global_viewers
GROUP BY normalized_uid
HAVING COUNT(*) > 1;

-- 2. 자식 테이블 참조 업데이트
UPDATE func_viewer_donation_histories vdh
JOIN tmp_duplicates td ON FIND_IN_SET(vdh.global_viewer_id, td.all_ids)
SET vdh.global_viewer_id = td.keep_id
WHERE vdh.global_viewer_id != td.keep_id;

UPDATE func_viewer_donations vd
JOIN tmp_duplicates td ON FIND_IN_SET(vd.global_viewer_id, td.all_ids)
SET vd.global_viewer_id = td.keep_id
WHERE vd.global_viewer_id != td.keep_id;

UPDATE func_viewer_points vp
JOIN tmp_duplicates td ON FIND_IN_SET(vp.global_viewer_id, td.all_ids)
SET vp.global_viewer_id = td.keep_id
WHERE vp.global_viewer_id != td.keep_id;

UPDATE core_viewer_relations vr
JOIN tmp_duplicates td ON FIND_IN_SET(vr.global_viewer_id, td.all_ids)
SET vr.global_viewer_id = td.keep_id
WHERE vr.global_viewer_id != td.keep_id;

UPDATE func_roulette_spins frs
JOIN tmp_duplicates td ON FIND_IN_SET(frs.global_viewer_id, td.all_ids)
SET frs.global_viewer_id = td.keep_id
WHERE frs.global_viewer_id != td.keep_id;

UPDATE log_roulette_results lrr
JOIN tmp_duplicates td ON FIND_IN_SET(lrr.global_viewer_id, td.all_ids)
SET lrr.global_viewer_id = td.keep_id
WHERE lrr.global_viewer_id != td.keep_id;

UPDATE func_song_list_queues fslq
JOIN tmp_duplicates td ON FIND_IN_SET(fslq.global_viewer_id, td.all_ids)
SET fslq.global_viewer_id = td.keep_id
WHERE fslq.global_viewer_id != td.keep_id;

UPDATE log_command_executions lce
JOIN tmp_duplicates td ON FIND_IN_SET(lce.global_viewer_id, td.all_ids)
SET lce.global_viewer_id = td.keep_id
WHERE lce.global_viewer_id != td.keep_id;

UPDATE log_point_transactions lpt
JOIN tmp_duplicates td ON FIND_IN_SET(lpt.global_viewer_id, td.all_ids)
SET lpt.global_viewer_id = td.keep_id
WHERE lpt.global_viewer_id != td.keep_id;

UPDATE core_streamer_managers csm
JOIN tmp_duplicates td ON FIND_IN_SET(csm.global_viewer_id, td.all_ids)
SET csm.global_viewer_id = td.keep_id
WHERE csm.global_viewer_id != td.keep_id;

-- 3. 중복된 글로벌 프로필 삭제
DELETE v FROM core_global_viewers v
JOIN tmp_duplicates td ON LOWER(TRIM(v.viewer_uid)) = td.normalized_uid
WHERE v.id != td.keep_id;

-- 4. 스키마 변경
ALTER TABLE core_global_viewers MODIFY COLUMN viewer_uid VARCHAR(255) NOT NULL;
CREATE UNIQUE INDEX ix_core_global_viewers_viewer_uid ON core_global_viewers (viewer_uid);

COMMIT;
SET SQL_MODE=@OLD_SQL_MODE;
