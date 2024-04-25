using System.Diagnostics;

namespace AlfenHub.Alfen.Logging;

internal static class DiagnosticsConfig
{

    public static ActivitySource ActivitySource = new (nameof(AlfenHub));
}
