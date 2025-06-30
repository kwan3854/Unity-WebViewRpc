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
            public string RequestId { get; set; }
        }
        
        private readonly Dictionary<string, ChunkSet> _chunkSets = new();
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
                
                // Check if we've reached the maximum number of chunk sets
                if (!_chunkSets.ContainsKey(chunkInfo.ChunkSetId) && 
                    _chunkSets.Count >= WebViewRpcConfiguration.MaxConcurrentChunkSets)
                {
                    // Remove the oldest chunk set
                    var oldestKey = _chunkSets
                        .OrderBy(kvp => kvp.Value.LastActivity)
                        .First()
                        .Key;
                    _chunkSets.Remove(oldestKey);
                    Debug.LogWarning($"Maximum chunk sets reached ({WebViewRpcConfiguration.MaxConcurrentChunkSets}). Removed oldest chunk set {oldestKey}");
                }
                
                if (!_chunkSets.TryGetValue(chunkInfo.ChunkSetId, out var chunkSet))
                {
                    chunkSet = new ChunkSet
                    {
                        TotalChunks = chunkInfo.TotalChunks,
                        OriginalSize = chunkInfo.OriginalSize,
                        LastActivity = DateTime.UtcNow,
                        RequestId = envelope.RequestId
                    };
                    _chunkSets[chunkInfo.ChunkSetId] = chunkSet;
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
                            Debug.LogError($"Missing chunk {i} in set {chunkInfo.ChunkSetId}");
                            return null;
                        }
                        
                        Array.Copy(chunk, 0, result, offset, chunk.Length);
                        offset += chunk.Length;
                    }
                    
                    // Clean up
                    _chunkSets.Remove(chunkInfo.ChunkSetId);
                    
                    // Verify size
                    if (offset != chunkSet.OriginalSize)
                    {
                        Debug.LogError($"Assembled size mismatch: expected {chunkSet.OriginalSize}, got {offset}");
                        return null;
                    }
                    
                    return result;
                }
                
                // Cleanup old chunk sets (older than configured timeout)
                var cutoff = DateTime.UtcNow.AddSeconds(-WebViewRpcConfiguration.ChunkTimeoutSeconds);
                var toRemove = _chunkSets
                    .Where(kvp => kvp.Value.LastActivity < cutoff)
                    .ToList();
                
                foreach (var kvp in toRemove)
                {
                    timedOutRequestIds.Add(kvp.Value.RequestId);
                    _chunkSets.Remove(kvp.Key);
                    Debug.LogWarning($"Removed incomplete chunk set {kvp.Key} for request {kvp.Value.RequestId} due to timeout");
                }
                
                return null; // Not all chunks received yet
            }
        }
    }
} 
} 