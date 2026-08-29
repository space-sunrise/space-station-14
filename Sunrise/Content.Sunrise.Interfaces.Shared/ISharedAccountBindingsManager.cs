using Robust.Shared.Serialization;

namespace Content.Sunrise.Interfaces.Shared;

public interface ISharedAccountBindingsManager
{
    public void Initialize();

    public event Action<AccountBindingsSnapshot>? BindingsChanged;

    public AccountBindingsSnapshot GetSnapshot();

    public void RequestBindingsRefresh();
}

[Serializable, NetSerializable]
public enum AccountBindingState
{
    Linked,
    Unlinked,
    Unavailable,
}

[Serializable, NetSerializable]
public sealed class AccountBindingEntry
{
    public AccountBindingState State { get; set; }
    public string? DisplayValue { get; set; }

    public AccountBindingEntry()
    {
    }

    public AccountBindingEntry(AccountBindingState state, string? displayValue = null)
    {
        State = state;
        DisplayValue = string.IsNullOrWhiteSpace(displayValue) ? null : displayValue.Trim();
    }

    public static AccountBindingEntry Linked(string? displayValue = null)
    {
        return new AccountBindingEntry(AccountBindingState.Linked, displayValue);
    }

    public static AccountBindingEntry Unlinked()
    {
        return new AccountBindingEntry(AccountBindingState.Unlinked);
    }

    public static AccountBindingEntry Unavailable()
    {
        return new AccountBindingEntry(AccountBindingState.Unavailable);
    }
}

[Serializable, NetSerializable]
public sealed class AccountBindingsSnapshot
{
    public AccountBindingEntry Discord { get; set; } = AccountBindingEntry.Unavailable();
    public AccountBindingEntry Telegram { get; set; } = AccountBindingEntry.Unavailable();
    public AccountBindingEntry Github { get; set; } = AccountBindingEntry.Unavailable();

    public AccountBindingsSnapshot()
    {
    }

    public AccountBindingsSnapshot(
        AccountBindingEntry discord,
        AccountBindingEntry telegram,
        AccountBindingEntry github)
    {
        Discord = discord;
        Telegram = telegram;
        Github = github;
    }

    public static AccountBindingsSnapshot Unavailable()
    {
        return new AccountBindingsSnapshot(
            AccountBindingEntry.Unavailable(),
            AccountBindingEntry.Unavailable(),
            AccountBindingEntry.Unavailable());
    }
}
