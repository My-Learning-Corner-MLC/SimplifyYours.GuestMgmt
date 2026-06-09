using System;
using GuestManagementService.Domain.Guests;
using GuestManagementService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GuestManagementServiceDbContext))]
partial class GuestManagementServiceDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("GuestManagementService.Domain.EventReferences.EventReference", b =>
        {
            b.Property<Guid>("EventId")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("event_id");

            b.Property<string>("EventName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("event_name");

            b.Property<bool>("IsDeleted")
                .ValueGeneratedOnAdd()
                .HasColumnType("boolean")
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");

            b.Property<DateTimeOffset>("LastSyncedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("last_synced_at");

            b.HasKey("EventId")
                .HasName("pk_event_references");

            b.ToTable("event_references", (string)null);
        });

        modelBuilder.Entity("GuestManagementService.Domain.Guests.Guest", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<string>("EmailAddress")
                .HasMaxLength(254)
                .HasColumnType("character varying(254)")
                .HasColumnName("email_address");

            b.Property<Guid>("EventId")
                .HasColumnType("uuid")
                .HasColumnName("event_id");

            b.Property<string>("FirstName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("first_name");

            b.Property<Gender>("Gender")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("gender");

            b.Property<string>("LastName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("last_name");

            b.Property<string>("NormalizedEmailAddress")
                .HasMaxLength(254)
                .HasColumnType("character varying(254)")
                .HasColumnName("normalized_email_address");

            b.Property<string>("NormalizedPhoneNumber")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("normalized_phone_number");

            b.Property<string>("PhoneNumber")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("character varying(40)")
                .HasColumnName("phone_number");

            b.Property<DateTimeOffset>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("Id")
                .HasName("pk_guests");

            b.HasIndex("EventId")
                .HasDatabaseName("ix_guests_event_id");

            b.HasIndex("EventId", "NormalizedEmailAddress")
                .IsUnique()
                .HasDatabaseName("ux_guests_event_id_normalized_email_address")
                .HasFilter("normalized_email_address IS NOT NULL");

            b.HasIndex("EventId", "NormalizedPhoneNumber")
                .IsUnique()
                .HasDatabaseName("ux_guests_event_id_normalized_phone_number");

            b.ToTable("guests", (string)null);
        });

        modelBuilder.Entity("GuestManagementService.Infrastructure.Persistence.Inbox.InboxMessage", b =>
        {
            b.Property<string>("Error")
                .HasMaxLength(2000)
                .HasColumnType("character varying(2000)")
                .HasColumnName("error");

            b.Property<int>("HandleAttempts")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasDefaultValue(0)
                .HasColumnName("handle_attempts");

            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<string>("EventType")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("event_type");

            b.Property<DateTimeOffset?>("ProcessedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("processed_at");

            b.Property<DateTimeOffset>("ReceivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("received_at");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("status");

            b.HasKey("Id")
                .HasName("pk_inbox_messages");

            b.ToTable("inbox_messages", (string)null);
        });
#pragma warning restore 612, 618
    }
}
