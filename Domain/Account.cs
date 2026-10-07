using System.Text.Json;

namespace Usage;

public sealed record Account(string Key, string Service, string Label, string CredentialPath);
