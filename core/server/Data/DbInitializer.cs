using Microsoft.EntityFrameworkCore;
using Server.Config;
using Server.Domain;
using Server.Services;

namespace Server.Data;

/// <summary>
/// Crea el esquema la primera vez, siembra el administrador, importa los clientes
/// y los datos de ejemplo. Reintenta mientras SQL Server termina de arrancar.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        SeedOptions seed,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        const int maxAttempts = 20;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    "SQL Server no está listo (intento {Attempt}/{Max}): {Message}",
                    attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        await SeedAdminAsync(db, seed, logger, cancellationToken);
        await RetirarPersonalDeEjemploAsync(db, logger, cancellationToken);
        // Antes de SeedData: en una base nueva la demo también recibe asistencia y ventas de ejemplo.
        await SeedDemoAsync(db, seed.Demo, logger, cancellationToken);

        try
        {
            await SeedClients.SeedAsync(db, logger, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron importar los clientes.");
        }

        try
        {
            await SeedData.SeedAsync(db, cancellationToken);
            logger.LogInformation("Datos de ejemplo verificados/sembrados.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron sembrar los datos de ejemplo.");
        }
    }

    private static async Task SeedAdminAsync(
        AppDbContext db,
        SeedOptions seed,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.Admin.Email) ||
            string.IsNullOrWhiteSpace(seed.Admin.Password))
        {
            logger.LogWarning("No hay 'Seed:Admin' configurado; la tabla Users queda vacía.");
            return;
        }

        db.Users.Add(new User
        {
            Email = seed.Admin.Email.Trim(),
            Name = string.IsNullOrWhiteSpace(seed.Admin.Name) ? "Administrador" : seed.Admin.Name,
            Role = Roles.Admin,
            PasswordHash = PasswordHasher.Hash(seed.Admin.Password),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Usuario administrador sembrado: {Email}", seed.Admin.Email);
    }

    // Versiones anteriores sembraban estos trabajadores con una contraseña publicada en el
    // repositorio, así que cualquiera podía entrar con ellos (gerencia lo ve todo).
    private static readonly string[] PersonalDeEjemplo =
    [
        "ana.quispe@compipro.net", "luis.torres@compipro.net",
        "marta.rios@compipro.net", "diego.flores@compipro.net",
    ];

    private const string ClaveDelPersonalDeEjemplo = "Compipro2025!";

    /// <summary>
    /// Deja solo al admin y la cuenta demo: elimina al personal de ejemplo que aún tiene la
    /// contraseña publicada (si alguien la cambió, la cuenta está en uso y se conserva). Su
    /// asistencia se borra con ellos; sus cotizaciones y tickets quedan sin asignar.
    /// </summary>
    private static async Task RetirarPersonalDeEjemploAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var usuarios = await db.Users
            .Where(u => PersonalDeEjemplo.Contains(u.Email))
            .ToListAsync(cancellationToken);
        if (usuarios.Count == 0) return;

        var retirar = usuarios
            .Where(u => PasswordHasher.Verify(ClaveDelPersonalDeEjemplo, u.PasswordHash))
            .ToList();
        foreach (var u in usuarios.Except(retirar))
            logger.LogWarning("{Email} era un usuario de ejemplo pero cambió su contraseña: se conserva.", u.Email);
        if (retirar.Count == 0) return;

        db.Users.RemoveRange(retirar);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Usuarios de ejemplo eliminados: {Emails}", string.Join(", ", retirar.Select(u => u.Email)));
    }

    /// <summary>
    /// Crea o restaura la cuenta de la demo pública: activa, con su rol limitado y la
    /// contraseña configurada. Sus credenciales se muestran en el login, así que si un
    /// visitante la desactiva o le cambia algo, el siguiente arranque la deja como estaba.
    /// </summary>
    private static async Task SeedDemoAsync(
        AppDbContext db,
        SeedDemo demo,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!demo.Configurada) return;

        if (!Roles.EsValido(demo.Role) || Roles.Es(demo.Role, Roles.Admin))
        {
            logger.LogError(
                "'Seed:Demo:Role' = '{Role}' no es válido para la demo (usa ventas, operaciones o gerencia); no se crea la cuenta demo.",
                demo.Role);
            return;
        }

        string email = demo.Email.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            db.Users.Add(new User
            {
                Email = email,
                Name = demo.Name,
                Role = demo.Role.ToLowerInvariant(),
                PasswordHash = PasswordHasher.Hash(demo.Password),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Cuenta demo creada: {Email} ({Role})", email, demo.Role);
            return;
        }

        // Nunca degradar a un admin real por una configuración equivocada.
        if (Roles.Es(user.Role, Roles.Admin))
        {
            logger.LogError(
                "'Seed:Demo:Email' ({Email}) pertenece a un administrador; no se usa como cuenta demo.", email);
            return;
        }

        // Solo reescribe lo que cambió: rehacer el hash en cada arranque no hace falta.
        if (!PasswordHasher.Verify(demo.Password, user.PasswordHash))
            user.PasswordHash = PasswordHasher.Hash(demo.Password);
        user.Name = demo.Name;
        user.Role = demo.Role.ToLowerInvariant();
        user.Activo = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}
