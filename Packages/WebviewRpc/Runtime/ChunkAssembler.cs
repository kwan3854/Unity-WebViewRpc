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
        
        private readonly Dictionary<string, ChunkSet> _chunkSets = new();
        private readonly object _lock = new object();
        
        /// <summary>
        /// Try to reassemble chunks. Returns null if not all chunks received yet.
        /// </summary>
        public byte[] TryAssemble(RpcEnvelope envelope)
        {
            if (envelope.ChunkInfo == null)
            {
                // Not a chunked message
                return envelope.Payload.ToByteArray();
            }
            
            lock (_lock)
            {
                var chunkInfo = envelope.ChunkInfo;
                
                if (!_chunkSets.TryGetValue(chunkInfo.ChunkSetId, out var chunkSet))
                {
                    chunkSet = new ChunkSet
                    {
                        TotalChunks = chunkInfo.TotalChunks,
                        OriginalSize = chunkInfo.OriginalSize,
                        LastActivity = DateTime.UtcNow
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
                
                // Cleanup old chunk sets (older than 30 seconds)
                var cutoff = DateTime.UtcNow.AddSeconds(-30);
                var toRemove = _chunkSets
                    .Where(kvp => kvp.Value.LastActivity < cutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in toRemove)
                {
                    _chunkSets.Remove(key);
                    Debug.LogWarning($"Removed incomplete chunk set {key} due to timeout");
                }
                
                return null; // Not all chunks received yet
            }
        }
    }
} 