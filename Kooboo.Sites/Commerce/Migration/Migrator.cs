﻿using Dapper;
using Kooboo.Data.Context;
using Kooboo.Data.Models;
using Kooboo.Sites.Commerce.Cache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace Kooboo.Sites.Commerce.Migration
{
    public static class Migrator
    {
        readonly static List<IMigration> _migrationRecords = Lib.IOC.Service.GetInstances<IMigration>();

        private static void DeleteTables(IDbConnection connection)
        {

            connection.Execute(@"
drop table if exists 'Consignee';
drop table if exists 'OrderItem';
drop table if exists 'Order';
drop table if exists 'CartItem';
drop table if exists 'ProductCategory';
drop table if exists 'ProductStock';
drop table if exists 'ProductVariant';
drop table if exists 'Product';
drop table if exists 'Category';
drop table if exists 'ProductType';
drop table if exists 'Promotion';
drop table if exists 'Customer';
drop table if exists '_migration';
");

        }

        public static int Migrate(IDbConnection connection)
        {
            //DeleteTables(connection);

            connection.Execute("create table if not exists _migration( ver integer not null);");
            var lastVersion = connection.QueryFirstOrDefault<int?>("select max(ver) from _migration") ?? 0;
            var appends = _migrationRecords.Where(w => w.Version > lastVersion).OrderBy(o => o.Version);

            foreach (var item in appends)
            {
                item.Migrate(connection);
                connection.Execute($"INSERT INTO _migration (ver) VALUES({item.Version});");
            }

            if (lastVersion == 0) Seed.Insert(connection);

            return lastVersion;
        }
    }
}
