using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WebViewRPC
{
    /// <summary>
    /// Manages chunking and reassembly of messages
    /// </summary>
    public class ChunkAssembler
    {
        private class ChunkSet
        {
            public Dictionary<int, byte[]> Chunks { get; } = new();
            public int TotalChunks { get; set; }
            public int OriginalSize { get; set; }
            public DateTime LastActivity { get; set; }
        }

        private readonly Dictionary<string, ChunkSet> _chunkSetsByRequest = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Try to reassemble chunks. Returns null if not all chunks received yet.
        /// </summary>
        public byte[] TryAssemble(RpcEnvelope envelope)
        {
            return TryAssemble(envelope, out _);
        }

        /// <summary>
        /// Try to reassemble chunks. Returns null if not all chunks received yet.
        /// </summary>
        public byte[] TryAssemble(RpcEnvelope envelope, out List<string> timedOutRequestIds)
        {
            timedOutRequestIds = new List<string>();

            if (envelope.ChunkInfo == null)
            {
                // Not a chunked message
                return envelope.Payload.ToByteArray();
            }

            lock (_lock)
            {
                var chunkInfo = envelope.ChunkInfo;
                var requestId = envelope.RequestId;

                // Check if we've reached the maximum number of concurrent requests
                if (!_chunkSetsByRequest.ContainsKey(requestId) &&
                    _chunkSetsByRequest.Count >= WebViewRpcConfiguration.MaxConcurrentChunkSets)
                {
                    // Remove the oldest request
                    var oldestKey = _chunkSetsByRequest
                        .OrderBy(kvp => kvp.Value.LastActivity)
                        .First()
                        .Key;
                    _chunkSetsByRequest.Remove(oldestKey);
                    Debug.LogWarning(
                        $"Maximum concurrent requests reached ({WebViewRpcConfiguration.MaxConcurrentChunkSets}). Removed oldest request {oldestKey}");
                }

                if (!_chunkSetsByRequest.TryGetValue(requestId, out var chunkSet))
                {
                    chunkSet = new ChunkSet
                    {
                        TotalChunks = chunkInfo.TotalChunks,
                        OriginalSize = chunkInfo.OriginalSize,
                        LastActivity = DateTime.UtcNow
                    };
                    _chunkSetsByRequest[requestId] = chunkSet;
                }

                // Add chunk
                chunkSet.Chunks[chunkInfo.ChunkIndex] = envelope.Payload.ToByteArray();
                chunkSet.LastActivity = DateTime.UtcNow;

                // Check if all chunks received
                if (chunkSet.Chunks.Count == chunkSet.TotalChunks)
                {
                    // Assemble
                    var result = new byte[chunkSet.OriginalSize];
                    var offset = 0;

                    for (int i = 1; i <= chunkSet.TotalChunks; i++)
                    {
                        if (!chunkSet.Chunks.TryGetValue(i, out var chunk))
                        {
                            Debug.LogError($"Missing chunk {i} for request {requestId}");
                            return null;
                        }

                        Array.Copy(chunk, 0, result, offset, chunk.Length);
                        offset += chunk.Length;
                    }

                    // Clean up
                    _chunkSetsByRequest.Remove(requestId);

                    // Verify size
                    if (offset != chunkSet.OriginalSize)
                    {
                        Debug.LogError($"Assembled size mismatch: expected {chunkSet.OriginalSize}, got {offset}");
                        return null;
                    }

                    return result;
                }

                // Cleanup old requests (older than configured timeout)
                var cutoff = DateTime.UtcNow.AddSeconds(-WebViewRpcConfiguration.ChunkTimeoutSeconds);
                var toRemove = _chunkSetsByRequest
                    .Where(kvp => kvp.Value.LastActivity < cutoff)
                    .ToList();

                foreach (var kvp in toRemove)
                {
                    timedOutRequestIds.Add(kvp.Key);
                    _chunkSetsByRequest.Remove(kvp.Key);
                    Debug.LogWarning(
                        $"Removed incomplete chunks for request {kvp.Key} due to timeout");
                }

                return null; // Not all chunks received yet
            }
        }
    }
}