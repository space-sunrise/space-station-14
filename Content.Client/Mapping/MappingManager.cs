using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Mapping;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client.Mapping;

public sealed class MappingManager : IPostInjectInit
{
    [Dependency] private readonly IFileDialogManager _file = default!;
    [Dependency] private readonly IClientNetManager _net = default!;

    private Stream? _saveStream;
    private MappingMapDataMessage? _mapData;

    public void PostInject()
    {
        _net.RegisterNetMessage<MappingSaveMapMessage>();
        _net.RegisterNetMessage<MappingSaveMapErrorMessage>(OnSaveError);
        _net.RegisterNetMessage<MappingMapDataMessage>(OnMapData);
    }

    private void OnSaveError(MappingSaveMapErrorMessage message)
    {
        _saveStream?.DisposeAsync();
        _saveStream = null;
    }

    private async void OnMapData(MappingMapDataMessage message)
    {
        if (_saveStream == null)
        {
            _mapData = message;
            return;
        }

        await _saveStream.WriteAsync(Encoding.ASCII.GetBytes(message.Yml));
        await _saveStream.DisposeAsync();

        _saveStream = null;
        _mapData = null;
    }

    public async Task SaveMap()
    {
        if (_saveStream != null)
            await _saveStream.DisposeAsync();

        // Sunrise added start - only request server-side map save processing after destination is confirmed
        _mapData = null;
        // Sunrise added end

        var path = await _file.SaveFile();
        if (path is not { fileStream: var stream })
            return;

        // Sunrise added start - avoid mutating the map on the server when the save dialog is canceled
        _saveStream = stream;

        var request = new MappingSaveMapMessage();
        _net.ClientSendMessage(request);
        // Sunrise added end

        if (_mapData != null)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(_mapData.Yml));
            _mapData = null;
            await stream.FlushAsync();
            await stream.DisposeAsync();
            return;
        }
    }
}
