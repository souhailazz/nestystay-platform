CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE arrears_record (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    owner_user_id uuid NOT NULL,
    amount_overdue numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    months_overdue integer NOT NULL,
    last_reminder_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_arrears_record" PRIMARY KEY (id)
);

CREATE TABLE association_storage_plan (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    monthly_price numeric(18,2) NOT NULL,
    annual_price numeric(18,2) NOT NULL,
    storage_megabytes integer NOT NULL,
    retention_years integer NOT NULL,
    includes_zoom_archive boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_association_storage_plan" PRIMARY KEY (id)
);

CREATE TABLE audit_log (
    id uuid NOT NULL,
    actor_user_id uuid,
    action character varying(512) NOT NULL,
    entity_type character varying(512) NOT NULL,
    entity_id uuid,
    before_json jsonb,
    after_json jsonb,
    ip_address character varying(512),
    user_agent character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_audit_log" PRIMARY KEY (id)
);

CREATE TABLE badge_assignment (
    id uuid NOT NULL,
    badge_definition_id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    status character varying(128) NOT NULL,
    earned_at timestamp with time zone,
    paid_through timestamp with time zone,
    expires_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_badge_assignment" PRIMARY KEY (id)
);

CREATE TABLE badge_definition (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    level character varying(128) NOT NULL,
    applies_to character varying(512) NOT NULL,
    unlocks_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_badge_definition" PRIMARY KEY (id)
);

CREATE TABLE badge_renewal (
    id uuid NOT NULL,
    badge_assignment_id uuid NOT NULL,
    reminder_due_at timestamp with time zone NOT NULL,
    payment_attempted_at timestamp with time zone,
    payment_status character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_badge_renewal" PRIMARY KEY (id)
);

CREATE TABLE bid_opening (
    id uuid NOT NULL,
    meeting_id uuid NOT NULL,
    sealed_bids_json jsonb NOT NULL,
    revealed_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_bid_opening" PRIMARY KEY (id)
);

CREATE TABLE booking (
    id uuid NOT NULL,
    guest_user_id uuid NOT NULL,
    property_id uuid NOT NULL,
    check_in date NOT NULL,
    check_out date NOT NULL,
    status character varying(128) NOT NULL,
    requires_guest_verification boolean NOT NULL,
    hold_expires_at timestamp with time zone,
    total_amount numeric(18,2) NOT NULL,
    paid_amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_booking" PRIMARY KEY (id)
);

CREATE TABLE booking_cancellation (
    id uuid NOT NULL,
    booking_id uuid NOT NULL,
    policy_type character varying(128) NOT NULL,
    refund_amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    reason character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_booking_cancellation" PRIMARY KEY (id)
);

CREATE TABLE booking_dispute (
    id uuid NOT NULL,
    booking_id uuid NOT NULL,
    opened_by_user_id uuid NOT NULL,
    reason character varying(512) NOT NULL,
    status character varying(512) NOT NULL,
    resolution character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_booking_dispute" PRIMARY KEY (id)
);

CREATE TABLE booking_guest (
    id uuid NOT NULL,
    booking_id uuid NOT NULL,
    full_name character varying(512) NOT NULL,
    is_primary boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_booking_guest" PRIMARY KEY (id)
);

CREATE TABLE booking_payment_schedule (
    id uuid NOT NULL,
    booking_id uuid NOT NULL,
    schedule_type character varying(128) NOT NULL,
    due_at timestamp with time zone NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    status character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_booking_payment_schedule" PRIMARY KEY (id)
);

CREATE TABLE booking_price_line (
    id uuid NOT NULL,
    booking_id uuid NOT NULL,
    line_type character varying(512) NOT NULL,
    description character varying(512) NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    is_refundable boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_booking_price_line" PRIMARY KEY (id)
);

CREATE TABLE booking_status_event (
    id uuid NOT NULL,
    booking_id uuid NOT NULL,
    from_status character varying(128) NOT NULL,
    to_status character varying(128) NOT NULL,
    reason character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_booking_status_event" PRIMARY KEY (id)
);

CREATE TABLE campaign (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    name character varying(512) NOT NULL,
    campaign_type character varying(512) NOT NULL,
    opens_at timestamp with time zone,
    closes_at timestamp with time zone,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_campaign" PRIMARY KEY (id)
);

CREATE TABLE campaign_enrollment (
    id uuid NOT NULL,
    campaign_id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    enrolled_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_campaign_enrollment" PRIMARY KEY (id)
);

CREATE TABLE community (
    id uuid NOT NULL,
    name character varying(512) NOT NULL,
    address character varying(512) NOT NULL,
    governance_mode character varying(128) NOT NULL,
    has_licensed_manager boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_community" PRIMARY KEY (id)
);

CREATE TABLE community_announcement (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    posted_by_user_id uuid NOT NULL,
    title character varying(512) NOT NULL,
    body character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_community_announcement" PRIMARY KEY (id)
);

CREATE TABLE community_membership (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    user_id uuid NOT NULL,
    role character varying(128) NOT NULL,
    can_view_tenant_content boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_community_membership" PRIMARY KEY (id)
);

CREATE TABLE conversation_thread (
    id uuid NOT NULL,
    thread_type character varying(512) NOT NULL,
    booking_id uuid,
    booking_code character varying(512),
    retention_expires_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_conversation_thread" PRIMARY KEY (id)
);

CREATE TABLE directory_commission (
    id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    commission_percent numeric(18,2) NOT NULL,
    active_from timestamp with time zone NOT NULL,
    active_to timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_directory_commission" PRIMARY KEY (id)
);

CREATE TABLE directory_review (
    id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    reviewer_user_id uuid NOT NULL,
    rating numeric(18,2) NOT NULL,
    is_verified boolean NOT NULL,
    comment character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_directory_review" PRIMARY KEY (id)
);

CREATE TABLE document_retention_rule (
    id uuid NOT NULL,
    document_type character varying(512) NOT NULL,
    retention_years integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_document_retention_rule" PRIMARY KEY (id)
);

CREATE TABLE document_vault_item (
    id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    storage_object_id uuid NOT NULL,
    document_type character varying(512) NOT NULL,
    retain_until timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_document_vault_item" PRIMARY KEY (id)
);

CREATE TABLE escrow_hold (
    id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    recipient_user_id uuid NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    status character varying(128) NOT NULL,
    auto_release_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_escrow_hold" PRIMARY KEY (id)
);

CREATE TABLE financial_statement_version (
    id uuid NOT NULL,
    meeting_id uuid NOT NULL,
    version_type character varying(512) NOT NULL,
    storage_object_id uuid NOT NULL,
    sent_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_financial_statement_version" PRIMARY KEY (id)
);

CREATE TABLE identity_document (
    id uuid NOT NULL,
    subject_type character varying(128) NOT NULL,
    subject_id uuid NOT NULL,
    storage_object_id uuid NOT NULL,
    encrypted_metadata_json jsonb NOT NULL,
    issuing_country character varying(512),
    expires_on date,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_identity_document" PRIMARY KEY (id)
);

CREATE TABLE integration_failover (
    id uuid NOT NULL,
    kind character varying(128) NOT NULL,
    from_provider character varying(512) NOT NULL,
    to_provider character varying(512) NOT NULL,
    switched_by_user_id uuid NOT NULL,
    reason character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_integration_failover" PRIMARY KEY (id)
);

CREATE TABLE invoice (
    id uuid NOT NULL,
    invoice_number character varying(512) NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    status character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_invoice" PRIMARY KEY (id)
);

CREATE TABLE invoice_line (
    id uuid NOT NULL,
    invoice_id uuid NOT NULL,
    description character varying(512) NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_invoice_line" PRIMARY KEY (id)
);

CREATE TABLE local_business (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    business_name character varying(512) NOT NULL,
    is_brick_and_mortar boolean NOT NULL,
    has_legal_documents boolean NOT NULL,
    average_rating numeric(18,2) NOT NULL,
    verified_review_count integer NOT NULL,
    standing character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_local_business" PRIMARY KEY (id)
);

CREATE TABLE maintenance_request (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    property_unit_id uuid,
    submitted_by_user_id uuid NOT NULL,
    issue character varying(512) NOT NULL,
    priority character varying(512) NOT NULL,
    status character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_maintenance_request" PRIMARY KEY (id)
);

CREATE TABLE manager_statement (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    owner_user_id uuid,
    statement_type character varying(512) NOT NULL,
    statement_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_manager_statement" PRIMARY KEY (id)
);

CREATE TABLE meeting (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    title character varying(512) NOT NULL,
    meeting_at timestamp with time zone NOT NULL,
    status character varying(128) NOT NULL,
    zoom_archive_url character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_meeting" PRIMARY KEY (id)
);

CREATE TABLE meeting_document (
    id uuid NOT NULL,
    meeting_id uuid NOT NULL,
    storage_object_id uuid NOT NULL,
    document_type character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_meeting_document" PRIMARY KEY (id)
);

CREATE TABLE message (
    id uuid NOT NULL,
    conversation_thread_id uuid NOT NULL,
    sender_user_id uuid NOT NULL,
    channel character varying(512) NOT NULL,
    body character varying(512) NOT NULL,
    retention_expires_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_message" PRIMARY KEY (id)
);

CREATE TABLE notification_queue_item (
    id uuid NOT NULL,
    recipient_user_id uuid,
    channel character varying(512) NOT NULL,
    recipient character varying(512) NOT NULL,
    subject character varying(512) NOT NULL,
    body character varying(512) NOT NULL,
    status character varying(128) NOT NULL,
    sent_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_notification_queue_item" PRIMARY KEY (id)
);

CREATE TABLE notification_template (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    channel character varying(512) NOT NULL,
    subject_template character varying(512) NOT NULL,
    body_template character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_notification_template" PRIMARY KEY (id)
);

CREATE TABLE officer (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    is_active_jcf boolean NOT NULL,
    is_retired boolean NOT NULL,
    current_nesty_stay_id character varying(512) NOT NULL,
    eligibility_status character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_officer" PRIMARY KEY (id)
);

CREATE TABLE officer_id_history (
    id uuid NOT NULL,
    officer_id uuid NOT NULL,
    nesty_stay_id character varying(512) NOT NULL,
    year integer NOT NULL,
    is_retired_identifier boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_officer_id_history" PRIMARY KEY (id)
);

CREATE TABLE owner_unit (
    id uuid NOT NULL,
    owner_user_id uuid NOT NULL,
    property_unit_id uuid NOT NULL,
    ownership_started_at timestamp with time zone NOT NULL,
    ownership_ended_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_owner_unit" PRIMARY KEY (id)
);

CREATE TABLE payment_account (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    provider character varying(512) NOT NULL,
    external_account_id character varying(512) NOT NULL,
    is_payout_enabled boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_payment_account" PRIMARY KEY (id)
);

CREATE TABLE payment_intent_record (
    id uuid NOT NULL,
    booking_id uuid,
    provider character varying(512) NOT NULL,
    external_intent_id character varying(512) NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    status character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_payment_intent_record" PRIMARY KEY (id)
);

CREATE TABLE payment_transaction (
    id uuid NOT NULL,
    payment_intent_record_id uuid NOT NULL,
    transaction_type character varying(512) NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    external_transaction_id character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_payment_transaction" PRIMARY KEY (id)
);

CREATE TABLE payout (
    id uuid NOT NULL,
    recipient_user_id uuid NOT NULL,
    provider character varying(512) NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    status character varying(512) NOT NULL,
    external_transfer_id character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_payout" PRIMARY KEY (id)
);

CREATE TABLE pricebook_entry (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    label character varying(512) NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency_or_unit character varying(512) NOT NULL,
    cadence character varying(512) NOT NULL,
    applies_to character varying(512) NOT NULL,
    active_from timestamp with time zone,
    active_to timestamp with time zone,
    is_configurable boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_pricebook_entry" PRIMARY KEY (id)
);

CREATE TABLE property (
    id uuid NOT NULL,
    host_user_id uuid NOT NULL,
    title character varying(512) NOT NULL,
    address_line1 character varying(512) NOT NULL,
    parish character varying(512),
    country character varying(512) NOT NULL,
    status character varying(128) NOT NULL,
    highest_badge character varying(128) NOT NULL,
    is_verification_opted_out boolean NOT NULL,
    is_guest_verification_enabled boolean NOT NULL,
    is_insura_guest_enabled boolean NOT NULL,
    cancellation_policy character varying(128) NOT NULL,
    custom_cancellation_terms character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_property" PRIMARY KEY (id)
);

CREATE TABLE property_availability (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    starts_on date NOT NULL,
    ends_on date NOT NULL,
    availability_type character varying(512) NOT NULL,
    booking_id uuid,
    hold_expires_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_property_availability" PRIMARY KEY (id)
);

CREATE TABLE property_founding_benefit (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    tier character varying(128) NOT NULL,
    guest_flat_fee numeric(18,2) NOT NULL,
    host_commission_percent numeric(18,2) NOT NULL,
    is_lifetime_guest_fee boolean NOT NULL,
    is_transferable_with_property boolean NOT NULL,
    is_forfeited boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_property_founding_benefit" PRIMARY KEY (id)
);

CREATE TABLE property_media (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    storage_object_id uuid NOT NULL,
    media_type character varying(512) NOT NULL,
    sort_order integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_property_media" PRIMARY KEY (id)
);

CREATE TABLE property_pricing_rule (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    nightly_rate numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    seven_night_discount_percent numeric(18,2) NOT NULL,
    fourteen_night_discount_percent numeric(18,2) NOT NULL,
    twenty_eight_night_discount_percent numeric(18,2) NOT NULL,
    market_override_key character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_property_pricing_rule" PRIMARY KEY (id)
);

CREATE TABLE property_transfer_request (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    previous_owner_user_id uuid NOT NULL,
    new_owner_user_id uuid NOT NULL,
    tax_receipt_storage_object_id uuid NOT NULL,
    previous_owner_verified_and_trusted boolean NOT NULL,
    status character varying(512) NOT NULL,
    admin_notes character varying(512),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_property_transfer_request" PRIMARY KEY (id)
);

CREATE TABLE property_unit (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    community_id uuid,
    unit_number character varying(512) NOT NULL,
    bedrooms integer NOT NULL,
    bathrooms integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_property_unit" PRIMARY KEY (id)
);

CREATE TABLE provider_config (
    id uuid NOT NULL,
    kind character varying(128) NOT NULL,
    provider_name character varying(512) NOT NULL,
    is_primary boolean NOT NULL,
    encrypted_config_reference character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_provider_config" PRIMARY KEY (id)
);

CREATE TABLE provider_event (
    id uuid NOT NULL,
    kind character varying(128) NOT NULL,
    provider_name character varying(512) NOT NULL,
    event_type character varying(512) NOT NULL,
    payload_json jsonb NOT NULL,
    received_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_provider_event" PRIMARY KEY (id)
);

CREATE TABLE proxy (
    id uuid NOT NULL,
    meeting_id uuid NOT NULL,
    owner_user_id uuid NOT NULL,
    cutoff_option character varying(128) NOT NULL,
    custom_cutoff_at timestamp with time zone,
    is_eligible boolean NOT NULL,
    is_sealed boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_proxy" PRIMARY KEY (id)
);

CREATE TABLE qr_access_code (
    id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    code_hash character varying(512) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    is_revoked boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_qr_access_code" PRIMARY KEY (id)
);

CREATE TABLE qr_scan_log (
    id uuid NOT NULL,
    qr_access_code_id uuid NOT NULL,
    gate_guard_user_id uuid,
    scanned_at timestamp with time zone NOT NULL,
    result character varying(512) NOT NULL,
    device_metadata_json jsonb,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_qr_scan_log" PRIMARY KEY (id)
);

CREATE TABLE rating_policy (
    id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    minimum_reviews_before_enforcement numeric(18,2) NOT NULL,
    top_rated_minimum numeric(18,2) NOT NULL,
    good_standing_minimum numeric(18,2) NOT NULL,
    warning_minimum numeric(18,2) NOT NULL,
    final_warning_minimum numeric(18,2) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_rating_policy" PRIMARY KEY (id)
);

CREATE TABLE role (
    id uuid NOT NULL,
    key character varying(128) NOT NULL,
    name character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_role" PRIMARY KEY (id)
);

CREATE TABLE service_job (
    id uuid NOT NULL,
    service_provider_profile_id uuid NOT NULL,
    requested_by_user_id uuid NOT NULL,
    status character varying(512) NOT NULL,
    quote_amount numeric(18,2),
    currency character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_service_job" PRIMARY KEY (id)
);

CREATE TABLE service_provider_profile (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    provider_type character varying(128) NOT NULL,
    display_name character varying(512) NOT NULL,
    average_rating numeric(18,2) NOT NULL,
    verified_review_count integer NOT NULL,
    status character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_service_provider_profile" PRIMARY KEY (id)
);

CREATE TABLE service_provider_sponsorship (
    id uuid NOT NULL,
    service_provider_profile_id uuid NOT NULL,
    sponsor_host_user_id uuid NOT NULL,
    starts_at timestamp with time zone NOT NULL,
    ends_at timestamp with time zone NOT NULL,
    withdrawn_at timestamp with time zone,
    replacement_due_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_service_provider_sponsorship" PRIMARY KEY (id)
);

CREATE TABLE staff_assignment (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    staff_user_id uuid NOT NULL,
    staff_type character varying(512) NOT NULL,
    schedule_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_staff_assignment" PRIMARY KEY (id)
);

CREATE TABLE storage_object (
    id uuid NOT NULL,
    provider character varying(512) NOT NULL,
    bucket character varying(512) NOT NULL,
    object_key character varying(512) NOT NULL,
    content_type character varying(512) NOT NULL,
    size_bytes bigint NOT NULL,
    checksum character varying(512),
    access_scope character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_storage_object" PRIMARY KEY (id)
);

CREATE TABLE subscription (
    id uuid NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    subscription_type character varying(512) NOT NULL,
    provider character varying(512) NOT NULL,
    status character varying(512) NOT NULL,
    renews_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_subscription" PRIMARY KEY (id)
);

CREATE TABLE "user" (
    id uuid NOT NULL,
    email character varying(512) NOT NULL,
    phone character varying(512),
    display_name character varying(512) NOT NULL,
    external_auth_subject character varying(512),
    is_two_factor_enabled boolean NOT NULL,
    status character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_user" PRIMARY KEY (id)
);

CREATE TABLE user_consent (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    type character varying(128) NOT NULL,
    version character varying(512) NOT NULL,
    accepted_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_user_consent" PRIMARY KEY (id)
);

CREATE TABLE user_role_assignment (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_user_role_assignment" PRIMARY KEY (id)
);

CREATE TABLE utility_bill (
    id uuid NOT NULL,
    community_id uuid NOT NULL,
    storage_object_id uuid NOT NULL,
    utility_type character varying(512) NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    allocation_method character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_utility_bill" PRIMARY KEY (id)
);

CREATE TABLE verification_check (
    id uuid NOT NULL,
    subject_type character varying(128) NOT NULL,
    subject_id uuid NOT NULL,
    provider character varying(512) NOT NULL,
    status character varying(128) NOT NULL,
    cost_amount numeric(18,2) NOT NULL,
    cost_currency character varying(512) NOT NULL,
    document_type character varying(512),
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_verification_check" PRIMARY KEY (id)
);

CREATE TABLE verification_event (
    id uuid NOT NULL,
    verification_check_id uuid NOT NULL,
    provider character varying(512) NOT NULL,
    event_type character varying(512) NOT NULL,
    payload_json jsonb NOT NULL,
    received_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_verification_event" PRIMARY KEY (id)
);

CREATE TABLE visitor_log (
    id uuid NOT NULL,
    community_id uuid,
    unit_id uuid,
    visitor_name character varying(512) NOT NULL,
    purpose character varying(512) NOT NULL,
    logged_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_visitor_log" PRIMARY KEY (id)
);

CREATE TABLE vote (
    id uuid NOT NULL,
    meeting_id uuid NOT NULL,
    owner_user_id uuid NOT NULL,
    encrypted_vote_payload character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_vote" PRIMARY KEY (id)
);

CREATE TABLE vote_result (
    id uuid NOT NULL,
    meeting_id uuid NOT NULL,
    aggregate_result_json jsonb NOT NULL,
    published_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_vote_result" PRIMARY KEY (id)
);

CREATE TABLE wellness_badge (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    wellness_visit_id uuid NOT NULL,
    valid_from timestamp with time zone NOT NULL,
    valid_through timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_wellness_badge" PRIMARY KEY (id)
);

CREATE TABLE wellness_escrow_event (
    id uuid NOT NULL,
    wellness_visit_id uuid NOT NULL,
    status character varying(128) NOT NULL,
    reason character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_wellness_escrow_event" PRIMARY KEY (id)
);

CREATE TABLE wellness_report (
    id uuid NOT NULL,
    wellness_visit_id uuid NOT NULL,
    submitted_by_officer_id uuid NOT NULL,
    notes character varying(512) NOT NULL,
    photo_storage_object_ids_json jsonb NOT NULL,
    submitted_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_wellness_report" PRIMARY KEY (id)
);

CREATE TABLE wellness_visit (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    host_user_id uuid NOT NULL,
    officer_id uuid NOT NULL,
    visit_type character varying(128) NOT NULL,
    scheduled_at timestamp with time zone NOT NULL,
    officer_rate numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    status character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_wellness_visit" PRIMARY KEY (id)
);

CREATE TABLE wellness_visit_type_definition (
    id uuid NOT NULL,
    visit_type character varying(128) NOT NULL,
    name character varying(512) NOT NULL,
    minimum_duration_minutes integer NOT NULL,
    description character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_wellness_visit_type_definition" PRIMARY KEY (id)
);

INSERT INTO association_storage_plan (id, annual_price, created_at, created_by_user_id, includes_zoom_archive, is_deleted, key, monthly_price, retention_years, storage_megabytes, updated_at, updated_by_user_id)
VALUES ('24293f94-bd5d-fdda-48e6-ee01066adb9e', 190.0, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, FALSE, 'starter', 19.0, 7, 1024, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO association_storage_plan (id, annual_price, created_at, created_by_user_id, includes_zoom_archive, is_deleted, key, monthly_price, retention_years, storage_megabytes, updated_at, updated_by_user_id)
VALUES ('38f246db-8a87-e966-8dd0-6c016530e455', 390.0, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, TRUE, FALSE, 'pro', 39.0, 7, 10240, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO association_storage_plan (id, annual_price, created_at, created_by_user_id, includes_zoom_archive, is_deleted, key, monthly_price, retention_years, storage_megabytes, updated_at, updated_by_user_id)
VALUES ('9b1368a3-e082-b90d-c0fc-69bcad95a9b4', 0.0, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, FALSE, 'free', 0.0, 2, 100, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO association_storage_plan (id, annual_price, created_at, created_by_user_id, includes_zoom_archive, is_deleted, key, monthly_price, retention_years, storage_megabytes, updated_at, updated_by_user_id)
VALUES ('efbf72b2-6da0-c2ec-eb8e-a22b7c6f39f6', 790.0, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, TRUE, FALSE, 'elite', 79.0, 99, 51200, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);

INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('0715904d-bc5f-6bfc-875c-4fdde8f6fdb7', 'Officer', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'officer-verified', 'Verified', '["Officer onboarding"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('1e7d3363-6686-83bd-b8d5-07acba2674b1', 'LocalBusiness', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'business-verified', 'Verified', '["Mild search boost"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('349b1695-2f9d-d460-ff6c-2bffb3af8f35', 'Host', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'host-verified', 'Verified', '["Verified badge","Custodian directory","Local business directory","Guest verification upsell"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('6b747f5f-f571-4846-24fa-1e31902964ab', 'Host', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'host-trusted', 'Trusted', '["Trades directory","Search boost","Referral program"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('a7465928-a25f-ea05-0dca-d5da270ed2cb', 'Host', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'host-free', 'Free', '["Listings","Calendar","Messaging","QR","Stripe","InsuraGuest","97% payout"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('b76cbb37-5c92-9613-47dd-6cc804ba7267', 'Host', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'host-wellness', 'Wellness', '["Police directory","Wellness visits","Wellness badge","Security verified filter"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('cd24f84a-13d4-e02b-440c-c84489b07e73', 'LocalBusiness', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'business-trusted', 'Trusted', '["Guest promotion","Strong search boost"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO badge_definition (id, applies_to, created_at, created_by_user_id, is_deleted, key, level, unlocks_json, updated_at, updated_by_user_id)
VALUES ('fc5c517f-8218-4e35-0b18-abf74154ab71', 'Officer', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'officer-trusted', 'Trusted', '["Wellness jobs"]', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);

INSERT INTO document_retention_rule (id, created_at, created_by_user_id, document_type, is_deleted, retention_years, updated_at, updated_by_user_id)
VALUES ('13c1fd21-1906-4380-c615-bf08b96562fe', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'FinancialStatement', FALSE, 7, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO document_retention_rule (id, created_at, created_by_user_id, document_type, is_deleted, retention_years, updated_at, updated_by_user_id)
VALUES ('a1353af7-dd11-423b-3dad-e17fb1489d2c', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'MeetingMinutes', FALSE, 7, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO document_retention_rule (id, created_at, created_by_user_id, document_type, is_deleted, retention_years, updated_at, updated_by_user_id)
VALUES ('c9ada982-7ce5-1617-bd31-bcbd98409870', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'WellnessReport', FALSE, 7, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO document_retention_rule (id, created_at, created_by_user_id, document_type, is_deleted, retention_years, updated_at, updated_by_user_id)
VALUES ('ec3ef392-e024-2322-328e-9fe322ff7ab7', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Proxy', FALSE, 7, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO document_retention_rule (id, created_at, created_by_user_id, document_type, is_deleted, retention_years, updated_at, updated_by_user_id)
VALUES ('efe884d4-68d2-39ef-c55c-515cbb568040', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'VoteResult', FALSE, 7, TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);

INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('0cce0a0c-73a8-85dc-17bb-5b4b8ab355f9', NULL, NULL, 10.0, 'Guests', 'Per booking', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PERCENT', TRUE, FALSE, 'guest-fee-mid', 'guest fee mid', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('25f4ac0e-180a-846a-15b7-ee792142a5a1', NULL, NULL, 4.99, 'Guests', 'Per return check', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'guest-ekyc-return-html', 'guest ekyc return html', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('2f620474-2fbc-9eba-c975-6c5046c05ff0', NULL, NULL, 36.0, 'Founding properties', 'Per booking lifetime', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'founding-gold-guest-flat', 'founding gold guest flat', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('2f8b0c3b-15fc-abb8-a761-b18689a33ba3', NULL, NULL, 39.0, 'Communities', 'Monthly', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'association-pro-monthly', 'association pro monthly', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('4e2ec970-92a2-0943-f9d0-b275378e1450', NULL, NULL, 0.14, 'NestyStay', 'Per check', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'alibaba-ekyc-vendor-cost', 'alibaba ekyc vendor cost', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('57cb2eb3-9be9-4d37-0894-be1af411084b', NULL, NULL, 45.0, 'Founding properties', 'Per booking lifetime', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'founding-silver-guest-flat', 'founding silver guest flat', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('59d4c1d5-9e48-a929-56d5-96eb67d1feb0', NULL, NULL, 8.0, 'Guests', 'Per booking', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PERCENT', TRUE, FALSE, 'guest-fee-large-long', 'guest fee large long', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('752e4c8c-8df6-ea48-b821-7fe4013c06dd', NULL, NULL, 15.0, 'Officers', 'Per completed visit', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PERCENT', TRUE, FALSE, 'officer-commission-max', 'officer commission max', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('77403b3e-22fb-0071-774c-5245e7ed81bc', NULL, NULL, 79.0, 'Communities', 'Monthly', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'association-elite-monthly', 'association elite monthly', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('79f39598-2230-0ad2-404a-9e042cf10bd8', NULL, NULL, 0.0, 'Hosts', 'Always', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'host-listing', 'host listing', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('818cd436-df71-3b5e-6095-601ee9a6370d', NULL, NULL, 60.0, 'Hosts', 'Annual', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'verified-host-standard-annual', 'verified host standard annual', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('8c5b02ff-6832-3fe3-d13d-434e74947d1c', NULL, NULL, 12.0, 'Guests', 'Per booking', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PERCENT', TRUE, FALSE, 'guest-fee-single-night', 'guest fee single night', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('a633594e-c72f-9093-a9c4-a224ff6b5d1e', NULL, NULL, 120.0, 'Hosts', 'Annual', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'trusted-host-standard-annual', 'trusted host standard annual', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('b4683aef-20d8-6a33-1314-d34d29804f05', NULL, NULL, 3.0, 'Hosts', 'Per booking', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PERCENT', TRUE, FALSE, 'host-commission-standard', 'host commission standard', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('b6004a45-788c-93df-7d9a-722154892464', NULL, NULL, 8.0, 'Officers', 'Per completed visit', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PERCENT', TRUE, FALSE, 'officer-commission-min', 'officer commission min', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('b912c215-89d5-bf99-7c3e-9b70ef74a3f2', NULL, NULL, 49.0, 'Hosts', 'One time', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'trusted-host-pdf-campaign', 'trusted host pdf campaign', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('c6b18389-7c15-02bd-5cde-54f666637dc5', NULL, NULL, 19.0, 'Communities', 'Monthly', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'association-starter-monthly', 'association starter monthly', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('c75a21fe-6537-cc02-cba3-de5df326e76b', NULL, NULL, 0.14, 'Hosts', 'Per booking', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'guest-ekyc-host-paid-pdf', 'guest ekyc host paid pdf', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('de55e4c6-37b8-667d-d0b8-95a96c956b5e', NULL, NULL, 9.99, 'Guests', 'Per first check', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'guest-ekyc-first-html', 'guest ekyc first html', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('e8b2ccf7-7ec5-127d-90f9-e4bb577e6411', NULL, NULL, 19.0, 'Hosts', 'Monthly', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'wellness-subscription-pdf', 'wellness subscription pdf', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO pricebook_entry (id, active_from, active_to, amount, applies_to, cadence, created_at, created_by_user_id, currency_or_unit, is_configurable, is_deleted, key, label, updated_at, updated_by_user_id)
VALUES ('ebb971a9-27b7-f4a4-5c5c-fc5b1ebbe5db', NULL, NULL, 29.0, 'Founding properties', 'Per booking lifetime', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'USD', TRUE, FALSE, 'founding-platinum-guest-flat', 'founding platinum guest flat', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);

INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('08a0ac26-bebe-1c3a-b739-9f2e4a6d9b95', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/payment/stripe', FALSE, TRUE, 'Payment', 'Stripe', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('0f15f02b-2f3c-354c-2e03-56dcc6286bb1', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/payment/paypal', FALSE, FALSE, 'Payment', 'PayPal', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('1920aff1-5ed3-47ac-2633-20c219a5a27e', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/ekyc/jumio', FALSE, FALSE, 'Ekyc', 'Jumio', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('2051c60e-60c1-29f7-0be7-c9f83a1797a2', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/storage/cloudflarer2', FALSE, TRUE, 'Storage', 'CloudflareR2', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('2c68348d-30f6-b959-8a2e-fa8be3a99530', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/ekyc/onfido', FALSE, FALSE, 'Ekyc', 'Onfido', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('52c17d79-74b3-c176-6bb8-bc6887d3fadb', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/notification/awssestwiliofirebase', FALSE, TRUE, 'Notification', 'AwsSesTwilioFirebase', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('5f54f9cc-4b24-42ef-a9d3-84e07bc981a8', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/storage/amazons3', FALSE, FALSE, 'Storage', 'AmazonS3', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('ac8c27e3-b174-0a22-adb6-c5ae94230a26', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/storage/digitaloceanspaces', FALSE, FALSE, 'Storage', 'DigitalOceanSpaces', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('bb92e267-1ae5-7f5f-5a68-1a769647db7f', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/ekyc/alibabacloud', FALSE, TRUE, 'Ekyc', 'AlibabaCloud', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO provider_config (id, created_at, created_by_user_id, encrypted_config_reference, is_deleted, is_primary, kind, provider_name, updated_at, updated_by_user_id)
VALUES ('f7a4cbc0-5911-cb8f-539e-a096519ab5f2', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'vault://nestystay/insurance/insuraguest', FALSE, TRUE, 'Insurance', 'InsuraGuest', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);

INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('22fe6a08-f54a-f368-bd0c-641ad968214b', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'Admin', 'Admin', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('3f7c71a7-03f3-94c7-5325-fe2b2b1ce1fc', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'PropertyManager', 'PropertyManager', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('5ebe470a-573f-c5f9-603b-97603236bb45', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'Tenant', 'Tenant', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('71d84886-f1bb-f748-1f19-2d165d625dcc', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'AssociationExecutive', 'AssociationExecutive', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('809ada79-f34e-f805-e531-8f243f54de33', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'GateGuard', 'GateGuard', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('84022d57-c4a7-62a1-952f-f34e4d645eea', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'Host', 'Host', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('8db3d94f-bd83-8df0-ad06-0ceecbeb4193', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'ServiceProvider', 'ServiceProvider', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('bb08e7ba-7541-70a1-e41b-cb1a7fb91b42', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'Owner', 'Owner', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('d4038f52-7591-5120-828f-71e1f731a18e', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'Officer', 'Officer', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('f2235913-248a-5540-1203-d87ff50e5596', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'LocalBusiness', 'LocalBusiness', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);
INSERT INTO role (id, created_at, created_by_user_id, is_deleted, key, name, updated_at, updated_by_user_id)
VALUES ('fa51a8c2-1877-ea4c-22d2-916394d4ee97', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, FALSE, 'Guest', 'Guest', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL);

INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('07057f51-6816-a0eb-25db-e3e860fa0e29', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Officer enters and meets guest. Verifies safety. Photo report submitted.', FALSE, 30, 'In-Person With Guest', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'InPersonWithGuest');
INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('409ed29a-2490-f0f1-e251-97fa354330dd', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Officer covers property and personal security as agreed.', FALSE, 240, 'Half Day Security', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'HalfDaySecurity');
INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('6a393b98-ffb7-396d-7f20-8df096bc1557', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Officer accompanies owner or guest around property and surroundings.', FALSE, 120, 'Property Escort', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PropertyEscort');
INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('8e04b5f1-bf1a-c275-4eeb-31e85ecc2791', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Officer inspects property while guest is away. Photo report submitted.', FALSE, 30, 'In-Person Without Guest', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'InPersonWithoutGuest');
INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('c39c7ba0-2d81-34eb-ed22-09b2b12c2e42', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Officer stationed at property from dusk to dawn.', FALSE, 720, 'Overnight Security', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'OvernightSecurity');
INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('c8730050-e1a6-45f0-671e-ae8288ae9733', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Officer accompanies owner or guest anywhere.', FALSE, 240, 'Personal Escort', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'PersonalEscort');
INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('dc4b7906-ba0c-3c79-cc27-e44407921856', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Officer drives past at agreed intervals. No entry. Photos submitted.', FALSE, 0, 'Drive-By Patrol', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'DriveByPatrol');
INSERT INTO wellness_visit_type_definition (id, created_at, created_by_user_id, description, is_deleted, minimum_duration_minutes, name, updated_at, updated_by_user_id, visit_type)
VALUES ('e00300d4-d646-0605-dc0a-209028f39f9b', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'Full property and personal security coverage.', FALSE, 480, 'Full Day Security', TIMESTAMPTZ '2026-05-01T00:00:00+00:00', NULL, 'FullDaySecurity');

CREATE INDEX "IX_booking_property_id_check_in_check_out" ON booking (property_id, check_in, check_out);

CREATE UNIQUE INDEX "IX_officer_current_nesty_stay_id" ON officer (current_nesty_stay_id);

CREATE UNIQUE INDEX "IX_officer_id_history_officer_id_year" ON officer_id_history (officer_id, year);

CREATE UNIQUE INDEX "IX_pricebook_entry_key" ON pricebook_entry (key);

CREATE INDEX "IX_property_availability_property_id_starts_on_ends_on" ON property_availability (property_id, starts_on, ends_on);

CREATE UNIQUE INDEX "IX_provider_config_kind_provider_name" ON provider_config (kind, provider_name);

CREATE UNIQUE INDEX "IX_qr_access_code_code_hash" ON qr_access_code (code_hash);

CREATE UNIQUE INDEX "IX_role_key" ON role (key);

CREATE UNIQUE INDEX "IX_user_email" ON "user" (email);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260501214102_InitialBackendSchema', '10.0.1');

COMMIT;

START TRANSACTION;
CREATE TABLE milestone_badge_assignment (
    id uuid NOT NULL,
    badge_definition_id uuid NOT NULL,
    badge_key character varying(512) NOT NULL,
    level character varying(128) NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    status character varying(128) NOT NULL,
    earned_at timestamp with time zone NOT NULL,
    paid_through timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    amount_charged numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    payment_status character varying(128) NOT NULL,
    payment_reference character varying(512) NOT NULL,
    unlocks_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_badge_assignment" PRIMARY KEY (id)
);

CREATE TABLE milestone_badge_definition (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    level character varying(128) NOT NULL,
    applies_to character varying(512) NOT NULL,
    pricebook_key character varying(512) NOT NULL,
    unlocks_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_badge_definition" PRIMARY KEY (id)
);

CREATE TABLE milestone_badge_renewal (
    id uuid NOT NULL,
    badge_assignment_id uuid NOT NULL,
    reminder_due_at timestamp with time zone NOT NULL,
    payment_attempted_at timestamp with time zone,
    payment_status character varying(128) NOT NULL,
    amount_due numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_badge_renewal" PRIMARY KEY (id)
);

CREATE TABLE milestone_booking (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    host_user_id uuid NOT NULL,
    host_name character varying(512) NOT NULL,
    host_email character varying(512) NOT NULL,
    guest_user_id uuid NOT NULL,
    guest_email character varying(512) NOT NULL,
    guest_name character varying(512) NOT NULL,
    check_in date NOT NULL,
    check_out date NOT NULL,
    status character varying(128) NOT NULL,
    verification_status character varying(128) NOT NULL,
    payment_status character varying(128) NOT NULL,
    requires_guest_verification boolean NOT NULL,
    hold_expires_at timestamp with time zone,
    nights integer NOT NULL,
    nightly_rate numeric(18,2) NOT NULL,
    stay_subtotal numeric(18,2) NOT NULL,
    guest_platform_fee numeric(18,2) NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    property_title character varying(512),
    ekyc_provider character varying(512),
    ekyc_transaction_id character varying(512),
    ekyc_transaction_url character varying(512),
    payment_provider character varying(512),
    payment_authorization_reference character varying(512),
    payment_client_secret character varying(512),
    payment_capture_reference character varying(512),
    price_breakdown_json jsonb NOT NULL,
    notifications_json jsonb NOT NULL,
    timeline_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_booking" PRIMARY KEY (id)
);

CREATE TABLE milestone_campaign (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    name character varying(512) NOT NULL,
    campaign_type character varying(512) NOT NULL,
    override_amount numeric(18,2),
    applies_to character varying(512),
    opens_at timestamp with time zone,
    closes_at timestamp with time zone,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_campaign" PRIMARY KEY (id)
);

CREATE TABLE milestone_campaign_enrollment (
    id uuid NOT NULL,
    campaign_key character varying(512) NOT NULL,
    subject_type character varying(512) NOT NULL,
    subject_id uuid NOT NULL,
    enrolled_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_campaign_enrollment" PRIMARY KEY (id)
);

CREATE TABLE milestone_founding_benefit (
    id uuid NOT NULL,
    property_id uuid NOT NULL,
    tier character varying(128) NOT NULL,
    guest_flat_fee numeric(18,2) NOT NULL,
    host_commission_percent numeric(18,2) NOT NULL,
    is_lifetime_guest_fee boolean NOT NULL,
    is_transferable_with_property boolean NOT NULL,
    is_forfeited boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_founding_benefit" PRIMARY KEY (id)
);

CREATE TABLE milestone_pricebook_entry (
    id uuid NOT NULL,
    key character varying(512) NOT NULL,
    label character varying(512) NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    cadence character varying(512) NOT NULL,
    applies_to character varying(512) NOT NULL,
    is_configurable boolean NOT NULL,
    is_active boolean NOT NULL,
    active_from timestamp with time zone,
    active_to timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_pricebook_entry" PRIMARY KEY (id)
);

CREATE TABLE milestone_property (
    id uuid NOT NULL,
    host_user_id uuid NOT NULL,
    host_name character varying(512) NOT NULL,
    host_email character varying(512) NOT NULL,
    title character varying(512) NOT NULL,
    location character varying(512) NOT NULL,
    country character varying(512) NOT NULL,
    nightly_rate numeric(18,2) NOT NULL,
    currency character varying(512) NOT NULL,
    badge_level character varying(128) NOT NULL,
    guest_verification_enabled boolean NOT NULL,
    insura_guest_enabled boolean NOT NULL,
    cancellation_policy character varying(512) NOT NULL,
    highlights_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_property" PRIMARY KEY (id)
);

CREATE TABLE milestone_two_factor_challenge (
    id uuid NOT NULL,
    challenge_id character varying(512) NOT NULL,
    user_id uuid NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_two_factor_challenge" PRIMARY KEY (id)
);

CREATE TABLE milestone_user (
    id uuid NOT NULL,
    email character varying(512) NOT NULL,
    normalized_email character varying(512) NOT NULL,
    password_hash character varying(512) NOT NULL,
    display_name character varying(512) NOT NULL,
    phone character varying(512),
    two_factor_secret bytea NOT NULL,
    roles_json jsonb NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    created_by_user_id uuid,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL,
    CONSTRAINT "PK_milestone_user" PRIMARY KEY (id)
);

CREATE INDEX "IX_milestone_badge_assignment_subject_type_subject_id_level" ON milestone_badge_assignment (subject_type, subject_id, level);

CREATE UNIQUE INDEX "IX_milestone_badge_definition_level_applies_to" ON milestone_badge_definition (level, applies_to);

CREATE INDEX "IX_milestone_badge_renewal_badge_assignment_id_reminder_due_at" ON milestone_badge_renewal (badge_assignment_id, reminder_due_at);

CREATE INDEX "IX_milestone_booking_ekyc_transaction_id" ON milestone_booking (ekyc_transaction_id);

CREATE INDEX "IX_milestone_booking_property_id_check_in_check_out" ON milestone_booking (property_id, check_in, check_out);

CREATE UNIQUE INDEX "IX_milestone_campaign_key" ON milestone_campaign (key);

CREATE UNIQUE INDEX "IX_milestone_campaign_enrollment_campaign_key_subject_type_sub~" ON milestone_campaign_enrollment (campaign_key, subject_type, subject_id);

CREATE UNIQUE INDEX "IX_milestone_founding_benefit_property_id" ON milestone_founding_benefit (property_id);

CREATE UNIQUE INDEX "IX_milestone_pricebook_entry_key" ON milestone_pricebook_entry (key);

CREATE INDEX "IX_milestone_property_host_user_id" ON milestone_property (host_user_id);

CREATE UNIQUE INDEX "IX_milestone_two_factor_challenge_challenge_id" ON milestone_two_factor_challenge (challenge_id);

CREATE UNIQUE INDEX "IX_milestone_user_normalized_email" ON milestone_user (normalized_email);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260621163343_MilestonePersistentStores', '10.0.1');

COMMIT;

