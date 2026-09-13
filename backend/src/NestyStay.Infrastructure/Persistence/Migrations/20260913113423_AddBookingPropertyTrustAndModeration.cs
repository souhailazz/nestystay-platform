using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPropertyTrustAndModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "host_verification_document_type",
                table: "milestone_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "host_verification_reason",
                table: "milestone_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "host_verification_reviewed_at",
                table: "milestone_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "host_verification_reviewed_by_user_id",
                table: "milestone_user",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "host_verification_status",
                table: "milestone_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "NotStarted");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "host_verification_submitted_at",
                table: "milestone_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "amenities_json",
                table: "milestone_property",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "bathrooms",
                table: "milestone_property",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "bedrooms",
                table: "milestone_property",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "cleaning_fee",
                table: "milestone_property",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "milestone_property",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "gallery_urls_json",
                table: "milestone_property",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "house_rules_json",
                table: "milestone_property",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "milestone_property",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "milestone_property",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "milestone_property",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_guests",
                table: "milestone_property",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "moderated_at",
                table: "milestone_property",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "moderated_by_user_id",
                table: "milestone_property",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "moderation_reason",
                table: "milestone_property",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "moderation_status",
                table: "milestone_property",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<string>(
                name: "parish",
                table: "milestone_property",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "service_fee",
                table: "milestone_property",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "sleeping_arrangements_json",
                table: "milestone_property",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rejected_at",
                table: "milestone_booking",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rejected_by_user_id",
                table: "milestone_booking",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "milestone_booking",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_source",
                table: "milestone_booking",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            // Older rows pre-date these trust fields. Preserve their existing
            // public behavior while giving every new host/listing an explicit
            // state through the entity defaults above.
            migrationBuilder.Sql("UPDATE milestone_user SET host_verification_status = 'NotStarted' WHERE host_verification_status IS NULL OR btrim(host_verification_status) = '';");
            migrationBuilder.Sql("UPDATE milestone_property SET moderation_status = 'Approved' WHERE moderation_status IS NULL OR btrim(moderation_status) = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "host_verification_document_type",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "host_verification_reason",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "host_verification_reviewed_at",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "host_verification_reviewed_by_user_id",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "host_verification_status",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "host_verification_submitted_at",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "amenities_json",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "bathrooms",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "bedrooms",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "cleaning_fee",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "description",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "gallery_urls_json",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "house_rules_json",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "max_guests",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "moderated_at",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "moderated_by_user_id",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "moderation_reason",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "moderation_status",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "parish",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "service_fee",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "sleeping_arrangements_json",
                table: "milestone_property");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "milestone_booking");

            migrationBuilder.DropColumn(
                name: "rejected_by_user_id",
                table: "milestone_booking");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "milestone_booking");

            migrationBuilder.DropColumn(
                name: "rejection_source",
                table: "milestone_booking");
        }
    }
}
