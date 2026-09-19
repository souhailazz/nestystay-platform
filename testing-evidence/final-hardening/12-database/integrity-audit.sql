\pset pager off
\timing on

SELECT current_database() AS database, current_user AS role, version();

SELECT 'invalid_booking_dates_or_amounts' AS check_name, COUNT(*) AS violations
FROM milestone_booking
WHERE NOT is_deleted AND (check_out <= check_in OR nights <= 0 OR nightly_rate < 0 OR stay_subtotal < 0 OR guest_platform_fee < 0 OR total_amount < 0 OR refunded_amount < 0 OR refunded_amount > total_amount)
UNION ALL
SELECT 'orphan_booking_property', COUNT(*) FROM milestone_booking b LEFT JOIN milestone_property p ON p.id=b.property_id WHERE NOT b.is_deleted AND p.id IS NULL
UNION ALL
SELECT 'orphan_booking_guest', COUNT(*) FROM milestone_booking b LEFT JOIN milestone_user u ON u.id=b.guest_user_id WHERE NOT b.is_deleted AND u.id IS NULL
UNION ALL
SELECT 'orphan_booking_host', COUNT(*) FROM milestone_booking b LEFT JOIN milestone_user u ON u.id=b.host_user_id WHERE NOT b.is_deleted AND u.id IS NULL
UNION ALL
SELECT 'orphan_property_host', COUNT(*) FROM milestone_property p LEFT JOIN milestone_user u ON u.id=p.host_user_id WHERE NOT p.is_deleted AND u.id IS NULL
UNION ALL
SELECT 'invalid_manager_invoice_totals', COUNT(*) FROM milestone_manager_invoice WHERE NOT is_deleted AND (subtotal < 0 OR tax < 0 OR total <> subtotal + tax OR amount_paid < 0 OR amount_paid > total OR balance <> total - amount_paid)
UNION ALL
SELECT 'orphan_manager_invoice_owner', COUNT(*) FROM milestone_manager_invoice i LEFT JOIN milestone_user u ON u.id=i.owner_user_id WHERE NOT i.is_deleted AND u.id IS NULL
UNION ALL
SELECT 'orphan_manager_invoice_property', COUNT(*) FROM milestone_manager_invoice i LEFT JOIN milestone_manager_property p ON p.id=i.property_id WHERE NOT i.is_deleted AND p.id IS NULL
UNION ALL
SELECT 'orphan_manager_invoice_line', COUNT(*) FROM milestone_manager_invoice_line l LEFT JOIN milestone_manager_invoice i ON i.id=l.invoice_id WHERE NOT l.is_deleted AND i.id IS NULL
UNION ALL
SELECT 'orphan_manager_payment_invoice', COUNT(*) FROM milestone_manager_payment p LEFT JOIN milestone_manager_invoice i ON i.id=p.invoice_id WHERE NOT p.is_deleted AND i.id IS NULL
UNION ALL
SELECT 'manager_payment_sum_exceeds_invoice', COUNT(*) FROM (SELECT i.id FROM milestone_manager_invoice i LEFT JOIN milestone_manager_payment p ON p.invoice_id=i.id AND NOT p.is_deleted AND p.status='SUCCEEDED' WHERE NOT i.is_deleted GROUP BY i.id,i.total HAVING COALESCE(SUM(p.amount),0) > i.total) x
UNION ALL
SELECT 'duplicate_invoice_numbers_per_manager', COUNT(*) FROM (SELECT manager_user_id, invoice_number FROM milestone_manager_invoice WHERE NOT is_deleted GROUP BY manager_user_id,invoice_number HAVING COUNT(*)>1) x
UNION ALL
SELECT 'duplicate_active_manager_owner_links', COUNT(*) FROM (SELECT manager_user_id,owner_user_id FROM milestone_manager_owner WHERE NOT is_deleted GROUP BY manager_user_id,owner_user_id HAVING COUNT(*)>1) x
UNION ALL
SELECT 'duplicate_eligible_voters', COUNT(*) FROM (SELECT proposal_id,owner_user_id FROM milestone_manager_eligible_voter WHERE NOT is_deleted GROUP BY proposal_id,owner_user_id HAVING COUNT(*)>1) x
UNION ALL
SELECT 'duplicate_active_proxies', COUNT(*) FROM (SELECT proposal_id,owner_user_id FROM milestone_manager_proxy WHERE NOT is_deleted GROUP BY proposal_id,owner_user_id HAVING COUNT(*)>1) x
UNION ALL
SELECT 'duplicate_ballot_hashes', COUNT(*) FROM (SELECT ballot_hash FROM milestone_manager_vote WHERE NOT is_deleted GROUP BY ballot_hash HAVING COUNT(*)>1) x
UNION ALL
SELECT 'invalid_qr_windows', COUNT(*) FROM milestone_manager_qr_access WHERE NOT is_deleted AND valid_until <= valid_from
UNION ALL
SELECT 'invalid_wellness_visit_money', COUNT(*) FROM milestone_wellness_visit WHERE NOT is_deleted AND (price < 0 OR platform_fee < 0 OR officer_payout_amount < 0 OR price <> platform_fee + officer_payout_amount)
UNION ALL
SELECT 'invalid_wellness_payout_money', COUNT(*) FROM milestone_wellness_payout WHERE NOT is_deleted AND (gross_amount < 0 OR platform_fee < 0 OR officer_amount < 0 OR gross_amount <> platform_fee + officer_amount)
UNION ALL
SELECT 'orphan_wellness_visit_property', COUNT(*) FROM milestone_wellness_visit v LEFT JOIN milestone_property p ON p.id=v.property_id WHERE NOT v.is_deleted AND p.id IS NULL
UNION ALL
SELECT 'orphan_wellness_visit_host', COUNT(*) FROM milestone_wellness_visit v LEFT JOIN milestone_user u ON u.id=v.host_user_id WHERE NOT v.is_deleted AND u.id IS NULL
UNION ALL
SELECT 'orphan_wellness_payout_visit', COUNT(*) FROM milestone_wellness_payout p LEFT JOIN milestone_wellness_visit v ON v.id=p.visit_id WHERE NOT p.is_deleted AND v.id IS NULL
UNION ALL
SELECT 'orphan_conversation_participant_user', COUNT(*) FROM milestone_conversation_participant p LEFT JOIN milestone_user u ON u.id=p.user_id WHERE NOT p.is_deleted AND u.id IS NULL
UNION ALL
SELECT 'orphan_message_conversation', COUNT(*) FROM milestone_message m LEFT JOIN milestone_conversation c ON c.id=m.conversation_id WHERE NOT m.is_deleted AND c.id IS NULL
UNION ALL
SELECT 'orphan_message_sender', COUNT(*) FROM milestone_message m LEFT JOIN milestone_user u ON u.id=m.sender_user_id WHERE NOT m.is_deleted AND u.id IS NULL
UNION ALL
SELECT 'multiple_default_payment_methods', COUNT(*) FROM (SELECT user_id FROM milestone_traveler_payment_method WHERE NOT is_deleted AND is_default GROUP BY user_id HAVING COUNT(*)>1) x
ORDER BY check_name;

SELECT COUNT(*) AS total_integrity_violations FROM (
  SELECT id FROM milestone_booking WHERE NOT is_deleted AND (check_out <= check_in OR total_amount < 0 OR refunded_amount < 0 OR refunded_amount > total_amount)
  UNION ALL SELECT b.id FROM milestone_booking b LEFT JOIN milestone_property p ON p.id=b.property_id WHERE NOT b.is_deleted AND p.id IS NULL
  UNION ALL SELECT l.id FROM milestone_manager_invoice_line l LEFT JOIN milestone_manager_invoice i ON i.id=l.invoice_id WHERE NOT l.is_deleted AND i.id IS NULL
  UNION ALL SELECT p.id FROM milestone_manager_payment p LEFT JOIN milestone_manager_invoice i ON i.id=p.invoice_id WHERE NOT p.is_deleted AND i.id IS NULL
  UNION ALL SELECT m.id FROM milestone_message m LEFT JOIN milestone_conversation c ON c.id=m.conversation_id WHERE NOT m.is_deleted AND c.id IS NULL
) violations;

SELECT con.contype, COUNT(*) AS constraint_count
FROM pg_constraint con
JOIN pg_namespace ns ON ns.oid=con.connamespace
WHERE ns.nspname='public'
GROUP BY con.contype
ORDER BY con.contype;

SELECT COUNT(*) AS table_count FROM pg_tables WHERE schemaname='public';
SELECT COUNT(*) AS index_count FROM pg_indexes WHERE schemaname='public';
SELECT COUNT(*) AS migration_count FROM "__EFMigrationsHistory";

EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM milestone_property WHERE host_user_id=(SELECT host_user_id FROM milestone_property LIMIT 1) AND NOT is_deleted;
EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM milestone_booking WHERE guest_user_id=(SELECT guest_user_id FROM milestone_booking LIMIT 1) AND NOT is_deleted ORDER BY created_at DESC;
EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM milestone_manager_invoice WHERE owner_user_id=(SELECT owner_user_id FROM milestone_manager_invoice LIMIT 1) AND NOT is_deleted ORDER BY due_date DESC;
EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM milestone_conversation_participant WHERE user_id=(SELECT user_id FROM milestone_conversation_participant LIMIT 1) AND NOT is_deleted;
EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM milestone_wellness_visit WHERE host_user_id=(SELECT host_user_id FROM milestone_wellness_visit LIMIT 1) AND NOT is_deleted ORDER BY scheduled_at DESC;
