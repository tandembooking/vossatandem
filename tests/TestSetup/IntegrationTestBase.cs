using System;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using TandemBooking.Models;
using Xunit;

namespace TandemBooking.Tests.TestSetup
{
    public class IntegrationTestBase : IClassFixture<IntegrationTestFixture>, IDisposable
    {
        protected TestServer Server { get; }
        protected HttpClient Client { get; }

        protected TandemBookingContext Context { get; }
        protected IDbContextTransaction Transaction { get; }

        public IntegrationTestBase(IntegrationTestFixture fixture)
        {
            Server = fixture.Server;
            Client = fixture.Client;

            Context = Server.Host.Services.GetService<TandemBookingContext>();

            //Run everything inside a transaction
            Transaction = Context.Database.BeginTransaction();
        }

        public virtual void Dispose()
        {
            //roll back transaction
            Transaction.Dispose();            
        }

        public T GetService<T>()
        {
           return Server.Host.Services.GetRequiredService<T>();
        }

    }
}