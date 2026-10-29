using CharManJur.Models;

namespace CharManJur.Services;

// Turns a campaign's saved password into what the network session needs, without storing anything new.
public static class CampaignSessionCredentials
{
    // null = the campaign has no password. A damaged password throws instead of quietly opening the session to everyone.
    public static SessionCredentials? From(Campaign campaign)
    {
        if (!campaign.HasPassword) return null;

        try
        {
            return new SessionCredentials(
                Convert.FromBase64String(campaign.PasswordSalt ?? string.Empty),
                Convert.FromBase64String(campaign.PasswordHash ?? string.Empty),
                Campaign.PasswordIterations);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new InvalidOperationException("This campaign's password is damaged. Set the password again before opening the session.", ex);
        }
    }
}