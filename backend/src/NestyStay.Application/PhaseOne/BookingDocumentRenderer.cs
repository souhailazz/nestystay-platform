using System.Globalization;
using System.Text;

namespace NestyStay.Application.PhaseOne;

public static class BookingDocumentRenderer
{
    private const string PdfContentType = "application/pdf";

    public static BookingDocumentDto RenderInvoice(BookingDto booking, DateTimeOffset generatedAt) =>
        Render("Invoice", booking, generatedAt, requireCapturedPayment: false);

    public static BookingDocumentDto RenderReceipt(BookingDto booking, DateTimeOffset generatedAt) =>
        Render("Receipt", booking, generatedAt, requireCapturedPayment: true);

    private static BookingDocumentDto Render(
        string documentType,
        BookingDto booking,
        DateTimeOffset generatedAt,
        bool requireCapturedPayment)
    {
        if (requireCapturedPayment && !booking.PaymentStatus.Equals("CAPTURED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Receipt is available after payment capture.");
        }

        var title = $"NestyStay {documentType}";
        var lines = new List<string>
        {
            title,
            $"Generated: {generatedAt.UtcDateTime:yyyy-MM-dd HH:mm:ss} UTC",
            $"Booking ID: {booking.Id:N}",
            $"Property: {ValueOrDash(booking.PropertyTitle)}",
            $"Host: {ValueOrDash(booking.HostName)}",
            $"Stay: {booking.CheckIn:yyyy-MM-dd} to {booking.CheckOut:yyyy-MM-dd} ({booking.Nights} nights)",
            $"Booking status: {booking.Status}",
            $"Verification status: {booking.VerificationStatus}",
            $"Payment status: {booking.PaymentStatus}",
            string.Empty,
            "Charges"
        };

        lines.AddRange(booking.PriceBreakdown.Select(line =>
            $"{line.Description}: {Money(line.Amount, line.Currency)}"));

        lines.Add($"Stay subtotal: {Money(booking.StaySubtotal, booking.Currency)}");
        lines.Add($"Guest platform fee: {Money(booking.GuestPlatformFee, booking.Currency)}");
        lines.Add($"Total: {Money(booking.TotalAmount, booking.Currency)}");
        lines.Add(string.Empty);
        lines.Add($"Payment provider: {ValueOrDash(booking.PaymentProvider)}");
        lines.Add($"Authorization reference: {ValueOrDash(booking.PaymentAuthorizationReference)}");
        lines.Add($"Capture reference: {ValueOrDash(booking.PaymentCaptureReference)}");
        lines.Add($"Refund reference: {ValueOrDash(booking.PaymentRefundReference)}");
        lines.Add($"Refunded amount: {Money(booking.RefundedAmount, booking.Currency)}");
        lines.Add($"Refund reason: {ValueOrDash(booking.RefundReason)}");
        lines.Add(string.Empty);
        lines.Add(documentType.Equals("Receipt", StringComparison.Ordinal)
            ? "This receipt confirms captured payment for the booking above."
            : "This invoice summarizes the booking charges and current payment state.");

        return new BookingDocumentDto(
            $"nestystay-{documentType.ToLowerInvariant()}-{booking.Id:N}.pdf",
            PdfContentType,
            BuildPdf(lines),
            generatedAt);
    }

    private static string Money(decimal amount, string currency) =>
        $"{currency.ToUpperInvariant()} {amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static byte[] BuildPdf(IReadOnlyList<string> lines)
    {
        var streamBuilder = new StringBuilder();
        streamBuilder.Append("BT\n/F1 11 Tf\n15 TL\n54 760 Td\n");
        for (var index = 0; index < lines.Count; index++)
        {
            streamBuilder.Append('(').Append(EscapePdfText(lines[index])).Append(") Tj\n");
            if (index < lines.Count - 1)
            {
                streamBuilder.Append("T*\n");
            }
        }

        streamBuilder.Append("ET\n");
        var stream = streamBuilder.ToString();
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
            $"5 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream\nendobj\n"
        };

        var pdf = new StringBuilder();
        var offsets = new List<int> { 0 };
        AppendAscii(pdf, "%PDF-1.4\n");
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            AppendAscii(pdf, obj);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n");
        pdf.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static void AppendAscii(StringBuilder builder, string value) =>
        builder.Append(value);

    private static string EscapePdfText(string value)
    {
        var escaped = new StringBuilder(value.Length);
        foreach (var character in value.ReplaceLineEndings(" "))
        {
            switch (character)
            {
                case '\\':
                case '(':
                case ')':
                    escaped.Append('\\').Append(character);
                    break;
                default:
                    escaped.Append(character is >= ' ' and <= '~' ? character : '?');
                    break;
            }
        }

        return escaped.ToString();
    }
}
