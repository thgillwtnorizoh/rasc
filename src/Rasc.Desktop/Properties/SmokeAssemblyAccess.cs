using System.Runtime.CompilerServices;

// Only the dedicated real-WPF Windows smoke-test assembly may inspect and
// instantiate internal presentation hosts. This does not expose them as public
// application APIs or change shipping notification behavior.
[assembly: InternalsVisibleTo("Rasc.Desktop.Smoke")]
