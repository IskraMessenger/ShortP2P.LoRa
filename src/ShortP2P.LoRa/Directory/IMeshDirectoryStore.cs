namespace ShortP2P.LoRa.Directory;

/// <summary>
///     Persistence for the NetworkId → MeshId directory (REQ-DIR-2).
///     The ShortP2P host may replace the default file store with SQLite.
/// </summary>
public interface IMeshDirectoryStore
{
    ValueTask<IReadOnlyList<MeshBinding>> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(IReadOnlyList<MeshBinding> bindings, CancellationToken cancellationToken = default);
}
