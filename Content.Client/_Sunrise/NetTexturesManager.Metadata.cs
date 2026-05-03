using System.Collections.Generic;
using System.IO;
using System.Text;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    #region Animation Timing
    private static readonly float[] OneFrameDelay = new float[] { 1f };

    /// <summary>
    /// Normalizes per-direction RSI delays so each direction has at least one frame delay row.
    /// </summary>
    /// <param name="delays">The raw delay table read from metadata, if present.</param>
    /// <param name="dirCount">The expected number of RSI directions.</param>
    /// <returns>A normalized per-direction delay table.</returns>
    private static float[][] NormalizeDelays(float[][]? delays, int dirCount)
    {
        if (delays == null)
        {
            var result = new float[dirCount][];
            for (var i = 0; i < dirCount; i++)
            {
                result[i] = OneFrameDelay;
            }

            return result;
        }

        if (delays.Length != dirCount)
            throw new InvalidDataException($"Direction count {dirCount} does not match delay rows {delays.Length}");

        var normalized = new float[dirCount][];
        for (var i = 0; i < dirCount; i++)
        {
            normalized[i] = delays[i].Length == 0 ? OneFrameDelay : delays[i];
        }

        return normalized;
    }

    /// <summary>
    /// Folds multi-direction RSI delays into a shared frame timeline plus direction-specific frame indices.
    /// </summary>
    /// <param name="delays">The per-direction delay table.</param>
    /// <returns>The folded delay track and the frame index table for each direction.</returns>
    private static (float[] Delays, int[][] Indices) FoldDelays(float[][] delays)
    {
        if (delays.Length == 1)
        {
            var delayList = delays[0];
            var output = new float[delayList.Length];
            var singleIndices = new int[delayList.Length];

            for (var i = 0; i < delayList.Length; i++)
            {
                output[i] = delayList[i];
                singleIndices[i] = i;
            }

            return (output, new[] { singleIndices });
        }

        const float fixedPointResolution = 1000;

        var dirCount = delays.Length;
        var iDelays = new int[dirCount][];
        var dirLengths = new int[dirCount];
        var maxLength = 0;

        for (var d = 0; d < dirCount; d++)
        {
            var length = 0;
            var delayList = new int[delays[d].Length];
            iDelays[d] = delayList;

            for (var i = 0; i < delayList.Length; i++)
            {
                var delay = (int) (delays[d][i] * fixedPointResolution);
                delayList[i] = delay;
                length += delay;
            }

            maxLength = Math.Max(length, maxLength);
            dirLengths[d] = length;
        }

        for (var d = 0; d < dirCount; d++)
        {
            var diff = maxLength - dirLengths[d];
            iDelays[d][^1] += diff;
        }

        var dirIndexOffsets = new int[dirCount];
        for (var i = 0; i < dirCount - 1; i++)
        {
            dirIndexOffsets[i + 1] = dirIndexOffsets[i] + delays[i].Length;
        }

        var dirDelayOffsets = new int[dirCount];

        var newDelays = new List<int>();
        var newIndices = new List<int>[dirCount];
        for (var d = 0; d < dirCount; d++)
        {
            newIndices[d] = new List<int>();
        }

        var finished = false;
        while (!finished)
        {
            var minDelay = int.MaxValue;

            for (var d = 0; d < dirCount; d++)
            {
                var offset = dirDelayOffsets[d];
                var delay = iDelays[d][offset];
                minDelay = Math.Min(delay, minDelay);
                newIndices[d].Add(dirIndexOffsets[d] + offset);
            }

            newDelays.Add(minDelay);

            for (var d = 0; d < dirCount; d++)
            {
                var offset = dirDelayOffsets[d];
                var delay = iDelays[d][offset] - minDelay;
                iDelays[d][offset] = delay;

                if (delay == 0)
                {
                    offset += 1;
                    dirDelayOffsets[d] = offset;
                }

                if (offset == iDelays[d].Length)
                {
                    finished = true;
                    break;
                }
            }
        }

        var floatDelays = new float[newDelays.Count];
        for (var i = 0; i < newDelays.Count; i++)
        {
            floatDelays[i] = newDelays[i] / fixedPointResolution;
        }

        var indices = new int[dirCount][];
        for (var d = 0; d < dirCount; d++)
        {
            indices[d] = newIndices[d].ToArray();
        }

        return (floatDelays, indices);
    }

    /// <summary>
    /// Creates a consumer-facing animation state from a fully uploaded RSI state payload.
    /// </summary>
    /// <param name="state">The uploaded RSI state payload.</param>
    /// <returns>The animation state exposed to consumers.</returns>
    private static NetTextureAnimationState CreateAnimationState(PreparedRsiState state)
    {
        var frames = new Texture[state.FoldedIndices.Length][];

        for (var dir = 0; dir < state.FoldedIndices.Length; dir++)
        {
            var indices = state.FoldedIndices[dir];
            var output = new Texture[indices.Length];

            for (var frame = 0; frame < indices.Length; frame++)
            {
                var index = indices[frame];
                output[frame] = state.UploadedFrames[index];
            }

            frames[dir] = output;
        }

        var directionType = state.DirectionCount switch
        {
            1 => RsiDirectionType.Dir1,
            4 => RsiDirectionType.Dir4,
            8 => RsiDirectionType.Dir8,
            _ => throw new InvalidOperationException($"Unsupported RSI direction count {state.DirectionCount}")
        };

        return new NetTextureAnimationState(state.StateId, directionType, state.FoldedDelays, frames);
    }
    #endregion

    #region RSI Metadata
    /// <summary>
    /// Parses the uploaded RSI <c>meta.json</c> file into validated metadata structures.
    /// </summary>
    /// <param name="metaStream">The metadata stream to parse.</param>
    /// <returns>The parsed RSI metadata.</returns>
    private static RsiMetadataData LoadRsiMetadata(Stream metaStream)
    {
        using var reader = new StreamReader(metaStream, Encoding.UTF8, true, 4096, leaveOpen: true);
        var yaml = new YamlStream();
        yaml.Load(reader);

        if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
            throw new InvalidDataException("RSI metadata root must be a mapping");

        if (!root.TryGetNode("size", out YamlMappingNode? sizeNode))
            throw new InvalidDataException("RSI metadata is missing size");

        if (!sizeNode.TryGetNode("x", out var sizeXNode) || !sizeNode.TryGetNode("y", out var sizeYNode))
            throw new InvalidDataException("RSI metadata size is incomplete");

        if (!root.TryGetNode("states", out YamlSequenceNode? statesNode) || statesNode.Children.Count == 0)
            throw new InvalidDataException("RSI metadata is missing states");

        var states = new RsiStateMetadataData[statesNode.Children.Count];
        for (var i = 0; i < statesNode.Children.Count; i++)
        {
            if (statesNode.Children[i] is not YamlMappingNode stateNode)
                throw new InvalidDataException("RSI metadata state must be a mapping");

            if (!stateNode.TryGetNode("name", out var nameNode))
                throw new InvalidDataException("RSI metadata state is missing name");

            int? directions = null;
            if (stateNode.TryGetNode("directions", out var directionsNode))
                directions = directionsNode.AsInt();

            float[][]? delays = null;
            if (stateNode.TryGetNode("delays", out YamlSequenceNode? delayRowsNode))
                delays = ReadRsiDelays(delayRowsNode);

            states[i] = new RsiStateMetadataData(nameNode.AsString(), directions, delays);
        }

        var loadParameters = TextureLoadParameters.Default;
        if (root.TryGetNode("load", out YamlMappingNode? loadNode))
            loadParameters = TextureLoadParameters.FromYaml(loadNode);

        return new RsiMetadataData(new Vector2i(sizeXNode.AsInt(), sizeYNode.AsInt()), states, loadParameters);
    }

    /// <summary>
    /// Parses the optional RSI per-direction delay table.
    /// </summary>
    /// <param name="delayRowsNode">The YAML sequence containing delay rows.</param>
    /// <returns>The parsed delay table.</returns>
    private static float[][] ReadRsiDelays(YamlSequenceNode delayRowsNode)
    {
        var rows = new float[delayRowsNode.Children.Count][];
        for (var rowIndex = 0; rowIndex < delayRowsNode.Children.Count; rowIndex++)
        {
            if (delayRowsNode.Children[rowIndex] is not YamlSequenceNode delayRowNode)
                throw new InvalidDataException("RSI delay rows must be sequences");

            var row = new float[delayRowNode.Children.Count];
            for (var frameIndex = 0; frameIndex < delayRowNode.Children.Count; frameIndex++)
            {
                row[frameIndex] = delayRowNode.Children[frameIndex].AsFloat();
            }

            rows[rowIndex] = row;
        }

        return rows;
    }
    #endregion

    #region Path Helpers
    /// <summary>
    /// Checks whether a resource path points to an RSI directory.
    /// </summary>
    /// <param name="path">The normalized path to inspect.</param>
    /// <returns><see langword="true"/> if the path targets an RSI resource.</returns>
    private static bool IsRsiPath(ResPath path)
    {
        var pathString = path.ToRelativePath().ToString();
        return pathString.EndsWith(".rsi", StringComparison.Ordinal) ||
               pathString.EndsWith(".rsi/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Converts a consumer-facing string path into a normalized rooted resource path.
    /// </summary>
    /// <param name="resourcePath">The rooted or relative resource path string.</param>
    /// <returns>The normalized rooted resource path.</returns>
    private static ResPath ToResPath(string resourcePath)
    {
        var resPath = resourcePath.StartsWith("/", StringComparison.Ordinal)
            ? new ResPath(resourcePath)
            : (new ResPath("/") / resourcePath);

        return resPath.Clean();
    }

    /// <summary>
    /// Resolves the resource key used for ready-state tracking after uploaded files are published.
    /// </summary>
    /// <remarks>
    /// RSI directory contents are tracked by the directory path rather than individual file paths.
    /// </remarks>
    /// <param name="relativePath">The uploaded relative file or directory path.</param>
    /// <returns>The consumer-facing resource path key.</returns>
    private static string GetUploadedResourcePath(ResPath relativePath)
    {
        var rootedPath = (ResPath.Root / relativePath.ToRelativePath()).Clean();
        var parent = rootedPath.Directory.ToString();

        if (parent.EndsWith(".rsi", StringComparison.Ordinal) || parent.EndsWith(".rsi/", StringComparison.Ordinal))
            return rootedPath.Directory.ToString().TrimEnd('/');

        return rootedPath.ToString();
    }
    #endregion
}
