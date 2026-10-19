using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Services;
using Libreria.Domain;
using Libreria.Infrastructure;
using Libreria.Infrastructure.Configuration;
using Libreria.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace Libreria.Application.Tests;

// Opt-in: crea y elimina exclusivamente bases US71_Test_<guid>, nunca LibreriaDb.
public sealed class SqlUs71FactAttribute : FactAttribute
{
    public SqlUs71FactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LIBRERIA_US71_SQL_TEST_CONNECTION")))
            Skip = "Integración SQL: definir LIBRERIA_US71_SQL_TEST_CONNECTION para usar una BD temporal aislada.";
    }
}

public class UsuarioSeguridadSqlTests
{
    [SqlUs71Fact]
    public async Task CrearUsuario_Development_ConservaPendienteGeneraCorreoLocalYPermiteBaja()
    {
        await using var fixture = await SqlFixture.CrearAsync();
        var url = fixture.IniciarReplica(desarrollo: true);
        await EsperarAsync(url);
        using var admin = Cliente();
        await LoginAsync(admin, url, "admin", "Admin2026!");
        var rol = await fixture.EscalarAsync("SELECT RolId FROM dbo.Rol WHERE Nombre=N'Vendedor'");
        var html = await admin.Http.GetStringAsync(url + "/Usuarios/Create");
        var datos = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Csrf(html),
            ["Nombre"] = "Prueba", ["Apellido"] = "Usuario",
            ["Correo"] = "pruebaus71@example.com", ["RolId"] = rol.ToString()
        };
        using (var generar = await admin.Http.PostAsync(url + "/Usuarios/Create?handler=Generar", new FormUrlEncodedContent(datos)))
        {
            html = await generar.Content.ReadAsStringAsync();
            Assert.Contains("pusuario", html);
        }
        datos["__RequestVerificationToken"] = Csrf(html);
        using (var crear = await admin.Http.PostAsync(url + "/Usuarios/Create?handler=Crear", new FormUrlEncodedContent(datos)))
        {
            html = WebUtility.HtmlDecode(await crear.Content.ReadAsStringAsync());
            Assert.Contains("Usuario pusuario creado correctamente.", html);
            Assert.Contains("no se envió por Internet", html);
        }
        Assert.Equal(1, await fixture.EscalarAsync(@"
            SELECT COUNT(*) FROM dbo.Usuario WHERE NombreUsuario=N'pusuario' AND Estado=N'Pendiente'
            AND PasswordHash IS NULL AND TokenActivacionHash IS NOT NULL AND TokenActivacionExpiracion IS NOT NULL"));
        Assert.Single(Directory.GetFiles(fixture.CarpetaCorreo, "*.eml"));

        // Reintentar el mismo correo no duplica usuarios ni genera otra invitación.
        datos["__RequestVerificationToken"] = Csrf(html);
        using (var duplicado = await admin.Http.PostAsync(url + "/Usuarios/Create?handler=Crear", new FormUrlEncodedContent(datos)))
            Assert.Contains("Ya existe un usuario registrado con ese correo.",
                WebUtility.HtmlDecode(await duplicado.Content.ReadAsStringAsync()));
        Assert.Equal(3, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario"));
        Assert.Single(Directory.GetFiles(fixture.CarpetaCorreo, "*.eml"));

        html = await admin.Http.GetStringAsync(url + "/Usuarios");
        var creado = await fixture.UsuarioAsync("pusuario");
        using (var baja = await BajaAsync(admin, url, html, IdProtegido(html, creado.PublicId)))
            Assert.Equal(HttpStatusCode.Redirect, baja.StatusCode);
        Assert.Equal(1, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario WHERE NombreUsuario=N'pusuario' AND Estado=N'Inactivo'"));
        Assert.Equal(3, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario"));
    }

    [SqlUs71Fact]
    public async Task CrearUsuario_SinConfiguracionDeCorreo_RevierteConCausaBaseUrl()
    {
        await using var fixture = await SqlFixture.CrearAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var repository = new UsuarioRepository(fixture);
        var actor = await fixture.UsuarioAsync("admin");
        var servicio = new UsuarioService(repository, new GeneradorNombreUsuario(repository),
            new SmtpEmailSender(config, new CorreoTestEnvironment()), new ConfiguracionAplicacion(config), fixture,
            new UsuarioActualFake(actor.Id));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearPendienteAsync(
            new Libreria.Application.Models.UsuarioAltaInput
            {
                Nombre = "Prueba", Apellido = "Usuario", Correo = "pruebaus71@example.com", RolId = 2
            }));

        Assert.Equal("Falta la configuración BASE_URL.", error.InnerException!.Message);
        Assert.Equal(2, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario"));
    }

    [SqlUs71Fact]
    public async Task BajasConcurrentesEnSQL_ConservanUnAdministradorActivo()
    {
        await using var fixture = await SqlFixture.CrearAsync();
        await fixture.SqlAsync(@"
            INSERT dbo.Usuario (Nombre, Apellido, Correo, NombreUsuario, NombreCompleto, PasswordHash, RolId, Estado)
            SELECT N'Segundo', N'Administrador', N'segundo@example.test', N'segundo',
                   N'Segundo Administrador', PasswordHash, RolId, N'Activo'
            FROM dbo.Usuario WHERE NombreUsuario=N'admin';");
        var a = await fixture.UsuarioAsync("admin");
        var b = await fixture.UsuarioAsync("segundo");
        var repoA = new UsuarioRepository(fixture);
        var repoB = new UsuarioRepository(fixture);
        var servicioA = new UsuarioSeguridadService(repoA, fixture, new UsuarioActualFake(a.Id));
        var servicioB = new UsuarioSeguridadService(repoB, fixture, new UsuarioActualFake(b.Id));

        var resultados = await Task.WhenAll(servicioA.DarDeBajaAsync(b.PublicId), servicioB.DarDeBajaAsync(a.PublicId));

        Assert.Single(resultados, r => r.Exitoso);
        Assert.Equal(1, await fixture.EscalarAsync(@"
            SELECT COUNT(*) FROM dbo.Usuario u JOIN dbo.Rol r ON r.RolId=u.RolId
            WHERE u.Estado=N'Activo' AND r.Nombre=N'Administrador' AND r.Estado=1"));
        Assert.Equal(3, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario"));
    }

    [SqlUs71Fact]
    public async Task DosReplicas_ModalBajaLogicaLoginGenericoYRevocacionDeLaMismaCookie()
    {
        await using var fixture = await SqlFixture.CrearAsync();
        var urlA = fixture.IniciarReplica();
        var urlB = fixture.IniciarReplica();
        await EsperarAsync(urlA);
        await EsperarAsync(urlB);
        using var admin = Cliente();
        using var vendedor = Cliente();
        await LoginAsync(admin, urlB, "admin", "Admin2026!");
        await LoginAsync(vendedor, urlA, "vendedor", "Vendedor2026!");
        var cookies = vendedor.Handler.Cookies.GetCookieHeader(new Uri(urlA));
        using (var accesoPrevio = await PeticionConCookieAsync(urlB, cookies))
            Assert.Equal(HttpStatusCode.OK, accesoPrevio.StatusCode);

        var vendedorDatos = await fixture.UsuarioAsync("vendedor");
        var adminDatos = await fixture.UsuarioAsync("admin");
        var html = await admin.Http.GetStringAsync(urlB + "/Usuarios");
        Assert.Contains("id=\"usuario-baja-modal\"", html);
        Assert.Contains("data-confirm-trigger", html);
        var protegido = IdProtegido(html, vendedorDatos.PublicId);
        Assert.NotEqual(vendedorDatos.PublicId.ToString(), protegido);
        Assert.Equal(2, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario"));

        // Sin antifalsificación no debe haber modificación.
        using (var sinCsrf = await admin.Http.PostAsync(urlB + "/Usuarios?handler=DarDeBaja",
                   new FormUrlEncodedContent(new Dictionary<string, string> { ["publicId"] = protegido })))
            Assert.Equal(HttpStatusCode.BadRequest, sinCsrf.StatusCode);

        // Último administrador y baja propia: el servidor conserva su acceso.
        using (var propia = await BajaAsync(admin, urlB, html, IdProtegido(html, adminDatos.PublicId)))
            Assert.Equal(HttpStatusCode.Redirect, propia.StatusCode);
        Assert.Equal(1, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario WHERE NombreUsuario=N'admin' AND Estado=N'Activo'"));

        html = await admin.Http.GetStringAsync(urlB + "/Usuarios");
        using (var baja = await BajaAsync(admin, urlB, html, IdProtegido(html, vendedorDatos.PublicId)))
            Assert.Equal(HttpStatusCode.Redirect, baja.StatusCode);
        Assert.Equal(2, await fixture.EscalarAsync("SELECT COUNT(*) FROM dbo.Usuario"));
        Assert.Equal(1, await fixture.EscalarAsync(@"
            SELECT COUNT(*) FROM dbo.Usuario u JOIN dbo.Usuario a ON a.UsuarioId=u.UsuarioModificacionId
            WHERE u.NombreUsuario=N'vendedor' AND u.Estado=N'Inactivo'
              AND a.NombreUsuario=N'admin' AND u.FechaModificacion IS NOT NULL"));

        // Reutilizar exactamente la cookie anterior en dos procesos independientes.
        foreach (var url in new[] { urlA, urlB })
        {
            using var respuesta = await PeticionConCookieAsync(url, cookies);
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Contains("/Cuenta/Login", respuesta.Headers.Location!.OriginalString);
            Assert.Contains(respuesta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("Libreria.Sesion=;"));
        }

        foreach (var estado in new[] { EstadosUsuario.Inactivo, EstadosUsuario.Pendiente })
        {
            await fixture.SqlAsync("UPDATE dbo.Usuario SET Estado=@Estado WHERE NombreUsuario=N'vendedor'", estado);
            using var intento = Cliente();
            var login = await intento.Http.GetStringAsync(urlA + "/Cuenta/Login");
            using var respuesta = await intento.Http.PostAsync(urlA + "/Cuenta/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = Csrf(login),
                    ["Input.NombreUsuario"] = "vendedor", ["Input.Contrasena"] = "Vendedor2026!"
                }));
            var contenido = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Contains("Usuario o contraseña incorrectos.", contenido);
            Assert.DoesNotContain("Inactivo", contenido);
            Assert.DoesNotContain("Pendiente", contenido);
        }
    }

    private static string Csrf(string html) => WebUtility.HtmlDecode(Regex.Match(html,
        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);

    private static string IdProtegido(string html, Guid id)
    {
        var fila = Regex.Matches(html, "<tr>.*?</tr>", RegexOptions.Singleline)
            .Cast<Match>().Single(m => m.Value.Contains(id.ToString())).Value;
        return WebUtility.HtmlDecode(Regex.Match(fila, "data-entity-id=\"([^\"]+)\"").Groups[1].Value);
    }

    private static Task<HttpResponseMessage> BajaAsync(ClienteHttp cliente, string url, string html, string id) =>
        cliente.Http.PostAsync(url + "/Usuarios?handler=DarDeBaja", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = Csrf(html), ["publicId"] = id }));

    private static async Task LoginAsync(ClienteHttp cliente, string url, string usuario, string password)
    {
        var html = await cliente.Http.GetStringAsync(url + "/Cuenta/Login");
        using var respuesta = await cliente.Http.PostAsync(url + "/Cuenta/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = Csrf(html),
                ["Input.NombreUsuario"] = usuario, ["Input.Contrasena"] = password
            }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
    }

    private static async Task<HttpResponseMessage> PeticionConCookieAsync(string url, string cookies)
    {
        using var cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        using var peticion = new HttpRequestMessage(HttpMethod.Get, url + "/Ventas");
        peticion.Headers.Add("Cookie", cookies);
        return await cliente.SendAsync(peticion);
    }

    private static async Task EsperarAsync(string url)
    {
        using var cliente = new HttpClient();
        for (var i = 0; i < 100; i++)
        {
            try
            {
                using var respuesta = await cliente.GetAsync(url + "/Cuenta/Login");
                if (respuesta.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(100);
        }
        throw new TimeoutException("La réplica de pruebas no inició.");
    }

    private sealed class CookiesHandler : HttpClientHandler
    {
        public CookieContainer Cookies { get; } = new();
        public CookiesHandler() { CookieContainer = Cookies; AllowAutoRedirect = false; }
    }
    private sealed class ClienteHttp : IDisposable
    {
        public CookiesHandler Handler { get; } = new();
        public HttpClient Http { get; }
        public ClienteHttp() { Http = new HttpClient(Handler); }
        public void Dispose() => Http.Dispose();
    }
    private static ClienteHttp Cliente() => new();

    private sealed class SqlFixture : IAsyncDisposable, IDbConnectionFactory
    {
        private readonly string _nombre = "US71_Test_" + Guid.NewGuid().ToString("N");
        private readonly string _master;
        private readonly List<Process> _procesos = [];
        private readonly string _raiz;
        public string CarpetaCorreo => Path.Combine(_raiz, "tests", "Libreria.Application.Tests", "bin", _nombre);
        private string ConnectionString => new SqlConnectionStringBuilder(_master) { InitialCatalog = _nombre }.ConnectionString;

        private SqlFixture()
        {
            _master = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("LIBRERIA_US71_SQL_TEST_CONNECTION"))
                { InitialCatalog = "master" }.ConnectionString;
            var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
            while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "LibreriaSystem.slnx")))
                carpeta = carpeta.Parent;
            _raiz = carpeta?.FullName ?? throw new InvalidOperationException("No se encuentra la raíz del proyecto.");
        }

        public static async Task<SqlFixture> CrearAsync()
        {
            var fixture = new SqlFixture();
            try
            {
                await using var master = new SqlConnection(fixture._master);
                await master.OpenAsync();
                foreach (var file in Directory.GetFiles(Path.Combine(fixture._raiz, "db", "init"), "*.sql").OrderBy(f => f))
                {
                    var script = (await File.ReadAllTextAsync(file)).Replace("LibreriaDb", fixture._nombre);
                    foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(batch)) continue;
                        await using var command = new SqlCommand(batch, master) { CommandTimeout = 60 };
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }

        public DbConnection CreateConnection() => new SqlConnection(ConnectionString);
        public async Task SqlAsync(string sql, string? estado = null)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            if (estado is not null) command.Parameters.AddWithValue("@Estado", estado);
            await command.ExecuteNonQueryAsync();
        }
        public async Task<int> EscalarAsync(string sql)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        public async Task<(int Id, Guid PublicId)> UsuarioAsync(string nombre)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("SELECT UsuarioId, PublicId FROM dbo.Usuario WHERE NombreUsuario=@Nombre", connection);
            command.Parameters.AddWithValue("@Nombre", nombre);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            return (reader.GetInt32(0), reader.GetGuid(1));
        }
        public string IniciarReplica(bool desarrollo = false)
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            var url = "http://127.0.0.1:" + port;
            var configuracion = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var dll = Path.Combine(_raiz, "src", "Libreria.Client", "bin", configuracion, "net10.0", "Libreria.Client.dll");
            var inicio = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.Combine(_raiz, "src", "Libreria.Client"),
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            inicio.ArgumentList.Add(dll);
            inicio.ArgumentList.Add("--urls");
            inicio.ArgumentList.Add(url);
            inicio.Environment["DB_CONNECTION_STRING"] = ConnectionString;
            inicio.Environment["ASPNETCORE_ENVIRONMENT"] = desarrollo ? "Development" : "Production";
            if (desarrollo)
            {
                inicio.Environment["BASE_URL"] = url;
                inicio.Environment["EMAIL_PICKUP_DIRECTORY"] = CarpetaCorreo;
            }
            var proceso = Process.Start(inicio)!;
            proceso.OutputDataReceived += (_, _) => { };
            proceso.ErrorDataReceived += (_, _) => { };
            proceso.BeginOutputReadLine();
            proceso.BeginErrorReadLine();
            _procesos.Add(proceso);
            return url;
        }
        public async ValueTask DisposeAsync()
        {
            foreach (var proceso in _procesos)
            {
                if (!proceso.HasExited) { proceso.Kill(entireProcessTree: true); await proceso.WaitForExitAsync(); }
                proceso.Dispose();
            }
            SqlConnection.ClearAllPools();
            if (!Regex.IsMatch(_nombre, "^US71_Test_[a-f0-9]{32}$")) throw new InvalidOperationException("Nombre temporal inválido.");
            await using var connection = new SqlConnection(_master);
            await connection.OpenAsync();
            await using var command = new SqlCommand($"IF DB_ID(N'{_nombre}') IS NOT NULL BEGIN ALTER DATABASE [{_nombre}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_nombre}]; END", connection);
            await command.ExecuteNonQueryAsync();
            var raizCorreo = Path.GetFullPath(Path.Combine(_raiz, "tests", "Libreria.Application.Tests", "bin"));
            var carpetaCorreo = Path.GetFullPath(CarpetaCorreo);
            if (Directory.Exists(carpetaCorreo) && Path.GetDirectoryName(carpetaCorreo) == raizCorreo
                && Path.GetFileName(carpetaCorreo) == _nombre)
                Directory.Delete(carpetaCorreo, recursive: true);
        }
    }
}
