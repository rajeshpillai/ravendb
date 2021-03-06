﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raven.Client;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Smuggler;
using Raven.Client.ServerWide;
using Raven.Client.Util;
using Raven.Server.Documents;
using Raven.Server.ServerWide;
using Raven.Server.ServerWide.Context;
using Raven.Server.Smuggler.Documents.Data;
using Raven.Server.Utils;
using Sparrow.Json;
using Voron;

namespace Raven.Server.Smuggler.Documents
{
    public class DatabaseSource : ISmugglerSource
    {
        private readonly DocumentDatabase _database;
        private DocumentsOperationContext _context;

        private readonly long _startDocumentEtag;

        private IDisposable _returnContext;
        private IDisposable _disposeTransaction;

        private int _currentTypeIndex;

        private readonly DatabaseItemType[] _types =
        {
            DatabaseItemType.DatabaseRecord,
            DatabaseItemType.Documents,
            DatabaseItemType.RevisionDocuments,
            DatabaseItemType.Tombstones,
            DatabaseItemType.Conflicts,
            DatabaseItemType.Indexes,
            DatabaseItemType.Identities,
            DatabaseItemType.CompareExchange,
            DatabaseItemType.None
        };

        public DatabaseSource(DocumentDatabase database, long startDocumentEtag)
        {
            _database = database;
            _startDocumentEtag = startDocumentEtag;
        }

        public IDisposable Initialize(DatabaseSmugglerOptions options, SmugglerResult result, out long buildVersion)
        {
            _currentTypeIndex = 0;
            _returnContext = _database.DocumentsStorage.ContextPool.AllocateOperationContext(out _context);
            _disposeTransaction = _context.OpenReadTransaction();

            buildVersion = ServerVersion.Build;
            return new DisposableAction(() =>
            {
                _disposeTransaction.Dispose();
                _returnContext.Dispose();
            });
        }

        public DatabaseItemType GetNextType()
        {
            return _types[_currentTypeIndex++];
        }

        public DatabaseRecord GetDatabaseRecord()
        {
            return _database.ReadDatabaseRecord();
        }

        public IEnumerable<DocumentItem> GetDocuments(List<string> collectionsToExport, INewDocumentActions actions)
        {
            var documents = collectionsToExport.Count != 0
                ? _database.DocumentsStorage.GetDocumentsFrom(_context, collectionsToExport, _startDocumentEtag, int.MaxValue)
                : _database.DocumentsStorage.GetDocumentsFrom(_context, _startDocumentEtag, 0, int.MaxValue);

            foreach (var document in documents)
            {
                yield return new DocumentItem
                {
                    Document = document
                };
            }
        }

        public IEnumerable<DocumentItem> GetRevisionDocuments(List<string> collectionsToExport, INewDocumentActions actions)
        {
            var revisionsStorage = _database.DocumentsStorage.RevisionsStorage;
            if (revisionsStorage.Configuration == null)
                yield break;

            var documents = revisionsStorage.GetRevisionsFrom(_context, _startDocumentEtag, int.MaxValue);
            foreach (var document in documents)
            {
                yield return new DocumentItem
                {
                    Document = document
                };
            }
        }

        public IEnumerable<DocumentItem> GetLegacyAttachments(INewDocumentActions actions)
        {
            return Enumerable.Empty<DocumentItem>();
        }

        public IEnumerable<string> GetLegacyAttachmentDeletions()
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetLegacyDocumentDeletions()
        {
            return Enumerable.Empty<string>();
        }

        public Stream GetAttachmentStream(LazyStringValue hash, out string tag)
        {
            using (Slice.External(_context.Allocator, hash, out Slice hashSlice))
            {
                return _database.DocumentsStorage.AttachmentsStorage.GetAttachmentStream(_context, hashSlice, out tag);
            }
        }

        public IEnumerable<DocumentTombstone> GetTombstones(List<string> collectionsToExport, INewDocumentActions actions)
        {
            var tombstones = collectionsToExport.Count > 0
                ? _database.DocumentsStorage.GetTombstonesFrom(_context, collectionsToExport, _startDocumentEtag, int.MaxValue)
                : _database.DocumentsStorage.GetTombstonesFrom(_context, _startDocumentEtag, 0, int.MaxValue);

            foreach (var tombstone in tombstones)
            {
                yield return tombstone;
            }
        }

        public IEnumerable<DocumentConflict> GetConflicts(List<string> collectionsToExport, INewDocumentActions actions)
        {
            var conflicts = _database.DocumentsStorage.ConflictsStorage.GetConflictsFrom(_context, _startDocumentEtag);

            if (collectionsToExport.Count > 0)
            {
                foreach (var conflict in conflicts)
                {
                    if (collectionsToExport.Contains(conflict.Collection) == false)
                        continue;

                    yield return conflict;
                }

                yield break;
            }

            foreach (var conflict in conflicts)
            {
                yield return conflict;
            }
        }

        public IEnumerable<IndexDefinitionAndType> GetIndexes()
        {
            var allIndexes = _database.IndexStore.GetIndexes().ToList();
            var sideBySideIndexes = allIndexes.Where(x => x.Name.StartsWith(Constants.Documents.Indexing.SideBySideIndexNamePrefix)).ToList();

            var originalSideBySideIndexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var index in sideBySideIndexes)
            {
                allIndexes.Remove(index);

                if (index.Type == IndexType.Faulty)
                    continue;

                var indexName = index.Name.Remove(0, Constants.Documents.Indexing.SideBySideIndexNamePrefix.Length);
                originalSideBySideIndexNames.Add(indexName);
                var indexDefinition = index.GetIndexDefinition();
                indexDefinition.Name = indexName;

                yield return new IndexDefinitionAndType
                {
                    IndexDefinition = indexDefinition,
                    Type = index.Type
                };
            }

            foreach (var index in allIndexes)
            {
                if (originalSideBySideIndexNames.Contains(index.Name))
                    continue;
                
                if (index.Type == IndexType.Faulty)
                    continue;

                if (index.Type.IsStatic())
                {
                    yield return new IndexDefinitionAndType
                    {
                        IndexDefinition = index.GetIndexDefinition(),
                        Type = index.Type
                    };

                    continue;
                }

                yield return new IndexDefinitionAndType
                {
                    IndexDefinition = index.Definition,
                    Type = index.Type
                };
            }
        }

        public IDisposable GetIdentities(out IEnumerable<(string Prefix, long Value)> identities)
        {
            using (var scope = new DisposableScope())
            {
                scope.EnsureDispose(_database.ServerStore.ContextPool.AllocateOperationContext(out TransactionOperationContext context));
                scope.EnsureDispose(context.OpenReadTransaction());

                identities = _database.ServerStore.Cluster.ReadIdentities(context, _database.Name, 0, long.MaxValue);

                return scope.Delay();
            }
        }

        public IDisposable GetCompareExchangeValues(out IEnumerable<(string key, long index, BlittableJsonReaderObject value)> compareExchange)
        {
            using (var scope = new DisposableScope())
            {
                scope.EnsureDispose(_database.ServerStore.ContextPool.AllocateOperationContext(out TransactionOperationContext context));
                scope.EnsureDispose(context.OpenReadTransaction());

                compareExchange = _database.ServerStore.Cluster.GetCompareExchangeValuesStartsWith(context, _database.Name, _database.Name, 0, int.MaxValue);

                return scope.Delay();
            }
        }

        public long SkipType(DatabaseItemType type, Action<long> onSkipped)
        {
            return 0; // no-op
        }
    }
}
