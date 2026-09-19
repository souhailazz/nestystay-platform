using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NestyStay.Api.Services;

/// <summary>
/// Keeps user-controlled calendar feed fetches on public web destinations.
/// The connect callback performs the check at the socket boundary as well as
/// before the request, which closes the DNS-rebinding window between validation
/// and HttpClient's connection attempt.
/// </summary>
internal static class CalendarFeedSafety
{
    public const int MaximumCalendarBytes = 5 * 1024 * 1024;

    public static string ValidateUrl(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrWhiteSpace(uri.UserInfo) ||
            uri.Port is not (-1 or 80 or 443))
        {
            throw new InvalidOperationException("Calendar feed must be an HTTP(S) URL on port 80 or 443 without embedded credentials.");
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Private calendar feed addresses are not allowed.");
        }

        if (IPAddress.TryParse(uri.Host, out var address) && IsDisallowedAddress(address))
        {
            throw new InvalidOperationException("Private calendar feed addresses are not allowed.");
        }

        return uri.ToString();
    }

    public static async Task EnsurePublicDestinationAsync(string feedUrl, CancellationToken cancellationToken)
    {
        var uri = new Uri(ValidateUrl(feedUrl), UriKind.Absolute);
        var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(IsDisallowedAddress))
        {
            throw new InvalidOperationException("Calendar feed host resolves to a private or otherwise restricted network address.");
        }
    }

    public static SocketsHttpHandler CreateHttpHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        ConnectCallback = async (context, cancellationToken) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var address = addresses.FirstOrDefault(candidate => !IsDisallowedAddress(candidate))
                ?? throw new HttpRequestException("Calendar feed host resolved to a private or restricted network address.");
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };

    public static async Task<string> ReadTextWithLimitAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > MaximumCalendarBytes)
            {
                throw new InvalidOperationException("Calendar feed is larger than the 5 MB safety limit.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
    }

    public static bool IsDisallowedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) ||
            IPAddress.Any.Equals(address) ||
            IPAddress.IPv6Any.Equals(address) ||
            address.IsIPv6LinkLocal ||
            address.IsIPv6SiteLocal ||
            IsMulticast(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 0 ||
                   bytes[0] == 10 ||
                   (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 198 && bytes[1] is 18 or 19) ||
                   bytes[0] >= 224;
        }

        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
               bytes.Length >= 2 &&
               (bytes[0] & 0xFE) == 0xFC;
    }

    private static bool IsMulticast(IPAddress address) =>
        address.AddressFamily == AddressFamily.InterNetwork
            ? address.GetAddressBytes()[0] >= 224
            : address.AddressFamily == AddressFamily.InterNetworkV6 && address.GetAddressBytes()[0] == 0xFF;
}
