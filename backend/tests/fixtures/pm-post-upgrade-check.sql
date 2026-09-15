DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM milestone_manager_vendor WHERE id='00000000-0000-4000-8000-000000000502' AND notes='Must survive migration' AND availability_json='{}'::jsonb AND service_areas_json='[]'::jsonb) THEN
    RAISE EXCEPTION 'Vendor migration lost data or applied invalid JSON defaults';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM milestone_manager_notice WHERE id='00000000-0000-4000-8000-000000000503' AND audience_owner_ids_json='[]'::jsonb AND audience_roles_json='[]'::jsonb) THEN
    RAISE EXCEPTION 'Notice audience defaults are invalid';
  END IF;
  IF (SELECT count(DISTINCT idempotency_key) FROM milestone_manager_gate_message WHERE manager_user_id='00000000-0000-4000-8000-000000000501' AND idempotency_key LIKE 'legacy:%') <> 2 THEN
    RAISE EXCEPTION 'Existing gate messages lost their independent identity';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM milestone_directory_provider WHERE id='00000000-0000-4000-8000-000000000506' AND services_json='[]'::jsonb) THEN
    RAISE EXCEPTION 'Directory migration did not preserve the existing provider';
  END IF;
END $$;
SELECT 'Populated migration checks passed' AS result;
