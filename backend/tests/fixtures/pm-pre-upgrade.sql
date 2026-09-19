-- Synthetic rows for a disposable database at 20260905160110.
-- Never run this fixture against a client or production database.
BEGIN;
INSERT INTO milestone_user (id,email,normalized_email,password_hash,display_name,two_factor_secret,roles_json,created_at,updated_at,is_deleted,is_two_factor_enabled,status)
VALUES ('00000000-0000-4000-8000-000000000501','migration-fixture@system.invalid','MIGRATION-FIXTURE@SYSTEM.INVALID','disabled','Migration fixture',decode('','hex'),'["PropertyManager"]',now(),now(),false,false,'Disabled');
INSERT INTO milestone_manager_vendor (id,manager_user_id,name,category,contact,verification_status,is_active,notes,created_at,updated_at,is_deleted)
VALUES ('00000000-0000-4000-8000-000000000502','00000000-0000-4000-8000-000000000501','Preserved vendor','MAINTENANCE','Synthetic','PENDING',true,'Must survive migration',now(),now(),false);
INSERT INTO milestone_manager_notice (id,manager_user_id,title,body,publish_at,is_pinned,is_archived,created_at,updated_at,is_deleted)
VALUES ('00000000-0000-4000-8000-000000000503','00000000-0000-4000-8000-000000000501','Preserved notice','Must survive migration',now(),false,false,now(),now(),false);
INSERT INTO milestone_manager_gate_message (id,manager_user_id,recipient,message,visitor_type,valid_from,valid_until,created_at,updated_at,is_deleted)
SELECT id::uuid,'00000000-0000-4000-8000-000000000501','Synthetic guard','Must survive migration','VENDOR',now(),now()+interval '1 hour',now(),now(),false
FROM (VALUES ('00000000-0000-4000-8000-000000000504'),('00000000-0000-4000-8000-000000000505')) AS ids(id);
INSERT INTO milestone_directory_provider (id,slug,kind,category,name,parish,badge_level,description,availability_summary,contact_mode,rating,review_count,is_active,created_at,updated_at,is_deleted)
VALUES ('00000000-0000-4000-8000-000000000506','migration-fixture','Trades','Plumbing','Preserved trades fixture','Kingston','Verified','Synthetic','Weekdays','Message',0,0,false,now(),now(),false);
COMMIT;
