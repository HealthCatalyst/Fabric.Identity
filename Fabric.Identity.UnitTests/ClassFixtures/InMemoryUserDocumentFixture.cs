﻿using System;
using System.Collections.Generic;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;

namespace Fabric.Identity.UnitTests.ClassFixtures
{
    public class InMemoryUserDocumentFixture : IDisposable
    {
        public InMemoryDocumentService DocumentService { get; }

        public InMemoryUserDocumentFixture()
        {
            DocumentService = new InMemoryDocumentService();            
            _users.ForEach(u => DocumentService.AddDocument($"{u.SubjectId}:{u.ProviderName}", u));
        }

        private readonly List<User> _users = new List<User>
        {
            new User
            {
                SubjectId = "UserOne",
                ProviderName = "AD",
                Username = "User One"
            },
            new User
            {
                SubjectId = "UserTwo",
                ProviderName = "AzureAD",
                Username = "User Two"
            },
            new User
            {
                SubjectId = "UserThree",
                ProviderName = "AzureAD",
                Username = "User Three"
            }
        };

        public void Dispose()
        {
           // DocumentService.Clean();
        }
    }   
}
