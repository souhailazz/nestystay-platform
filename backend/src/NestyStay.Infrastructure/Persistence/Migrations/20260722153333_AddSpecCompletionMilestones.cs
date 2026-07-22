using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecCompletionMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_admin_case",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    priority = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    assigned_to = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    resolution_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_admin_case", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_audit_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    action = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_audit_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_auth_flow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    flow_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    destination = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_auth_flow", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_contact_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_contact_request", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_conversation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_support_thread = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_conversation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_conversation_participant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    role = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    last_read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    online_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_conversation_participant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_directory_provider",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    kind = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    category = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    parish = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    badge_level = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    availability_summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    contact_mode = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rating = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_directory_provider", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_experience",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    category = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    parish = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    images_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    included_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    rules_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    availability_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_experience", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_host_pricing_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    nightly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    minimum_stay = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_host_pricing_rule", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_host_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    display_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    parish = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    bio = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    response_time = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    badges_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    listing_ids_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    rating = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    highlights_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_host_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_host_promotion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    minimum_nights = table.Column<int>(type: "integer", nullable: false),
                    badge_level = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_host_promotion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_journal_article",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    category = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    author = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    tags_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    related_slugs_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_journal_article", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attachments_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_public_content_page",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    kind = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sections_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    links_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_public_content_page", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_recovery_code",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_recovery_code", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_review",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    host_reply = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    editable_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_review", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_traveler_notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    deep_link = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_traveler_notification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_traveler_payment_method",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    last4 = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    exp_month = table.Column<int>(type: "integer", nullable: false),
                    exp_year = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_traveler_payment_method", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wishlist_collection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wishlist_collection", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wishlist_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wishlist_item", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_admin_case_case_type_status",
                table: "milestone_admin_case",
                columns: new[] { "case_type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_audit_event_subject_type_subject_id_created_at",
                table: "milestone_audit_event",
                columns: new[] { "subject_type", "subject_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_auth_flow_token",
                table: "milestone_auth_flow",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_auth_flow_user_id_flow_type_status",
                table: "milestone_auth_flow",
                columns: new[] { "user_id", "flow_type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_conversation_participant_conversation_id_user_id",
                table: "milestone_conversation_participant",
                columns: new[] { "conversation_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_provider_kind_category_parish",
                table: "milestone_directory_provider",
                columns: new[] { "kind", "category", "parish" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_provider_slug",
                table: "milestone_directory_provider",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_experience_category_parish",
                table: "milestone_experience",
                columns: new[] { "category", "parish" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_experience_slug",
                table: "milestone_experience",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_host_pricing_rule_host_user_id_property_id_starts~",
                table: "milestone_host_pricing_rule",
                columns: new[] { "host_user_id", "property_id", "starts_on", "ends_on" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_host_profile_host_user_id",
                table: "milestone_host_profile",
                column: "host_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_host_profile_slug",
                table: "milestone_host_profile",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_host_promotion_host_user_id_property_id_is_active",
                table: "milestone_host_promotion",
                columns: new[] { "host_user_id", "property_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_journal_article_slug",
                table: "milestone_journal_article",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_message_conversation_id_sent_at",
                table: "milestone_message",
                columns: new[] { "conversation_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_public_content_page_slug",
                table: "milestone_public_content_page",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_recovery_code_user_id_code",
                table: "milestone_recovery_code",
                columns: new[] { "user_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_review_user_id_property_id_booking_id",
                table: "milestone_review",
                columns: new[] { "user_id", "property_id", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_traveler_notification_user_id_is_read",
                table: "milestone_traveler_notification",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_traveler_payment_method_user_id_is_default",
                table: "milestone_traveler_payment_method",
                columns: new[] { "user_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wishlist_collection_user_id_name",
                table: "milestone_wishlist_collection",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wishlist_item_user_id_property_id_collection_id",
                table: "milestone_wishlist_item",
                columns: new[] { "user_id", "property_id", "collection_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_admin_case");

            migrationBuilder.DropTable(
                name: "milestone_audit_event");

            migrationBuilder.DropTable(
                name: "milestone_auth_flow");

            migrationBuilder.DropTable(
                name: "milestone_contact_request");

            migrationBuilder.DropTable(
                name: "milestone_conversation");

            migrationBuilder.DropTable(
                name: "milestone_conversation_participant");

            migrationBuilder.DropTable(
                name: "milestone_directory_provider");

            migrationBuilder.DropTable(
                name: "milestone_experience");

            migrationBuilder.DropTable(
                name: "milestone_host_pricing_rule");

            migrationBuilder.DropTable(
                name: "milestone_host_profile");

            migrationBuilder.DropTable(
                name: "milestone_host_promotion");

            migrationBuilder.DropTable(
                name: "milestone_journal_article");

            migrationBuilder.DropTable(
                name: "milestone_message");

            migrationBuilder.DropTable(
                name: "milestone_public_content_page");

            migrationBuilder.DropTable(
                name: "milestone_recovery_code");

            migrationBuilder.DropTable(
                name: "milestone_review");

            migrationBuilder.DropTable(
                name: "milestone_traveler_notification");

            migrationBuilder.DropTable(
                name: "milestone_traveler_payment_method");

            migrationBuilder.DropTable(
                name: "milestone_wishlist_collection");

            migrationBuilder.DropTable(
                name: "milestone_wishlist_item");
        }
    }
}
