﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Services
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId);
        Task<IEnumerable<T>> GetDocuments<T>(string documentType);
        void AddDocument<T>(string documentId, T documentObject);
        void UpdateDocument<T>(string documentId, T documentObject);
        void DeleteDocument<T>(string documentId);
    }
}
