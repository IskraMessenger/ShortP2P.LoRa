using System.Text.Json;

namespace ShortP2P.LoRa.Directory;

/// <summary>Default directory persistence: a local JSON file (REQ-DIR-2).</summary>
public sealed class JsonFileMeshDirectoryStore : IMeshDirectoryStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonFileMeshDirectoryStore(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>Default location: %LocalAppData%/ShortP2P.LoRa/mesh-directory.json.</summary>
    public static string DefaultPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShortP2P.LoRa", "mesh-directory.json");
    }

    public async ValueTask<IReadOnlyList<MeshBinding>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return [];
            await using var stream = File.OpenRead(_path);
            var dtos = await JsonSerializer.DeserializeAsync<List<BindingDto>>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false) ?? [];
            var result = new List<MeshBinding>(dtos.Count);
            foreach (var d in dtos)
            {
                try
                {
                    var net = Convert.FromHexString(d.N);
                    var mesh = Convert.FromHexString(d.M);
                    if (net.Length != MeshBinding.NetworkIdLength || mesh.Length is < MeshId.MinLength or > MeshId.MaxLength)
                        continue;
                    result.Add(new MeshBinding(net, mesh, (MeshSubject)d.S, d.A, d.C, new DateTime(d.T, DateTimeKind.Utc)));
                }
                catch (FormatException)
                {
                    // skip corrupt row
                }
            }

            return result;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask SaveAsync(IReadOnlyList<MeshBinding> bindings, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var dtos = bindings.Select(b => new BindingDto
            {
                N = b.NetworkIdHex,
                M = b.MeshIdHex,
                S = (byte)b.Subject,
                A = b.IsActive,
                C = b.Confirmed,
                T = b.LastSeenUtc.Ticks
            }).ToList();
            var tmp = _path + ".tmp";
            await using (var stream = File.Create(tmp))
                await JsonSerializer.SerializeAsync(stream, dtos, cancellationToken: cancellationToken).ConfigureAwait(false);
            File.Move(tmp, _path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class BindingDto
    {
        public string N { get; set; } = "";
        public string M { get; set; } = "";
        public byte S { get; set; }
        public bool A { get; set; }
        public bool C { get; set; }
        public long T { get; set; }
    }
}
