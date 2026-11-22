namespace Server.Config;

/// <summary>Usuarios que se siembran al arrancar (sección <c>Seed</c>).</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public SeedAdmin Admin { get; set; } = new();

    public SeedDemo Demo { get; set; } = new();
}

public sealed class SeedAdmin
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = "Administrador";
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Cuenta de la demo pública, con un rol limitado (nunca admin). Sus credenciales se
/// publican en el login, así que cada arranque la restaura y no puede cambiar su contraseña.
/// Sin <c>Email</c> y <c>Password</c> no existe.
/// </summary>
public sealed class SeedDemo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = "Usuario Demo";
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Operaciones;

    public bool Configurada =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    public bool Es(string? email) =>
        Configurada && string.Equals(email?.Trim(), Email.Trim(), StringComparison.OrdinalIgnoreCase);
}
