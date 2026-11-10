using DoctorRegistry.Domain.Practitioners;
using DoctorRegistry.Application.Imports;
using DoctorRegistry.Application.Practitioners;
using DoctorRegistry.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace DoctorRegistry.IntegrationTests;

public sealed class PostgresPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Migration_seeds_and_soft_delete_hides_practitioner()
    {
        var options = new DbContextOptionsBuilder<DoctorRegistryDbContext>().UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new DoctorRegistryDbContext(options);
        await db.Database.MigrateAsync();
        Assert.True(await db.Practitioners.AnyAsync(x => x.Id == Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var practitioner = Practitioner.Create("Grace", "Hopper", "+375292222222", "Neurologist", "North Clinic",
            "grace@example.test", true, DateTimeOffset.UtcNow);
        db.Practitioners.Add(practitioner);
        await db.SaveChangesAsync();
        practitioner.Delete(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        Assert.False(await db.Practitioners.AnyAsync(x => x.Id == practitioner.Id));
    }

    [Fact]
    public async Task Import_persists_practitioner_and_processed_message_atomically()
    {
        await using var db = await CreateContext();
        var handler = new ImportPractitionerHandler(db, new PractitionerMutationService(),
            new CreatePractitionerCommandValidator());
        var message = Message();

        Assert.Equal(PractitionerImportResult.Processed, await handler.Handle(message, default));
        Assert.True(await db.Practitioners.AnyAsync(x => x.Id == message.PractitionerId));
        Assert.True(await db.ProcessedMessages.AnyAsync(x => x.MessageId == message.MessageId));
    }

    [Fact]
    public async Task Duplicate_message_is_a_no_op()
    {
        await using var db = await CreateContext();
        var handler = new ImportPractitionerHandler(db, new PractitionerMutationService(),
            new CreatePractitionerCommandValidator());
        var message = Message();

        await handler.Handle(message, CancellationToken.None);
        Assert.Equal(PractitionerImportResult.Duplicate,
            await handler.Handle(message with { FirstName = "Changed" }, default));
        Assert.Equal("Ada", (await db.Practitioners.SingleAsync(x => x.Id == message.PractitionerId)).FirstName);
    }

    [Fact]
    public async Task Atomic_registration_returns_one_winner_for_concurrent_attempts()
    {
        var id = Guid.NewGuid();
        await using var first = await CreateContext();
        await using var second = await CreateContext();

        var results = await Task.WhenAll(
            first.TryRegisterProcessedMessageAsync(id, DateTimeOffset.UtcNow),
            second.TryRegisterProcessedMessageAsync(id, DateTimeOffset.UtcNow));

        Assert.Equal(1, results.Sum());
    }

    [Fact]
    public async Task Failed_import_rolls_back_processed_message_and_practitioner()
    {
        var message = Message() with { FirstName = "PersistenceFailure" };
        await using (var setup = await CreateContext())
        {
            await setup.Database.ExecuteSqlRawAsync(
                "ALTER TABLE practitioner.practitioners ADD CONSTRAINT practitioners_fail_import CHECK (\"FirstName\" <> 'PersistenceFailure')");
        }

        try
        {
            await using var db = await CreateContext();
            var handler = new ImportPractitionerHandler(db, new PractitionerMutationService(),
                new CreatePractitionerCommandValidator());
            await Assert.ThrowsAsync<DbUpdateException>(() => handler.Handle(message, default));

            await using var verification = await CreateContext();
            Assert.False(await verification.ProcessedMessages.AnyAsync(x => x.MessageId == message.MessageId));
            Assert.False(await verification.Practitioners.AnyAsync(x => x.Id == message.PractitionerId));
        }
        finally
        {
            await using var cleanup = await CreateContext();
            await cleanup.Database.ExecuteSqlRawAsync(
                "ALTER TABLE practitioner.practitioners DROP CONSTRAINT IF EXISTS practitioners_fail_import");
        }
    }

    [Fact]
    public async Task Disposing_uncommitted_transaction_rolls_back_processed_message_and_is_idempotent()
    {
        var messageId = Guid.NewGuid();
        await using (var db = await CreateContext())
        {
            var transaction = await db.BeginTransactionAsync();
            Assert.Equal(1, await db.TryRegisterProcessedMessageAsync(messageId, DateTimeOffset.UtcNow));
            await transaction.DisposeAsync();
            await transaction.DisposeAsync();
        }

        await using var verification = await CreateContext();
        Assert.False(await verification.ProcessedMessages.AnyAsync(x => x.MessageId == messageId));
    }

    [Fact]
    public async Task Invalid_or_cancelled_import_does_not_register_message()
    {
        await using var db = await CreateContext();
        var handler = new ImportPractitionerHandler(db, new PractitionerMutationService(),
            new CreatePractitionerCommandValidator());
        var invalid = Message() with { Email = "not-an-email" };
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => handler.Handle(invalid, default));
        Assert.False(await db.ProcessedMessages.AnyAsync(x => x.MessageId == invalid.MessageId));

        var cancelled = Message();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(cancelled, cts.Token));
        db.ChangeTracker.Clear();
        Assert.False(await db.ProcessedMessages.AnyAsync(x => x.MessageId == cancelled.MessageId));
    }

    [Fact]
    public async Task Search_filters_by_name_family_and_given()
    {
        await using var db = await CreateContext();
        db.AddPractitioner(Practitioner.Create(Guid.NewGuid(), "Ada", "Lovelace", "+375291234567", "Therapist",
            "Clinic", "ada-search@example.test", true, DateTimeOffset.UtcNow));
        db.AddPractitioner(Practitioner.Create(Guid.NewGuid(), "Grace", "Hopper", "+375291234568", "Engineer",
            "Clinic", "grace-search@example.test", true, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var byName = await db.SearchPractitionersAsync(new SearchPractitionersQuery(null, null, null, null, null,
            "love", Page: 1, PageSize: 20));
        var byFamily = await db.SearchPractitionersAsync(new SearchPractitionersQuery(null, null, null, null, null,
            null, "hop", Page: 1, PageSize: 20));
        var byGiven = await db.SearchPractitionersAsync(new SearchPractitionersQuery(null, null, null, null, null,
            null, null, "ada", Page: 1, PageSize: 20));

        Assert.Single(byName.Items);
        Assert.Equal("Lovelace", byName.Items[0].LastName);
        Assert.Single(byFamily.Items);
        Assert.Equal("Hopper", byFamily.Items[0].LastName);
        Assert.Single(byGiven.Items);
        Assert.Equal("Ada", byGiven.Items[0].FirstName);
    }

    private async Task<DoctorRegistryDbContext> CreateContext()
    {
        var options = new DbContextOptionsBuilder<DoctorRegistryDbContext>().UseNpgsql(_postgres.GetConnectionString())
            .Options;
        var context = new DoctorRegistryDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    private static PractitionerImportMessageV1 Message() => new(Guid.NewGuid(),
        PractitionerImportMessageV1.CurrentSchemaVersion, Guid.NewGuid(), "Ada", "Lovelace", "+375291234567",
        "Therapist", "Clinic", "ada@example.test", true);
}
