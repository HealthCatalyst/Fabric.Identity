using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Newtonsoft.Json;

namespace Fabric.Identity.API.Persistence.InMemory.Services
{
    public class InMemoryDocumentService : IDocumentDbService
    {
        private static readonly ConcurrentDictionary<string, string> Documents =
            new ConcurrentDictionary<string, string>();

        public Task<T> GetDocument<T>(string documentId)
        {
            string documentJson;
            return Task.FromResult(Documents.TryGetValue(GetFullDocumentId<T>(documentId), out documentJson)
                ? JsonConvert.DeserializeObject<T>(documentJson, new SerializationSettings().JsonSettings)
                : default(T));
        }

        public Task<IEnumerable<T>> GetDocumentsById<T>(IEnumerable<string> documentIds)
        {
            var keys = documentIds.Select(GetFullDocumentId<T>);

            return Task.FromResult(Documents.Where(pair => keys.Contains(pair.Key))
                .Select(d => d.Value)
                .Deserialize<T>());
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string documentKey)
        {
            return Task.FromResult(Documents
                .Where(d => d.Key.StartsWith(documentKey, StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Value)
                .ToList()
                .Deserialize<T>());
        }

        public Task<int> GetDocumentCount(string documentType)
        {
            var count = Documents.Count(d => d.Key.StartsWith(documentType));
            return Task.FromResult(count);
        }

        public void AddDocument<T>(string documentId, T documentObject)
        {
            var fullDocumentId = GetFullDocumentId<T>(documentId);
            if (!Documents.TryAdd(fullDocumentId,
                JsonConvert.SerializeObject(documentObject, new SerializationSettings().JsonSettings)))
            {
                //TODO: Use non standard exception or change to TryAddDocument.
                throw new ArgumentException($"Document with id {documentId} already exists.");
            }
        }

        public void UpdateDocument<T>(string documentId, T documentObject)
        {
            var fullDocumentId = GetFullDocumentId<T>(documentId);
            var currentValue = Documents[fullDocumentId]; //TODO: support legitimate conditional updates ?

            if (!Documents.TryUpdate(fullDocumentId,
                JsonConvert.SerializeObject(documentObject, new SerializationSettings().JsonSettings), currentValue))
            {
                //TODO: Use non standard exception or change to TryUpdateDocument.
                throw new ArgumentException($"Failed to update document with id {documentId}.");
            }
        }

        public void DeleteDocument<T>(string documentId)
        {
            string document;
            Documents.TryRemove(GetFullDocumentId<T>(documentId), out document);
        }

        private static string GetFullDocumentId<T>(string documentId)
        {
            return $"{typeof(T).Name.ToLowerInvariant()}:{documentId}";
        }

        public void Clean()
        {
            Documents.Clear();
        }
    }
}