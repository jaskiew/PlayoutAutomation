using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Database.Interfaces;
using TAS.Common.Database.Interfaces.Media;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Common.Interfaces.Security;

namespace TAS.Database.SQLite
{
    [Export(typeof(IDatabase))]
    public class DatabaseSQLite : IDatabase
    {
        private SQLiteConnection _connection;
        public string ConnectionStringPrimary { get; private set; }
        public string ConnectionStringSecondary { get; private set; }

        public ConnectionStateRedundant ConnectionState => (ConnectionStateRedundant)_connection.State;

        public IDictionary<string, int> ServerMediaFieldLengths { get; private set; }

        public IDictionary<string, int> ArchiveMediaFieldLengths { get; private set; }

        public IDictionary<string, int> EventFieldLengths { get; private set; }

        public IDictionary<string, int> SecurityObjectFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> MediaSegmentFieldLengths { get; private set; }

        public IDictionary<string, int> EngineFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> ServerFieldLengths { get; } = new Dictionary<string, int>();

        public event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;

        public bool ArchiveContainsMedia(IArchiveDirectoryProperties dir, Guid mediaGuid)
        {
            if (dir == null || mediaGuid == Guid.Empty)
                return false;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT count(*) FROM archivemedia WHERE idArchive=@idArchive && MediaGuid=@MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                var result = cmd.ExecuteScalar();
                return result != null && (long)result > 0;
            }
        }

        public T ArchiveMediaFind<T>(IArchiveDirectoryServerSide dir, Guid mediaGuid) where T : IArchiveMedia, new()
        {
            var result = default(T);
            if (mediaGuid == Guid.Empty)
                return result;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT * FROM archivemedia WHERE idArchive=@idArchive && MediaGuid=@MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                using (var dataReader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (dataReader.Read())
                    {
                        result = _readArchiveMedia<T>(dataReader);
                    }
                    dataReader.Close();
                }
            }
            return result;
        }

        public List<T> ArchiveMediaSearch<T>(IArchiveDirectoryServerSide dir, TMediaCategory? mediaCategory, string search) where T : IArchiveMedia, new()
        {
            lock (_connection)
            {
                var textSearches = (from text in search.ToLower().Split(' ').Where(s => !string.IsNullOrEmpty(s)) select "(LOWER(MediaName) LIKE \"%" + text + "%\" or LOWER(FileName) LIKE \"%" + text + "%\")").ToArray();
                SQLiteCommand cmd;
                if (mediaCategory == null)
                    cmd = new SQLiteCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive"
                                                + (textSearches.Length > 0 ? " and" + string.Join(" and", textSearches) : string.Empty)
                                                + " order by idArchiveMedia DESC LIMIT 0, 1000;", _connection);
                else
                {
                    cmd = new SQLiteCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and ((flags >> 4) & 3)=@Category"
                                                + (textSearches.Length > 0 ? " and" + string.Join(" and", textSearches) : string.Empty)
                                                + " order by idArchiveMedia DESC LIMIT 0, 1000;", _connection);
                    cmd.Parameters.AddWithValue("@Category", (uint)mediaCategory);
                }
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                using (var dataReader = cmd.ExecuteReader())
                {
                    var result = new List<T>();
                    while (dataReader.Read())
                    {
                        var media = _readArchiveMedia<T>(dataReader);
                        dir.AddMedia(media);
                        result.Add(media);
                    }
                    dataReader.Close();
                    return result;
                }
            }
        }

        public void AsRunLogWrite(ulong idEngine, IEvent e)
        {
            try
            {
                lock (_connection)
                {
                    var cmd = new SQLiteCommand(
@"INSERT INTO asrunlog (
idEngine,
ExecuteTime, 
MediaName, 
StartTC,
Duration,
idProgramme, 
idAuxMedia, 
idAuxRundown, 
SecEvents, 
typVideo, 
typAudio,
Flags
)
VALUES
(
@idEngine,
@ExecuteTime, 
@MediaName, 
@StartTC,
@Duration,
@idProgramme, 
@idAuxMedia, 
@idAuxRundown, 
@SecEvents, 
@typVideo, 
@typAudio,
@Flags
);", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", idEngine);
                    cmd.Parameters.AddWithValue("@ExecuteTime", e.StartTime);
                    var media = e.Media;
                    if (media != null)
                    {
                        cmd.Parameters.AddWithValue("@MediaName", media.MediaName);
                        if (media is IPersistentMedia)
                            cmd.Parameters.AddWithValue("@idAuxMedia", (media as IPersistentMedia).IdAux);
                        else
                            cmd.Parameters.AddWithValue("@idAuxMedia", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
                        cmd.Parameters.AddWithValue("@typAudio", (byte)media.AudioChannelMapping);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@MediaName", e.EventName);
                        cmd.Parameters.AddWithValue("@idAuxMedia", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typAudio", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@StartTC", e.StartTc);
                    cmd.Parameters.AddWithValue("@Duration", e.Duration);
                    cmd.Parameters.AddWithValue("@idProgramme", e.IdProgramme);
                    cmd.Parameters.AddWithValue("@idAuxRundown", e.IdAux);
                    cmd.Parameters.AddWithValue("@SecEvents", string.Join(";", e.SubEvents.Select(se => se.EventName)));
                    cmd.Parameters.AddWithValue("@Flags", e.ToFlags());
                    cmd.ExecuteNonQuery();
                }
                Debug.WriteLine(e, "AsRunLog written for");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void CloneDatabase(string connectionStringSource, string connectionStringDestination)
        {
            var backupFile = Path.GetTempFileName();
            var csb = new SQLiteConnectionStringBuilder(connectionStringDestination);
            var databaseName = csb.DataSource;
            if (string.IsNullOrWhiteSpace(databaseName))
                return;
            csb.Remove("Database");
            try
            {
                using (var conn = new SQLiteConnection(connectionStringSource))
                {
                    using (var cmd = new SQLiteCommand())

                    {
                        using (var mb = new SQLiteConnection(cmd))
                        {
                            mb.ExportInfo.MaxSqlLength = 1024 * 1024; // 1M
                            cmd.Connection = conn;
                            conn.Open();
                            mb.ExportToFile(backupFile);
                            conn.Close();
                        }
                    }
                }
                //file ready
                using (var conn = new MySqlConnection(csb.ConnectionString))
                {
                    conn.Open();
                    using (var createCommand = new MySqlCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET = {charset};", conn))
                    {
                        if (createCommand.ExecuteNonQuery() != 1)
                            return;
                        using (var useCommand = new MySqlCommand($"use {databaseName};", conn))
                        {
                            useCommand.ExecuteNonQuery();
                            using (var cmd = new MySqlCommand())
                            {
                                using (var mb = new MySqlBackup(cmd))
                                {
                                    cmd.Connection = conn;
                                    mb.ImportFromFile(backupFile);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                File.Delete(backupFile);
            }
        }

        public void Close()
        {
            _connection.Close();
        }

        public bool CreateEmptyDatabase(string connectionString, string collate)
        {
            var csb = new SQLiteConnectionStringBuilder(connectionString);
            var databaseName = csb.DataSource;
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;
            csb.Remove("Database");
            csb.Remove("CharacterSet");
            using (var connection = new SQLiteConnection(csb.ConnectionString))
            {
                connection.Open();
                using (var createCommand = new SQLiteCommand($"CREATE DATABASE `{databaseName}` COLLATE = {collate};", connection))
                {
                    if (createCommand.ExecuteNonQuery() == 1)
                    {
                        using (var useCommand = new SQLiteCommand($"use {databaseName};", connection))
                        {
                            useCommand.ExecuteNonQuery();
                            using (var scriptReader = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Database.MySqlRedundant.database.sql")))
                            {
                                var createStatements = scriptReader.ReadToEnd();
                                var createScript = new SQLiteCommand( createStatements,connection);
                                if (createScript.ExecuteNonQuery() > 0)
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        public void DeleteArchiveDirectory(IArchiveDirectoryProperties dir)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM archive WHERE idArchive=@idArchive;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteEngine(IEnginePersistent engine)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM engine WHERE idEngine=@idEngine;", _connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public bool DeleteEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM engine_acl WHERE idEngine_ACL=@idEngine_ACL;", _connection);
                cmd.Parameters.AddWithValue("@idEngine_ACL", acl.Id);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public bool DeleteEvent(IEventPersistent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool DeleteEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            throw new NotImplementedException();
        }

        public bool DeleteMedia(IAnimatedMedia animatedMedia)
        {
            throw new NotImplementedException();
        }

        public bool DeleteMedia(IArchiveMedia archiveMedia)
        {
            throw new NotImplementedException();
        }

        public bool DeleteMedia(IServerMedia serverMedia)
        {
            throw new NotImplementedException();
        }

        public void DeleteMediaSegment(IMediaSegment mediaSegment)
        {
            throw new NotImplementedException();
        }

        public void DeleteSecurityObject(ISecurityObject aco)
        {
            throw new NotImplementedException();
        }

        public void DeleteServer(IPlayoutServerProperties server)
        {
            throw new NotImplementedException();
        }

        public bool DropDatabase(string connectionString)
        {
            throw new NotImplementedException();
        }

        public List<T> FindArchivedStaleMedia<T>(IArchiveDirectoryServerSide dir) where T : IArchiveMedia, new()
        {
            throw new NotImplementedException();
        }

        public void InitializeFieldLengths()
        {
            throw new NotImplementedException();
        }

        public void InsertArchiveDirectory(IArchiveDirectoryProperties dir)
        {
            throw new NotImplementedException();
        }

        public void InsertEngine(IEnginePersistent engine)
        {
            throw new NotImplementedException();
        }

        public bool InsertEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            throw new NotImplementedException();
        }

        public bool InsertEvent(IEventPersistent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool InsertEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            throw new NotImplementedException();
        }

        public bool InsertMedia(IAnimatedMedia animatedMedia, ulong serverId)
        {
            throw new NotImplementedException();
        }

        public bool InsertMedia(IArchiveMedia archiveMedia, ulong serverid)
        {
            throw new NotImplementedException();
        }

        public bool InsertMedia(IServerMedia serverMedia, ulong serverId)
        {
            throw new NotImplementedException();
        }

        public void InsertSecurityObject(ISecurityObject aco)
        {
            throw new NotImplementedException();
        }

        public void InsertServer(IPlayoutServerProperties server)
        {
            throw new NotImplementedException();
        }

        public List<T> Load<T>() where T : ISecurityObject
        {
            throw new NotImplementedException();
        }

        public void LoadAnimationDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IAnimatedMedia, new()
        {
            throw new NotImplementedException();
        }

        public List<T> LoadArchiveDirectories<T>() where T : IArchiveDirectoryProperties, new()
        {
            throw new NotImplementedException();
        }

        public T LoadArchiveDirectory<T>(ulong idArchive) where T : IArchiveDirectoryServerSide, new()
        {
            throw new NotImplementedException();
        }

        public List<T> LoadEngines<T>(ulong? instance = null) where T : IEnginePersistent
        {
            throw new NotImplementedException();
        }

        public void LoadServerDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IServerMedia, new()
        {
            throw new NotImplementedException();
        }

        public List<T> LoadServers<T>() where T : IPlayoutServerProperties
        {
            throw new NotImplementedException();
        }

        public MediaDeleteResult MediaInUse(IEngine engine, IServerMedia serverMedia)
        {
            throw new NotImplementedException();
        }

        public T MediaSegmentsRead<T>(IPersistentMedia media) where T : IMediaSegments
        {
            throw new NotImplementedException();
        }

        public void Open(string connectionStringPrimary = null, string connectionStringSecondary = null)
        {
            if (connectionStringPrimary != null)
            {
                ConnectionStringPrimary = connectionStringPrimary;
                ConnectionStringSecondary = connectionStringSecondary;
            }
            _connection = new SQLiteConnection(ConnectionStringPrimary);
            _connection.StateRedundantChange += _connection_StateRedundantChange;
            _connection.Open();
        }

        public List<IAclRight> ReadEngineAclList<TEngineAcl>(IPersistent engine, IAuthenticationServicePersitency authenticationService) where TEngineAcl : IAclRight, IPersistent, new()
        {
            throw new NotImplementedException();
        }

        public IEvent ReadEvent(IEngine engine, ulong idRundownEvent)
        {
            throw new NotImplementedException();
        }

        public List<IAclRight> ReadEventAclList<TEventAcl>(IEventPersistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl : IAclRight, IPersistent, new()
        {
            throw new NotImplementedException();
        }

        public IEvent ReadNext(IEngine engine, IEventPersistent aEvent)
        {
            throw new NotImplementedException();
        }

        public void ReadRootEvents(IEngine engine)
        {
            throw new NotImplementedException();
        }

        public List<IEvent> ReadSubEvents(IEngine engine, IEventPersistent eventOwner)
        {
            throw new NotImplementedException();
        }

        public ulong SaveMediaSegment(IMediaSegment mediaSegment)
        {
            throw new NotImplementedException();
        }

        public void SearchMissing(IEngine engine)
        {
            throw new NotImplementedException();
        }

        public List<IEvent> SearchPlaying(IEngine engine)
        {
            throw new NotImplementedException();
        }

        public void TestConnect(string connectionString)
        {
            throw new NotImplementedException();
        }

        public void UpdateArchiveDirectory(IArchiveDirectoryProperties dir)
        {
            throw new NotImplementedException();
        }

        public bool UpdateDb()
        {
            throw new NotImplementedException();
        }

        public void UpdateEngine(IEnginePersistent engine)
        {
            throw new NotImplementedException();
        }

        public bool UpdateEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            throw new NotImplementedException();
        }

        public bool UpdateEvent<TEvent>(TEvent aEvent) where TEvent : IEventPersistent
        {
            throw new NotImplementedException();
        }

        public bool UpdateEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            throw new NotImplementedException();
        }

        public void UpdateMedia(IAnimatedMedia animatedMedia, ulong serverId)
        {
            throw new NotImplementedException();
        }

        public void UpdateMedia(IArchiveMedia archiveMedia, ulong serverId)
        {
            throw new NotImplementedException();
        }

        public void UpdateMedia(IServerMedia serverMedia, ulong serverId)
        {
            throw new NotImplementedException();
        }

        public bool UpdateRequired()
        {
            throw new NotImplementedException();
        }

        public void UpdateSecurityObject(ISecurityObject aco)
        {
            throw new NotImplementedException();
        }

        public void UpdateServer(IPlayoutServerProperties server)
        {
            throw new NotImplementedException();
        }
    }
}
