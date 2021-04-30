﻿using Kooboo.Data;
using Kooboo.Data.Context;
using Kooboo.Data.Events;
using Kooboo.Data.Models;
using Kooboo.Events;
using Kooboo.Sites.Commerce.Migration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Kooboo.Sites.Commerce
{
    public class SiteCommerce
    {
        static readonly ConcurrentDictionary<Guid, SiteCommerce> _siteCache = new ConcurrentDictionary<Guid, SiteCommerce>();
        readonly Lazy<string> _connectionString;
        readonly Dictionary<Type, ServiceBase> _serviceMap;
        readonly Dictionary<Type, ICacheBase> _cacheMap;


        public static SiteCommerce Get(WebSite webSite)
        {
            return _siteCache.GetOrAdd(webSite.Id, _ =>
            {
                return new SiteCommerce(webSite);
            });
        }

        public static void Remove(WebSite webSite)
        {
            _siteCache.TryRemove(webSite.Id, out _);
        }

        public int LastMigrateVersion { get; }

        public SiteCommerce(WebSite site)
        {
            _serviceMap = Lib.IOC.Service.GetImplementationTypes<ServiceBase>().ToDictionary(k => k, v =>
            {
                return Activator.CreateInstance(v, new[] { this }) as ServiceBase;
            });

            _cacheMap = Lib.IOC.Service.GetImplementationTypes(typeof(ICacheBase)).ToDictionary(k => k, v =>
            {
                return Activator.CreateInstance(v, new[] { this }) as ICacheBase;
            });

            _connectionString = new Lazy<string>(() =>
            {
                var path = Path.Combine(AppSettings.GetFileIORoot(site), "commerce.db");
                return $"Data source = '{path}';Version=3;BinaryGUID=False;foreign keys=true";
            }, true);


            LastMigrateVersion = Migrator.Migrate(CreateDbConnection());
        }

        public T Service<T>() where T : ServiceBase
        {
            return _serviceMap[typeof(T)] as T;
        }

        public T Cache<T>() where T : class, ICacheBase
        {
            return _cacheMap[typeof(T)] as T;
        }

        public IDbConnection CreateDbConnection() => new SQLiteConnection(_connectionString.Value);
    }

    class CommerceCacheWebSiteChangeHandler : IHandler<WebSiteChange>
    {
        public void Handle(WebSiteChange theEvent, RenderContext context)
        {
            if (theEvent.ChangeType != ChangeType.Delete) return;
            SiteCommerce.Remove(theEvent.WebSite);
        }
    }
}

