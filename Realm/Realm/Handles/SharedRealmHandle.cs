////////////////////////////////////////////////////////////////////////////
//
// Copyright 2016 Realm Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MongoDB.Bson;
using Realms.Exceptions;
using Realms.Logging;
using Realms.Native;
using Realms.Schema;
using static Realms.RealmConfiguration;

namespace Realms
{
    internal class SharedRealmHandle : RealmHandle
    {
        private static class NativeMethods
        {
#pragma warning disable IDE0049 // Use built-in type alias
#pragma warning disable SA1121 // Use built-in type alias

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void NotifyRealmCallback(IntPtr stateHandle);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void GetNativeSchemaCallback(Native.Schema schema, IntPtr managed_callback);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void OpenRealmCallback(IntPtr task_completion_source, IntPtr shared_realm, NativeException ex);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void OnBindingContextDestructedCallback(IntPtr handle);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LogMessageCallback(PrimitiveValue message, LogLevel level);

            [return: MarshalAs(UnmanagedType.U1)]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate bool MigrationCallback(IntPtr oldRealm, IntPtr newRealm, Native.Schema oldSchema, ulong schemaVersion, IntPtr managedMigrationHandle);

            [return: MarshalAs(UnmanagedType.U1)]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate bool ShouldCompactCallback(IntPtr config, ulong totalSize, ulong dataSize);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_open", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr open(Configuration configuration,
                [MarshalAs(UnmanagedType.LPArray), In] SchemaObject[] objects, int objects_length,
                [MarshalAs(UnmanagedType.LPArray), In] SchemaProperty[] properties,
                byte[] encryptionKey,
                out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_open_with_sync", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr open_with_sync(Configuration configuration, Sync.Native.SyncConfiguration sync_configuration,
                [MarshalAs(UnmanagedType.LPArray), In] SchemaObject[] objects, int objects_length,
                [MarshalAs(UnmanagedType.LPArray), In] SchemaProperty[] properties,
                byte[] encryptionKey,
                out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_open_with_sync_async", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr open_with_sync_async(Configuration configuration, Sync.Native.SyncConfiguration sync_configuration,
                [MarshalAs(UnmanagedType.LPArray), In] SchemaObject[] objects, int objects_length,
                [MarshalAs(UnmanagedType.LPArray), In] SchemaProperty[] properties,
                byte[] encryptionKey,
                IntPtr task_completion_source,
                out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_set_managed_state_handle", CallingConvention = CallingConvention.Cdecl)]
            public static extern void set_managed_state_handle(SharedRealmHandle sharedRealm, IntPtr managedStateHandle, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_get_managed_state_handle", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr get_managed_state_handle(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_destroy", CallingConvention = CallingConvention.Cdecl)]
            public static extern void destroy(IntPtr sharedRealm);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_close_realm", CallingConvention = CallingConvention.Cdecl)]
            public static extern void close_realm(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_delete_files", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
            public static extern void delete_files([MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr path_len, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_close_all_realms", CallingConvention = CallingConvention.Cdecl)]
            public static extern void close_all_realms(out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_begin_transaction", CallingConvention = CallingConvention.Cdecl)]
            public static extern void begin_transaction(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_commit_transaction", CallingConvention = CallingConvention.Cdecl)]
            public static extern void commit_transaction(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_cancel_transaction", CallingConvention = CallingConvention.Cdecl)]
            public static extern void cancel_transaction(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_is_in_transaction", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool is_in_transaction(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_refresh", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool refresh(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_get_table_key", CallingConvention = CallingConvention.Cdecl)]
            public static extern UInt32 get_table_key(SharedRealmHandle sharedRealm, [MarshalAs(UnmanagedType.LPWStr)] string tableName, IntPtr tableNameLength, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_is_same_instance", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool is_same_instance(SharedRealmHandle lhs, SharedRealmHandle rhs, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_get_schema_version", CallingConvention = CallingConvention.Cdecl)]
            public static extern ulong get_schema_version(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_compact", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool compact(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_resolve_reference", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr resolve_reference(SharedRealmHandle sharedRealm, ThreadSafeReferenceHandle referenceHandle, ThreadSafeReference.Type type, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_resolve_realm_reference", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr resolve_realm_reference(ThreadSafeReferenceHandle referenceHandle, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_write_copy", CallingConvention = CallingConvention.Cdecl)]
            public static extern void write_copy(SharedRealmHandle sharedRealm, [MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr path_len, byte[] encryptionKey, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_create_object", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr create_object(SharedRealmHandle sharedRealm, UInt32 table_key, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_create_object_unique", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr create_object_unique(SharedRealmHandle sharedRealm, UInt32 table_key, PrimitiveValue value,
                                                             [MarshalAs(UnmanagedType.U1)] bool update,
                                                             [MarshalAs(UnmanagedType.U1)] out bool is_new, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_get_schema", CallingConvention = CallingConvention.Cdecl)]
            public static extern void get_schema(SharedRealmHandle sharedRealm, IntPtr callback, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_install_callbacks", CallingConvention = CallingConvention.Cdecl)]
            public static extern void install_callbacks(
                NotifyRealmCallback notify_realm_callback,
                GetNativeSchemaCallback native_schema_callback,
                OpenRealmCallback open_callback,
                OnBindingContextDestructedCallback context_destructed_callback,
                LogMessageCallback log_message_callback,
                NotifiableObjectHandleBase.NotificationCallback notify_object,
                DictionaryHandle.KeyNotificationCallback notify_dictionary,
                MigrationCallback migration_callback,
                ShouldCompactCallback should_compact_callback);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_has_changed", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool has_changed(SharedRealmHandle sharedRealm);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_get_is_frozen", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool get_is_frozen(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_freeze", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr freeze(SharedRealmHandle sharedRealm, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_get_object_for_primary_key", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr get_object_for_primary_key(SharedRealmHandle realmHandle, UInt32 table_key, PrimitiveValue value, out NativeException ex);

            [DllImport(InteropConfig.DLL_NAME, EntryPoint = "shared_realm_create_results", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr create_results(SharedRealmHandle sharedRealm, UInt32 table_key, out NativeException ex);

#pragma warning restore SA1121 // Use built-in type alias
#pragma warning restore IDE0049 // Use built-in type alias
        }

        static SharedRealmHandle()
        {
            NativeCommon.Initialize();
        }

        public static void Initialize()
        {
            NativeMethods.NotifyRealmCallback notifyRealm = NotifyRealmChanged;
            NativeMethods.GetNativeSchemaCallback getNativeSchema = GetNativeSchema;
            NativeMethods.OpenRealmCallback openRealm = HandleOpenRealmCallback;
            NativeMethods.OnBindingContextDestructedCallback onBindingContextDestructed = OnBindingContextDestructed;
            NativeMethods.LogMessageCallback logMessage = LogMessage;
            NotifiableObjectHandleBase.NotificationCallback notifyObject = NotifiableObjectHandleBase.NotifyObjectChanged;
            DictionaryHandle.KeyNotificationCallback notifyDictionary = DictionaryHandle.NotifyDictionaryChanged;
            NativeMethods.MigrationCallback onMigration = OnMigration;
            NativeMethods.ShouldCompactCallback shouldCompact = ShouldCompactOnLaunchCallback;

            GCHandle.Alloc(notifyRealm);
            GCHandle.Alloc(getNativeSchema);
            GCHandle.Alloc(openRealm);
            GCHandle.Alloc(onBindingContextDestructed);
            GCHandle.Alloc(logMessage);
            GCHandle.Alloc(notifyObject);
            GCHandle.Alloc(notifyDictionary);
            GCHandle.Alloc(onMigration);
            GCHandle.Alloc(shouldCompact);

            NativeMethods.install_callbacks(notifyRealm, getNativeSchema, openRealm, onBindingContextDestructed, logMessage, notifyObject, notifyDictionary, onMigration, shouldCompact);
        }

        [Preserve]
        public SharedRealmHandle(IntPtr handle) : base(null, handle)
        {
        }

        public virtual bool OwnsNativeRealm => true;

        protected override void Unbind()
        {
            NativeMethods.destroy(handle);
        }

        public static SharedRealmHandle Open(Configuration configuration, RealmSchema schema, byte[] encryptionKey)
        {
            var marshaledSchema = new SchemaMarshaler(schema);

            var result = NativeMethods.open(configuration, marshaledSchema.Objects, marshaledSchema.Objects.Length, marshaledSchema.Properties, encryptionKey, out var nativeException);
            nativeException.ThrowIfNecessary();
            return new SharedRealmHandle(result);
        }

        public static SharedRealmHandle OpenWithSync(Configuration configuration, Sync.Native.SyncConfiguration syncConfiguration, RealmSchema schema, byte[] encryptionKey)
        {
            var marshaledSchema = new SchemaMarshaler(schema);

            var result = NativeMethods.open_with_sync(configuration, syncConfiguration, marshaledSchema.Objects, marshaledSchema.Objects.Length, marshaledSchema.Properties, encryptionKey, out var nativeException);
            nativeException.ThrowIfNecessary();

            return new SharedRealmHandle(result);
        }

        public static AsyncOpenTaskHandle OpenWithSyncAsync(Configuration configuration, Sync.Native.SyncConfiguration syncConfiguration, RealmSchema schema, byte[] encryptionKey, GCHandle tcsHandle)
        {
            var marshaledSchema = new SchemaMarshaler(schema);

            var asyncTaskPtr = NativeMethods.open_with_sync_async(configuration, syncConfiguration, marshaledSchema.Objects, marshaledSchema.Objects.Length, marshaledSchema.Properties, encryptionKey, GCHandle.ToIntPtr(tcsHandle), out var nativeException);
            nativeException.ThrowIfNecessary();
            return new AsyncOpenTaskHandle(asyncTaskPtr);
        }

        public static SharedRealmHandle ResolveFromReference(ThreadSafeReferenceHandle referenceHandle)
        {
            var result = NativeMethods.resolve_realm_reference(referenceHandle, out var nativeException);
            nativeException.ThrowIfNecessary();
            return new SharedRealmHandle(result);
        }

        public void CloseRealm()
        {
            NativeMethods.close_realm(this, out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public static void DeleteFiles(string path)
        {
            NativeMethods.delete_files(path, (IntPtr)path.Length, out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public static void ForceCloseNativeRealms()
        {
            NativeMethods.close_all_realms(out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public bool IsFrozen
        {
            get
            {
                var result = NativeMethods.get_is_frozen(this, out var nativeException);
                nativeException.ThrowIfNecessary();
                return result;
            }
        }

        public void SetManagedStateHandle(Realm.State managedState)
        {
            // This is freed in OnBindingContextDestructed
            var stateHandle = GCHandle.Alloc(managedState);

            NativeMethods.set_managed_state_handle(this, GCHandle.ToIntPtr(stateHandle), out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public IntPtr GetManagedStateHandle()
        {
            var result = NativeMethods.get_managed_state_handle(this, out var nativeException);
            nativeException.ThrowIfNecessary();
            return result;
        }

        public void BeginTransaction()
        {
            NativeMethods.begin_transaction(this, out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public void CommitTransaction()
        {
            NativeMethods.commit_transaction(this, out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public void CancelTransaction()
        {
            NativeMethods.cancel_transaction(this, out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public bool IsInTransaction()
        {
            var result = NativeMethods.is_in_transaction(this, out var nativeException);
            nativeException.ThrowIfNecessary();
            return result;
        }

        public bool Refresh()
        {
            var result = NativeMethods.refresh(this, out var nativeException);
            nativeException.ThrowIfNecessary();
            return result;
        }

        public TableKey GetTableKey(string tableName)
        {
            var tableKey = NativeMethods.get_table_key(this, tableName, (IntPtr)tableName.Length, out var nativeException);
            nativeException.ThrowIfNecessary();
            return new TableKey(tableKey);
        }

        public bool IsSameInstance(SharedRealmHandle other)
        {
            var result = NativeMethods.is_same_instance(this, other, out var nativeException);
            nativeException.ThrowIfNecessary();
            return result;
        }

        public ulong GetSchemaVersion()
        {
            var result = NativeMethods.get_schema_version(this, out var nativeException);
            nativeException.ThrowIfNecessary();
            return result;
        }

        public bool Compact()
        {
            var result = NativeMethods.compact(this, out var nativeException);
            nativeException.ThrowIfNecessary();
            return result;
        }

        public IntPtr ResolveReference(ThreadSafeReference reference)
        {
            if (reference.Handle.IsClosed)
            {
                throw new RealmException("Can only resolve a thread safe reference once.");
            }

            var result = NativeMethods.resolve_reference(this, reference.Handle, reference.ReferenceType, out var nativeException);
            nativeException.ThrowIfNecessary();

            reference.Handle.Close();

            return result;
        }

        public void WriteCopy(string path, byte[] encryptionKey)
        {
            NativeMethods.write_copy(this, path, (IntPtr)path.Length, encryptionKey, out var nativeException);
            nativeException.ThrowIfNecessary();
        }

        public RealmSchema GetSchema()
        {
            RealmSchema result = null;
            Action<Native.Schema> callback = schema => result = RealmSchema.CreateFromObjectStoreSchema(schema);
            var callbackHandle = GCHandle.Alloc(callback);
            try
            {
                NativeMethods.get_schema(this, GCHandle.ToIntPtr(callbackHandle), out var nativeException);
                nativeException.ThrowIfNecessary();
            }
            finally
            {
                callbackHandle.Free();
            }

            return result;
        }

        public ObjectHandle CreateObject(TableKey tableKey)
        {
            var result = NativeMethods.create_object(this, tableKey.Value, out NativeException ex);
            ex.ThrowIfNecessary();
            return new ObjectHandle(this, result);
        }

        public ObjectHandle CreateObjectWithPrimaryKey(Property pkProperty, object primaryKey, TableKey tableKey, string parentType, bool update, out bool isNew)
        {
            if (primaryKey == null && !pkProperty.Type.IsNullable())
            {
                throw new ArgumentException($"{parentType}'s primary key is defined as non-nullable, but the value passed is null");
            }

            RealmValue pkValue = pkProperty.Type.ToRealmValueType() switch
            {
                RealmValueType.String => (string)primaryKey,
                RealmValueType.Int => primaryKey == null ? (long?)null : Convert.ToInt64(primaryKey),
                RealmValueType.ObjectId => (ObjectId?)primaryKey,
                RealmValueType.Guid => (Guid?)primaryKey,
                _ => throw new NotSupportedException($"Primary key of type {pkProperty.Type} is not supported"),
            };

            var (primitiveValue, handles) = pkValue.ToNative();
            var result = NativeMethods.create_object_unique(this, tableKey.Value, primitiveValue, update, out isNew, out var ex);
            handles?.Dispose();
            ex.ThrowIfNecessary();
            return new ObjectHandle(this, result);
        }

        public bool HasChanged()
        {
            return NativeMethods.has_changed(this);
        }

        public SharedRealmHandle Freeze()
        {
            var result = NativeMethods.freeze(this, out var nativeException);
            nativeException.ThrowIfNecessary();
            return new SharedRealmHandle(result);
        }

        public bool TryFindObject(TableKey tableKey, in RealmValue id, out ObjectHandle objectHandle)
        {
            var (primitiveValue, handles) = id.ToNative();
            var result = NativeMethods.get_object_for_primary_key(this, tableKey.Value, primitiveValue, out var ex);
            handles?.Dispose();
            ex.ThrowIfNecessary();

            if (result == IntPtr.Zero)
            {
                objectHandle = null;
                return false;
            }

            objectHandle = new ObjectHandle(this, result);
            return true;
        }

        public ResultsHandle CreateResults(TableKey tableKey)
        {
            var result = NativeMethods.create_results(this, tableKey.Value, out var nativeException);
            nativeException.ThrowIfNecessary();
            return new ResultsHandle(this, result);
        }

        [MonoPInvokeCallback(typeof(NativeMethods.GetNativeSchemaCallback))]
        private static void GetNativeSchema(Native.Schema schema, IntPtr managedCallbackPtr)
        {
            var handle = GCHandle.FromIntPtr(managedCallbackPtr);
            var callback = (Action<Native.Schema>)handle.Target;
            callback(schema);
        }

        [MonoPInvokeCallback(typeof(NativeMethods.NotifyRealmCallback))]
        public static void NotifyRealmChanged(IntPtr stateHandle)
        {
            var gch = GCHandle.FromIntPtr(stateHandle);
            ((Realm.State)gch.Target).NotifyChanged(EventArgs.Empty);
        }

        [MonoPInvokeCallback(typeof(NativeMethods.OpenRealmCallback))]
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The task awaiter will own the ThreadSafeReference handle.")]
        private static void HandleOpenRealmCallback(IntPtr taskCompletionSource, IntPtr realm_reference, NativeException ex)
        {
            var handle = GCHandle.FromIntPtr(taskCompletionSource);
            var tcs = (TaskCompletionSource<ThreadSafeReferenceHandle>)handle.Target;

            if (ex.type == RealmExceptionCodes.NoError)
            {
                tcs.TrySetResult(new ThreadSafeReferenceHandle(realm_reference));
            }
            else
            {
                var inner = ex.Convert();
                const string OuterMessage = "A system error occurred while opening a Realm. See InnerException for more details.";
                tcs.TrySetException(new RealmException(OuterMessage, inner));
            }
        }

        [MonoPInvokeCallback(typeof(NativeMethods.OnBindingContextDestructedCallback))]
        public static void OnBindingContextDestructed(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(handle).Free();
            }
        }

        [MonoPInvokeCallback(typeof(NativeMethods.LogMessageCallback))]
        private static void LogMessage(PrimitiveValue message, LogLevel level)
        {
            Logger.LogDefault(level, message.AsString());
        }

        [MonoPInvokeCallback(typeof(NativeMethods.MigrationCallback))]
        private static bool OnMigration(IntPtr oldRealmPtr, IntPtr newRealmPtr, Native.Schema oldSchema, ulong schemaVersion, IntPtr managedMigrationHandle)
        {
            var migrationHandle = GCHandle.FromIntPtr(managedMigrationHandle);
            var migration = (Migration)migrationHandle.Target;

            var oldRealmHandle = new UnownedRealmHandle(oldRealmPtr);
            var oldConfiguration = new RealmConfiguration(migration.Configuration.DatabasePath)
            {
                SchemaVersion = schemaVersion,
                IsReadOnly = true,
                EnableCache = false
            };
            var oldRealm = new Realm(oldRealmHandle, oldConfiguration, RealmSchema.CreateFromObjectStoreSchema(oldSchema));

            var newRealmHandle = new UnownedRealmHandle(newRealmPtr);
            var newRealm = new Realm(newRealmHandle, migration.Configuration, migration.Schema);

            var result = migration.Execute(oldRealm, newRealm);

            return result;
        }

        [MonoPInvokeCallback(typeof(NativeMethods.ShouldCompactCallback))]
        private static bool ShouldCompactOnLaunchCallback(IntPtr delegatePtr, ulong totalSize, ulong dataSize)
        {
            var handle = GCHandle.FromIntPtr(delegatePtr);
            var compactDelegate = (ShouldCompactDelegate)handle.Target;
            return compactDelegate(totalSize, dataSize);
        }

        public class SchemaMarshaler
        {
            public readonly SchemaObject[] Objects;
            public readonly SchemaProperty[] Properties;

            public SchemaMarshaler(RealmSchema schema)
            {
                var properties = new List<SchemaProperty>();

                Objects = schema.Select(@object =>
                {
                    var start = properties.Count;

                    properties.AddRange(@object.Select(ForMarshalling));

                    return new SchemaObject
                    {
                        name = @object.Name,
                        properties_start = start,
                        properties_end = properties.Count,
                        is_embedded = @object.IsEmbedded,
                    };
                }).ToArray();
                Properties = properties.ToArray();
            }

            public static SchemaProperty ForMarshalling(Property property)
            {
                return new SchemaProperty
                {
                    name = property.Name,
                    type = property.Type,
                    object_type = property.ObjectType,
                    link_origin_property_name = property.LinkOriginPropertyName,
                    is_indexed = property.IsIndexed,
                    is_primary = property.IsPrimaryKey
                };
            }
        }
    }
}
