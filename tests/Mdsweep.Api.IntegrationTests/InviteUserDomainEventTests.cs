using Ardalis.Result;
using Mdsweep.Application.Common.Email;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Users.Invitations.Invite;
using Mdsweep.Application.Users.Invitations.Resend;
using Mdsweep.Domain.Users.Events;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;
using Wolverine;
using Wolverine.Tracking;

namespace Mdsweep.Api.IntegrationTests;

public sealed class InviteUserDomainEventTests : MdsweepIntegrationTest
{
    private const string TenantId = "mdsw-eep2-3456";
    private const string InviteeEmail = "invitee@example.test";
    private readonly RecordingEmailSender emailSender = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IEmailSender>();
        services.AddSingleton<IEmailSender>(emailSender);
    }

    [Fact]
    public async Task Inviting_a_user_persists_the_invitation_and_sends_its_email()
    {
        Result? result = null;
        var command = new InviteUserCommand(InviteeEmail, "Synthetic", "Invitee", ["Driver"]);

        var tracked = await Application
            .Services.TrackActivity(TimeSpan.FromSeconds(5))
            .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ =>
            {
                await using var scope = Application.Services.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                result = await bus.InvokeForTenantAsync<Result>(TenantId, command);
            }));

        await using var verificationScope = Application.Services.CreateAsyncScope();
        var invitation = await verificationScope
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Invitations.SingleOrDefaultAsync(candidate => candidate.Email == InviteeEmail);

        var sentEventCount = tracked
            .FindEnvelopesWithMessageType<InvitationCreatedDomainEvent>(MessageEventType.Sent)
            .Count();
        var startedHandlerCount = tracked
            .FindEnvelopesWithMessageType<InvitationCreatedDomainEvent>(MessageEventType.ExecutionStarted)
            .Count();
        var finishedHandlerCount = tracked
            .FindEnvelopesWithMessageType<InvitationCreatedDomainEvent>(MessageEventType.ExecutionFinished)
            .Count();
        var invitationHasExpectedTenant = invitation?.TenantId == TenantId;

        Assert.True(
            result?.IsSuccess == true
                && invitation is not null
                && invitationHasExpectedTenant
                && sentEventCount == 1
                && startedHandlerCount == 1
                && finishedHandlerCount == 1
                && emailSender.CallCount == 1
                && emailSender.LastIsHtml,
            $"""
            InviteUser pipeline boundaries:
              command succeeded: {result?.IsSuccess == true}
              invitation persisted: {invitation is not null}
              invitation TenantId correct: {invitationHasExpectedTenant}
              InvitationCreatedDomainEvent sent once: {sentEventCount == 1}
              event handler started once: {startedHandlerCount == 1}
              event handler finished once: {finishedHandlerCount == 1}
              email sent once: {emailSender.CallCount == 1}
              email sent as HTML: {emailSender.LastIsHtml}
            """
        );
    }

    [Fact]
    public async Task Resending_an_invitation_rotates_its_token_and_sends_a_new_email()
    {
        Guid invitationId;
        string originalTokenHash;
        Instant originalExpiry;

        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var services = setupScope.ServiceProvider;
            var tokenService = services.GetRequiredService<ITokenService>();
            originalExpiry = NodaTime.SystemClock.Instance.GetCurrentInstant() + Duration.FromHours(1);
            var invitation = InvitationAggregate.Create(
                TenantId,
                InviteeEmail,
                "Synthetic",
                "Invitee",
                ["Driver"],
                "original-token",
                tokenService.Hash("original-token"),
                originalExpiry
            );
            invitation.DequeueDomainEvents();
            invitationId = invitation.Id;
            originalTokenHash = invitation.TokenHash;

            var db = services.GetRequiredService<ApplicationDbContext>();
            db.Invitations.Add(invitation);
            await db.SaveChangesAsync();
        }

        Result? result = null;
        var tracked = await Application
            .Services.TrackActivity(TimeSpan.FromSeconds(5))
            .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ =>
            {
                await using var scope = Application.Services.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                result = await bus.InvokeForTenantAsync<Result>(
                    TenantId,
                    new ResendInvitationCommand(invitationId)
                );
            }));

        await using var verificationScope = Application.Services.CreateAsyncScope();
        var resent = await verificationScope
            .ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Invitations.SingleAsync(candidate => candidate.Id == invitationId);

        Assert.True(result?.IsSuccess == true);
        Assert.NotEqual(originalTokenHash, resent.TokenHash);
        Assert.True(resent.ExpiresAt > originalExpiry);
        Assert.Single(
            tracked.FindEnvelopesWithMessageType<InvitationCreatedDomainEvent>(MessageEventType.Sent)
        );
        Assert.Equal(1, emailSender.CallCount);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        private int callCount;

        public int CallCount => Volatile.Read(ref callCount);
        public bool LastIsHtml { get; private set; }

        public Task SendEmailAsync(
            string to,
            string subject,
            string body,
            string? from = null,
            bool isHtml = false,
            CancellationToken ct = default
        )
        {
            LastIsHtml = isHtml;
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        }
    }
}
