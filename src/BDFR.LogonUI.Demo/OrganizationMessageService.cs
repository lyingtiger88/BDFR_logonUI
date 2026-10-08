using System.Globalization;
using System.IO;
using System.Text;

namespace BDFR.LogonUI.Demo;

public enum OrganizationMessagePriority
{
    Normal,
    Important,
    Critical
}

public sealed record OrganizationMessage(
    bool Enabled,
    string Organization,
    string Title,
    string Message,
    string Footer,
    OrganizationMessagePriority Priority,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string SourcePath)
{
    public bool IsActive(DateTimeOffset now)
    {
        if (!Enabled)
            return false;

        if (ValidFrom is not null && now < ValidFrom)
            return false;

        if (ValidUntil is not null && now > ValidUntil)
            return false;

        return true;
    }
}

public sealed class OrganizationMessageService
{
    private const string FileName = "organization-message.txt";
    private const string MessageMarker = "---MESSAGE---";

    private readonly string _portablePath;
    private readonly string _managedPath;

    private string? _activePath;
    private DateTime _lastWriteUtc = DateTime.MinValue;
    private long _lastLength = -1;

    public OrganizationMessageService()
    {
        _portablePath = Path.Combine(AppContext.BaseDirectory, FileName);

        _managedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BDFR",
            "LogonUI",
            FileName);
    }

    public string PortablePath => _portablePath;
    public string ManagedPath => _managedPath;
    public string ActivePath => ResolveActivePath();

    public bool HasChanged()
    {
        var path = ResolveActivePath();

        if (!string.Equals(path, _activePath, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!File.Exists(path))
            return _lastWriteUtc != DateTime.MinValue || _lastLength != -1;

        var info = new FileInfo(path);
        return info.LastWriteTimeUtc != _lastWriteUtc || info.Length != _lastLength;
    }

    public OrganizationMessage Load()
    {
        var path = ResolveActivePath();
        _activePath = path;

        if (!File.Exists(path))
        {
            _lastWriteUtc = DateTime.MinValue;
            _lastLength = -1;

            return new OrganizationMessage(
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                OrganizationMessagePriority.Normal,
                null,
                null,
                path);
        }

        var info = new FileInfo(path);
        _lastWriteUtc = info.LastWriteTimeUtc;
        _lastLength = info.Length;

        var lines = File.ReadAllLines(path, Encoding.UTF8);

        var organization = "BDFR";
        var title = "اطلاعیه سازمانی";
        var footer = string.Empty;
        var enabled = true;
        var priority = OrganizationMessagePriority.Normal;
        DateTimeOffset? validFrom = null;
        DateTimeOffset? validUntil = null;

        var messageStart = Array.FindIndex(
            lines,
            line => string.Equals(line.Trim(), MessageMarker, StringComparison.OrdinalIgnoreCase));

        var headerLines = messageStart >= 0
            ? lines.Take(messageStart)
            : lines.AsEnumerable();

        foreach (var rawLine in headerLines)
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            switch (key.ToLowerInvariant())
            {
                case "enabled":
                    if (bool.TryParse(value, out var parsedEnabled))
                        enabled = parsedEnabled;
                    break;

                case "organization":
                    organization = value;
                    break;

                case "title":
                    title = value;
                    break;

                case "footer":
                    footer = value;
                    break;

                case "priority":
                    if (Enum.TryParse<OrganizationMessagePriority>(
                            value,
                            ignoreCase: true,
                            out var parsedPriority))
                    {
                        priority = parsedPriority;
                    }
                    break;

                case "validfrom":
                    validFrom = ParseDate(value);
                    break;

                case "validuntil":
                    validUntil = ParseDate(value);
                    break;
            }
        }

        var messageLines = messageStart >= 0
            ? lines.Skip(messageStart + 1)
            : Array.Empty<string>();

        var message = string.Join(
            Environment.NewLine,
            messageLines).Trim();

        return new OrganizationMessage(
            enabled,
            organization,
            title,
            message,
            footer,
            priority,
            validFrom,
            validUntil,
            path);
    }

    private string ResolveActivePath()
    {
        // A managed machine-wide message wins when IT/admin deploys one.
        if (File.Exists(_managedPath))
            return _managedPath;

        // Portable/demo builds read the editable sidecar file next to the executable.
        return _portablePath;
    }

    private static DateTimeOffset? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var parsed)
            ? parsed
            : null;
    }
}
