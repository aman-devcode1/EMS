using EMS.Core.Enums;
using EMS.Core.Interfaces.IServices;
using Microsoft.Extensions.Caching.Memory;

namespace EMS.Infrastructure.ExternalServices;

public class EmailStatusStore : IEmailStatusStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    private readonly IMemoryCache _cache;
    public EmailStatusStore(IMemoryCache cache) => _cache = cache;

    private static string Key(string email) => $"otp-mail:{email.Trim().ToLowerInvariant()}";

    public void MarkQueued(string email) =>
        _cache.Set(Key(email), new EmailStatusSnapshot(EmailDeliveryStatus.Queued, DateTime.UtcNow), Ttl);

    public void Update(string email, EmailDeliveryStatus status)
    {
        var key = Key(email);
        if (!_cache.TryGetValue(key, out EmailStatusSnapshot? current) || current is null) return;

        // Late 'Sent' kisi aage badh chuke status ko peeche na kare (webhook pehle aa sakta hai)
        if (status == EmailDeliveryStatus.Sent &&
            current.Status is EmailDeliveryStatus.Delivered or EmailDeliveryStatus.Deferred or EmailDeliveryStatus.Failed)
            return;

        _cache.Set(key, current with { Status = status }, Ttl);
    }

    public EmailStatusSnapshot Get(string email) =>
        _cache.TryGetValue(Key(email), out EmailStatusSnapshot? s) && s is not null
            ? s
            : new EmailStatusSnapshot(EmailDeliveryStatus.Unknown, DateTime.UtcNow);
}