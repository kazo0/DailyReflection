namespace DailyReflection.Core.Entities;

/// <summary>
/// The daily reminder setting, sent when the app turns it off because the OS
/// no longer allows notifications (permission revoked or channel blocked).
/// </summary>
public record NotificationsEnabledSelection(bool Enabled);
